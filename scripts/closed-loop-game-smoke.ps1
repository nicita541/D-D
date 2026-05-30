# Backend closed-loop playable-flow smoke.
# Run from repository root:
# powershell -ExecutionPolicy Bypass -File ".\scripts\closed-loop-game-smoke.ps1"

$ErrorActionPreference = "Stop"
$BaseUrl = "http://localhost:8080"

function Write-Step($Text) {
    Write-Host ""
    Write-Host "== $Text ==" -ForegroundColor Cyan
}

function Assert-Status {
    param(
        [int]$Actual,
        [int[]]$Expected,
        [string]$Name,
        [string]$Body = ""
    )

    if ($Expected -notcontains $Actual) {
        Write-Host "FAILED: $Name" -ForegroundColor Red
        Write-Host "Expected: $($Expected -join ', '), Actual: $Actual" -ForegroundColor Red
        if (-not [string]::IsNullOrWhiteSpace($Body)) {
            Write-Host $Body
        }

        throw "$Name returned unexpected status $Actual."
    }

    Write-Host "OK $Actual - $Name" -ForegroundColor Green
}

function Read-ErrorResponseBody {
    param($Response)

    if ($null -eq $Response) {
        return ""
    }

    try {
        $stream = $Response.GetResponseStream()
        if ($null -eq $stream) {
            return ""
        }

        $reader = New-Object System.IO.StreamReader($stream)
        return $reader.ReadToEnd()
    }
    catch {
        return ""
    }
}

function Invoke-Api {
    param(
        [string]$Method,
        [string]$Path,
        [object]$Body = $null,
        [hashtable]$Headers = @{},
        [int[]]$ExpectedStatus = @(200),
        [string]$Name = $Path
    )

    $params = @{
        Method = $Method
        Uri = "$BaseUrl$Path"
        Headers = $Headers
        UseBasicParsing = $true
    }

    if ($null -ne $Body) {
        $params.ContentType = "application/json; charset=utf-8"
        $params.Body = ($Body | ConvertTo-Json -Depth 50)
    }

    try {
        $response = Invoke-WebRequest @params
        $statusCode = [int]$response.StatusCode
        $content = $response.Content
    }
    catch [System.Net.WebException] {
        $response = $_.Exception.Response
        if ($null -eq $response) {
            throw
        }

        $statusCode = [int]$response.StatusCode
        $content = Read-ErrorResponseBody $response
    }

    Assert-Status -Actual $statusCode -Expected $ExpectedStatus -Name $Name -Body $content

    if ([string]::IsNullOrWhiteSpace($content)) {
        return $null
    }

    try {
        return $content | ConvertFrom-Json
    }
    catch {
        return $content
    }
}

function Invoke-ApiRawJson {
    param(
        [string]$Method,
        [string]$Path,
        [string]$Json,
        [hashtable]$Headers = @{},
        [int[]]$ExpectedStatus = @(200),
        [string]$Name = $Path
    )

    try {
        $response = Invoke-WebRequest `
            -Method $Method `
            -Uri "$BaseUrl$Path" `
            -Headers $Headers `
            -ContentType "application/json; charset=utf-8" `
            -Body $Json `
            -UseBasicParsing

        $statusCode = [int]$response.StatusCode
        $content = $response.Content
    }
    catch [System.Net.WebException] {
        $response = $_.Exception.Response
        if ($null -eq $response) {
            throw
        }

        $statusCode = [int]$response.StatusCode
        $content = Read-ErrorResponseBody $response
    }

    Assert-Status -Actual $statusCode -Expected $ExpectedStatus -Name $Name -Body $content

    if ([string]::IsNullOrWhiteSpace($content)) {
        return $null
    }

    try {
        return $content | ConvertFrom-Json
    }
    catch {
        return $content
    }
}

Write-Step "Health"
Invoke-Api -Method "GET" -Path "/health/db" -ExpectedStatus @(200) -Name "GET /health/db" | Out-Null

Write-Step "Auth"
$stamp = Get-Date -Format "yyyyMMddHHmmss"
$email = "closed-loop-$stamp@example.com"
$password = "Password123!"

$register = Invoke-Api `
    -Method "POST" `
    -Path "/api/auth/register" `
    -ExpectedStatus @(200) `
    -Name "POST auth/register" `
    -Body @{
        email = $email
        username = "closed_loop_$stamp"
        password = $password
        displayName = "Closed Loop Smoke"
    }

$authHeaders = @{ Authorization = "Bearer $($register.accessToken)" }
Invoke-Api -Method "GET" -Path "/api/auth/me" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET auth/me" | Out-Null

Write-Step "Game and character"
$game = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states" `
    -Headers $authHeaders `
    -ExpectedStatus @(200, 201) `
    -Name "POST game-state" `
    -Body @{ name = "Closed loop smoke $stamp" }

$gameStateId = $game.id
if ([string]::IsNullOrWhiteSpace($gameStateId)) {
    throw "Create game-state did not return id."
}

$characterJson = @'
{
  "\u0438\u043c\u044f": "\u0422\u043e\u0440\u0432\u0435\u043d",
  "\u0432\u0438\u0434": "\u0447\u0435\u043b\u043e\u0432\u0435\u043a",
  "\u043a\u043b\u0430\u0441\u0441": "\u0432\u043e\u0438\u043d",
  "\u043f\u0440\u0435\u0434\u044b\u0441\u0442\u043e\u0440\u0438\u044f": "Closed loop smoke character.",
  "\u043e\u043f\u0438\u0441\u0430\u043d\u0438\u0435": "Runtime smoke-test character.",
  "\u0445\u0430\u0440\u0430\u043a\u0442\u0435\u0440\u0438\u0441\u0442\u0438\u043a\u0438": {
    "\u0441\u0438\u043b\u0430": 14,
    "\u043b\u043e\u0432\u043a\u043e\u0441\u0442\u044c": 12,
    "\u0442\u0435\u043b\u043e\u0441\u043b\u043e\u0436\u0435\u043d\u0438\u0435": 13,
    "\u0438\u043d\u0442\u0435\u043b\u043b\u0435\u043a\u0442": 10,
    "\u043c\u0443\u0434\u0440\u043e\u0441\u0442\u044c": 11,
    "\u0445\u0430\u0440\u0438\u0437\u043c\u0430": 10
  },
  "\u0440\u0435\u0441\u0443\u0440\u0441\u044b": {
    "\u0445\u043f\u041c\u0430\u043a\u0441\u0438\u043c\u0443\u043c": 12,
    "\u0445\u043f\u0422\u0435\u043a\u0443\u0449\u0435\u0435": 12
  },
  "\u0431\u043e\u0439": {
    "\u043a\u043b\u0430\u0441\u0441\u0414\u043e\u0441\u043f\u0435\u0445\u0430": 12,
    "\u0431\u043e\u043d\u0443\u0441\u041c\u0430\u0441\u0442\u0435\u0440\u0441\u0442\u0432\u0430": 2
  }
}
'@

$character = Invoke-ApiRawJson `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/characters" `
    -Headers $authHeaders `
    -ExpectedStatus @(200, 201) `
    -Name "POST character" `
    -Json $characterJson

$characterId = $character.id
if ([string]::IsNullOrWhiteSpace($characterId)) {
    throw "Create character did not return id."
}

Write-Step "Bootstrap and status"
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/play/bootstrap" -Headers $authHeaders -ExpectedStatus @(200, 201) -Name "POST play/bootstrap" -Body @{} | Out-Null
$status = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/play/status" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET play/status"
if (-not ($status.PSObject.Properties.Name -contains "scene")) {
    throw "play/status response does not contain scene."
}

Write-Step "Travel"
$location = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/world/locations" `
    -Headers $authHeaders `
    -ExpectedStatus @(201) `
    -Name "POST travel target location" `
    -Body @{
        name = "Closed Loop Target $stamp"
        description = "Target created by closed-loop smoke."
    }

$targetLocationId = $location.id
Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/travel/options" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET travel/options" | Out-Null
$travel = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/play/travel" `
    -Headers $authHeaders `
    -ExpectedStatus @(200) `
    -Name "POST play/travel direct" `
    -Body @{
        targetLocationId = $targetLocationId
        note = "Closed-loop direct/manual travel."
    }

if ($travel.mode -ne "travel" -and $travel.mode -ne "narration") {
    throw "Unexpected travel play state mode: $($travel.mode)"
}

Write-Step "Play act and roll blocker handling"
$act = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/play/act" `
    -Headers $authHeaders `
    -ExpectedStatus @(200, 409, 503) `
    -Name "POST play/act" `
    -Body @{
        message = "I inspect the new location carefully."
        characterId = $characterId
        autoApplySafeChanges = $true
    }

if ($act -ne $null -and $act.mode -eq "awaiting_roll") {
    $requests = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/mechanic-requests?status=pending" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET pending mechanic requests"
    if ($requests -is [array] -and $requests.Count -gt 0) {
        Invoke-Api `
            -Method "POST" `
            -Path "/api/game-states/$gameStateId/play/resolve-and-continue/$($requests[0].id)" `
            -Headers $authHeaders `
            -ExpectedStatus @(200, 400, 404, 409, 503) `
            -Name "POST play/resolve-and-continue" `
            -Body @{
                characterId = $characterId
                roll = 20
                modifier = 99
                note = "Client roll/modifier are diagnostic only."
            } | Out-Null
    }
}

Write-Step "Safe changes"
Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/changes" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET changes" | Out-Null
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/play/apply-safe-changes" -Headers $authHeaders -ExpectedStatus @(200) -Name "POST play/apply-safe-changes" -Body @{} | Out-Null

Write-Step "Play combat"
$combatStart = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/play/combat/start" `
    -Headers $authHeaders `
    -ExpectedStatus @(200, 400) `
    -Name "POST play/combat/start" `
    -Body @{
        participants = @(
            @{
                actorId = $characterId
                armorClass = 12
            }
        )
    }

if ($combatStart -ne $null -and $combatStart.mode -eq "combat") {
    $combatState = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/combat" -Headers $authHeaders -ExpectedStatus @(200, 404) -Name "GET combat"
    $participantId = $null
    if ($combatState -ne $null -and $combatState.participants -is [array] -and $combatState.participants.Count -gt 0) {
        $participantId = $combatState.participants[0].id
    }

    if (-not [string]::IsNullOrWhiteSpace($participantId)) {
        Invoke-Api `
            -Method "POST" `
            -Path "/api/game-states/$gameStateId/play/combat/action" `
            -Headers $authHeaders `
            -ExpectedStatus @(200, 400) `
            -Name "POST play/combat/action attack" `
            -Body @{
                action = "attack"
                attack = @{
                    attackerParticipantId = $participantId
                    targetParticipantId = $participantId
                    attackRoll = "1d20+4"
                    damageRoll = "1d4"
                    damageType = "smoke"
                    reason = "Closed-loop smoke attack."
                }
            } | Out-Null
    }

    Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/play/combat/end" -Headers $authHeaders -ExpectedStatus @(200, 404) -Name "POST play/combat/end" -Body @{} | Out-Null
}

Write-Step "Logout"
Invoke-Api -Method "POST" -Path "/api/auth/logout" -Headers $authHeaders -ExpectedStatus @(200) -Name "POST auth/logout" -Body @{ refreshToken = $register.refreshToken } | Out-Null

Write-Host ""
Write-Host "Closed-loop game smoke test passed." -ForegroundColor Green

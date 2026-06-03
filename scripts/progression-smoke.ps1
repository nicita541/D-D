$ErrorActionPreference = "Stop"

# Run from repository root:
# powershell -ExecutionPolicy Bypass -File ".\scripts\progression-smoke.ps1"

$BaseUrl = "http://localhost:8080"

function Write-Step($Text) {
    Write-Host ""
    Write-Host "== $Text ==" -ForegroundColor Cyan
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
        $params.Body = ($Body | ConvertTo-Json -Depth 80)
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
$email = "progression-smoke-$stamp@example.com"
$username = "progression_smoke_$stamp"
$password = "Password123!"

$register = Invoke-Api `
    -Method "POST" `
    -Path "/api/auth/register" `
    -ExpectedStatus @(200) `
    -Name "POST auth/register" `
    -Body @{
        email = $email
        username = $username
        password = $password
        displayName = "Progression Smoke User"
    }

$login = Invoke-Api `
    -Method "POST" `
    -Path "/api/auth/login" `
    -ExpectedStatus @(200) `
    -Name "POST auth/login" `
    -Body @{
        emailOrUsername = $email
        password = $password
    }

$authHeaders = @{ Authorization = "Bearer $($login.accessToken)" }
Invoke-Api -Method "GET" -Path "/api/auth/me" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET auth/me" | Out-Null

Write-Step "Game and character"
$game = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states" `
    -Headers $authHeaders `
    -ExpectedStatus @(200, 201) `
    -Name "POST game-states" `
    -Body @{
        name = "Progression smoke $stamp"
    }

$gameStateId = $game.id
if ([string]::IsNullOrWhiteSpace($gameStateId)) {
    throw "Create game-state did not return id."
}

$characterJson = @'
{
  "\u0438\u043c\u044f": "\u0422\u043e\u0440\u0432\u0435\u043d",
  "\u0432\u0438\u0434": "\u0447\u0435\u043b\u043e\u0432\u0435\u043a",
  "\u043a\u043b\u0430\u0441\u0441": "\u0432\u043e\u0438\u043d",
  "\u043f\u0440\u0435\u0434\u044b\u0441\u0442\u043e\u0440\u0438\u044f": "Progression smoke test character.",
  "\u043e\u043f\u0438\u0441\u0430\u043d\u0438\u0435": "Character for progression smoke.",
  "\u043c\u0438\u0440\u043e\u0432\u043e\u0437\u0437\u0440\u0435\u043d\u0438\u0435": "neutral good",
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
    "\u0445\u043f\u0422\u0435\u043a\u0443\u0449\u0435\u0435": 12,
    "\u043c\u0430\u043d\u0430\u041c\u0430\u043a\u0441\u0438\u043c\u0443\u043c": 0,
    "\u043c\u0430\u043d\u0430\u0422\u0435\u043a\u0443\u0449\u0430\u044f": 0,
    "\u043e\u0447\u043a\u0438\u0414\u0435\u0439\u0441\u0442\u0432\u0438\u0439\u041c\u0430\u043a\u0441\u0438\u043c\u0443\u043c": 1,
    "\u043e\u0447\u043a\u0438\u0414\u0435\u0439\u0441\u0442\u0432\u0438\u0439\u0422\u0435\u043a\u0443\u0449\u0438\u0435": 1
  },
  "\u0431\u043e\u0439": {
    "\u043a\u043b\u0430\u0441\u0441\u0414\u043e\u0441\u043f\u0435\u0445\u0430": 12,
    "\u0431\u043e\u043d\u0443\u0441\u041c\u0430\u0441\u0442\u0435\u0440\u0441\u0442\u0432\u0430": 2,
    "\u0432\u0411\u043e\u044e": false
  }
}
'@

$character = Invoke-ApiRawJson `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/characters" `
    -Headers $authHeaders `
    -ExpectedStatus @(200, 201) `
    -Name "POST characters" `
    -Json $characterJson

$characterId = $character.id
if ([string]::IsNullOrWhiteSpace($characterId)) {
    throw "Create character did not return id."
}

Write-Step "Progression"
$initial = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/characters/$characterId/progression" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET progression initial"
if ($initial.level -ne 1 -or $initial.levelUpAvailable -ne $false) {
    throw "Initial progression did not start at level 1 without level-up availability."
}

$below = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/characters/$characterId/xp" `
    -Headers $authHeaders `
    -ExpectedStatus @(200) `
    -Name "POST xp below threshold" `
    -Body @{
        amount = 299
        reason = "Progression smoke below threshold."
    }
if ($below.levelUpAvailable -ne $false) {
    throw "XP below threshold should not set levelUpAvailable."
}

$ready = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/characters/$characterId/xp" `
    -Headers $authHeaders `
    -ExpectedStatus @(200) `
    -Name "POST xp reaches threshold" `
    -Body @{
        amount = 1
        reason = "Progression smoke reaches level 2 threshold."
    }
if ($ready.levelUpAvailable -ne $true) {
    throw "XP at threshold should set levelUpAvailable."
}

$levelUp = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/characters/$characterId/level-up" `
    -Headers $authHeaders `
    -ExpectedStatus @(200) `
    -Name "POST level-up" `
    -Body @{
        hpMaxAdd = 5
        note = "Progression smoke level-up."
    }
if ($levelUp.newLevel -ne 2 -or $levelUp.proficiencyBonus -ne 2 -or $levelUp.hpIncrease -ne 5) {
    throw "Level-up response did not contain expected level/proficiency/hp increase."
}

Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/characters/$characterId/level-up" `
    -Headers $authHeaders `
    -ExpectedStatus @(400) `
    -Name "POST duplicate level-up without enough XP" `
    -Body @{
        hpMaxAdd = 5
    } | Out-Null

Write-Step "Play state"
$status = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/play/status" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET play/status"
if (-not ($status.PSObject.Properties.Name -contains "progression") -or $null -eq $status.progression) {
    throw "play/status did not include progression."
}
if ($status.progression.level -ne 2) {
    throw "play/status progression level should be 2."
}

Write-Step "Logout"
Invoke-Api -Method "POST" -Path "/api/auth/logout" -Headers $authHeaders -ExpectedStatus @(200) -Name "POST auth/logout" -Body @{ refreshToken = $login.refreshToken } | Out-Null

Write-Host ""
Write-Host "Progression smoke test passed." -ForegroundColor Green

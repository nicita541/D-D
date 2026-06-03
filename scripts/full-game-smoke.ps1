$ErrorActionPreference = "Stop"

# Run from repository root:
# powershell -ExecutionPolicy Bypass -File ".\scripts\full-game-smoke.ps1"

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

    $url = "$BaseUrl$Path"
    $params = @{
        Method = $Method
        Uri = $url
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

function To-Array {
    param($Value)

    if ($null -eq $Value) {
        return @()
    }

    if ($Value -is [System.Array]) {
        return $Value
    }

    return @($Value)
}

Write-Step "Health"
Invoke-Api -Method "GET" -Path "/health/db" -ExpectedStatus @(200) -Name "GET /health/db" | Out-Null

Write-Step "Auth"
$stamp = Get-Date -Format "yyyyMMddHHmmss"
$email = "full-smoke-$stamp@example.com"
$username = "full_smoke_$stamp"
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
        displayName = "Full Smoke User"
    }

if ([string]::IsNullOrWhiteSpace($register.accessToken)) {
    throw "register did not return accessToken."
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

$refresh = Invoke-Api `
    -Method "POST" `
    -Path "/api/auth/refresh" `
    -ExpectedStatus @(200) `
    -Name "POST auth/refresh" `
    -Body @{
        refreshToken = $login.refreshToken
    }

$token = $refresh.accessToken
if ([string]::IsNullOrWhiteSpace($token)) {
    throw "refresh did not return accessToken."
}

$authHeaders = @{
    Authorization = "Bearer $token"
}

Invoke-Api -Method "GET" -Path "/api/auth/me" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET auth/me" | Out-Null

Write-Step "Create game and character"
$game = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states" `
    -Headers $authHeaders `
    -ExpectedStatus @(200, 201) `
    -Name "POST game-states" `
    -Body @{
        name = "Full smoke game $stamp"
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
  "\u043f\u0440\u0435\u0434\u044b\u0441\u0442\u043e\u0440\u0438\u044f": "Full smoke test mercenary.",
  "\u043e\u043f\u0438\u0441\u0430\u043d\u0438\u0435": "Full game smoke-test character.",
  "\u043c\u0438\u0440\u043e\u0432\u043e\u0437\u0437\u0440\u0435\u043d\u0438\u0435": "neutral good",
  "\u0445\u0430\u0440\u0430\u043a\u0442\u0435\u0440\u0438\u0441\u0442\u0438\u043a\u0438": {
    "\u0441\u0438\u043b\u0430": 14,
    "\u043b\u043e\u0432\u043a\u043e\u0441\u0442\u044c": 12,
    "\u0442\u0435\u043b\u043e\u0441\u043b\u043e\u0436\u0435\u043d\u0438\u0435": 13,
    "\u0438\u043d\u0442\u0435\u043b\u043b\u0435\u043a\u0442": 10,
    "\u043c\u0443\u0434\u0440\u043e\u0441\u0442\u044c": 11,
    "\u0445\u0430\u0440\u0438\u0437\u043c\u0430": 10,
    "\u0438\u043d\u0438\u0446\u0438\u0430\u0442\u0438\u0432\u0430": 1,
    "\u0441\u043a\u043e\u0440\u043e\u0441\u0442\u044c": 9,
    "\u0432\u043e\u0441\u043f\u0440\u0438\u044f\u0442\u0438\u0435": 11
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
    "\u0432\u0411\u043e\u044e": false,
    "\u0431\u0440\u043e\u0441\u043e\u043a\u0418\u043d\u0438\u0446\u0438\u0430\u0442\u0438\u0432\u044b": 0
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

Write-Step "Play status and bootstrap"
$status = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/play/status" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET play/status initial"
if (-not ($status.PSObject.Properties.Name -contains "scene")) {
    throw "play/status initial response does not contain scene."
}

Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/play/bootstrap" -Headers $authHeaders -ExpectedStatus @(200, 201) -Name "POST play/bootstrap" -Body @{} | Out-Null

$bootstrappedStatus = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/play/status" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET play/status after bootstrap"
if (-not ($bootstrappedStatus.PSObject.Properties.Name -contains "scene")) {
    throw "play/status after bootstrap response does not contain scene."
}

Write-Step "Inventory"
Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/characters/$characterId/inventory" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET inventory" | Out-Null

$weapon = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/characters/$characterId/inventory/items" `
    -Headers $authHeaders `
    -ExpectedStatus @(201) `
    -Name "POST weapon item" `
    -Body @{
        name = "Full Smoke Sword"
        description = "Weapon created by full-game smoke test."
        itemType = "weapon"
        quantity = 1
        weight = 1
        slot = "main_hand"
        properties = @{}
    }

$weaponId = $weapon.id

$potion = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/characters/$characterId/inventory/items" `
    -Headers $authHeaders `
    -ExpectedStatus @(201) `
    -Name "POST consumable item" `
    -Body @{
        name = "Full Smoke Potion"
        description = "Consumable created by full-game smoke test."
        itemType = "consumable"
        quantity = 1
        weight = 0
        properties = @{
            effect = "heal"
            amount = 1
        }
    }

$potionId = $potion.id
if ([string]::IsNullOrWhiteSpace($weaponId) -or [string]::IsNullOrWhiteSpace($potionId)) {
    throw "Inventory item creation did not return ids."
}

Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/characters/$characterId/inventory/items/$weaponId/equip" -Headers $authHeaders -ExpectedStatus @(200) -Name "POST equip item" -Body @{} | Out-Null
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/characters/$characterId/inventory/items/$weaponId/unequip" -Headers $authHeaders -ExpectedStatus @(200) -Name "POST unequip item" -Body @{} | Out-Null
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/characters/$characterId/inventory/items/$potionId/use" -Headers $authHeaders -ExpectedStatus @(200) -Name "POST use item" -Body @{} | Out-Null

Write-Step "Play loop"
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/play/start" -Headers $authHeaders -ExpectedStatus @(200, 409, 503) -Name "POST play/start" -Body @{} | Out-Null

$requests = To-Array (Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/mechanic-requests?status=pending" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET mechanic-requests")
$abilityRequest = $requests | Where-Object { $_.requestType -eq "ability_check" } | Select-Object -First 1
if ($null -ne $abilityRequest -and -not [string]::IsNullOrWhiteSpace($abilityRequest.id)) {
    Invoke-Api `
        -Method "POST" `
        -Path "/api/game-states/$gameStateId/play/resolve-mechanic-request/$($abilityRequest.id)" `
        -Headers $authHeaders `
        -ExpectedStatus @(200) `
        -Name "POST play/resolve-mechanic-request" `
        -Body @{
            characterId = $characterId
        } | Out-Null

    Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/play/continue" -Headers $authHeaders -ExpectedStatus @(200, 409, 503) -Name "POST play/continue" -Body @{} | Out-Null
}

Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/changes" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET changes" | Out-Null
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/play/apply-safe-changes" -Headers $authHeaders -ExpectedStatus @(200) -Name "POST play/apply-safe-changes" -Body @{} | Out-Null
Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/play/status" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET play/status final" | Out-Null
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/play/message" -Headers $authHeaders -ExpectedStatus @(200, 409, 503) -Name "POST play/message" -Body @{ message = "I check the road and prepare to move on." } | Out-Null
Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/turns" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET turns" | Out-Null

Write-Step "Logout"
Invoke-Api `
    -Method "POST" `
    -Path "/api/auth/logout" `
    -Headers $authHeaders `
    -ExpectedStatus @(200) `
    -Name "POST auth/logout" `
    -Body @{
        refreshToken = $refresh.refreshToken
    } | Out-Null

Write-Step "Done"
Write-Host "Full game smoke test passed." -ForegroundColor Green

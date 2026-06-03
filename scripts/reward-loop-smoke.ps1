$ErrorActionPreference = "Stop"

# Run from repository root:
# powershell -ExecutionPolicy Bypass -File ".\scripts\reward-loop-smoke.ps1"

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
$email = "reward-smoke-$stamp@example.com"
$username = "reward_smoke_$stamp"
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
        displayName = "Reward Smoke User"
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

$authHeaders = @{ Authorization = "Bearer $($refresh.accessToken)" }
Invoke-Api -Method "GET" -Path "/api/auth/me" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET auth/me" | Out-Null

Write-Step "Game and character"
$game = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states" `
    -Headers $authHeaders `
    -ExpectedStatus @(200, 201) `
    -Name "POST game-states" `
    -Body @{
        name = "Reward loop smoke $stamp"
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
  "\u043f\u0440\u0435\u0434\u044b\u0441\u0442\u043e\u0440\u0438\u044f": "Reward loop smoke test mercenary.",
  "\u043e\u043f\u0438\u0441\u0430\u043d\u0438\u0435": "Reward loop smoke-test character.",
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

Write-Step "Monster and combat outcome"
$monster = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/monsters" `
    -Headers $authHeaders `
    -ExpectedStatus @(201) `
    -Name "POST monsters" `
    -Body @{
        name = "Reward Smoke Goblin"
        monsterType = "goblin"
        description = "Monster created by reward-loop smoke test."
        hpCurrent = 0
        hpMax = 5
        armorClass = 10
        isAlive = $true
        status = "alive"
        xpReward = 25
        currencyReward = 4
        loot = @(
            @{
                name = "Goblin Token"
                description = "Small token from reward-loop smoke test."
                quantity = 1
                itemType = "misc"
                rarity = "common"
            }
        )
    }

$monsterId = $monster.id
if ([string]::IsNullOrWhiteSpace($monsterId)) {
    throw "Create monster did not return id."
}

Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/monsters/$monsterId" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET monster" | Out-Null

Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/combat/start" `
    -Headers $authHeaders `
    -ExpectedStatus @(201) `
    -Name "POST combat/start" `
    -Body @{
        participants = @(
            @{
                actorType = "character"
                actorId = $characterId
                name = "Torven"
                initiative = 12
                hpCurrent = 12
                hpMax = 12
                armorClass = 12
                isEnemy = $false
            },
            @{
                monsterId = $monsterId
                initiative = 3
                hpCurrent = 0
                hpMax = 5
                armorClass = 10
                isEnemy = $true
            }
        )
    } | Out-Null

$outcome = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/combat/outcome" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET combat/outcome"
if ($outcome.status -ne "victory") {
    throw "Expected combat outcome victory, got '$($outcome.status)'."
}

$resolved = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/play/combat/resolve-outcome" `
    -Headers $authHeaders `
    -ExpectedStatus @(200) `
    -Name "POST play/combat/resolve-outcome" `
    -Body @{
        autoGrantRewards = $true
        note = "Reward-loop smoke resolved a defeated monster."
    }

if ($null -eq $resolved -or -not ($resolved.PSObject.Properties.Name -contains "loot")) {
    throw "Resolved play state did not contain loot field."
}

Write-Step "Loot and currency"
$loot = To-Array (Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/loot" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET loot")
$container = $loot | Where-Object { $_.status -eq "available" } | Select-Object -First 1
if ($null -eq $container -or [string]::IsNullOrWhiteSpace($container.id)) {
    throw "No available loot container was created."
}

Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/loot/$($container.id)" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET loot container" | Out-Null
Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/loot/$($container.id)/claim" `
    -Headers $authHeaders `
    -ExpectedStatus @(200) `
    -Name "POST loot claim" `
    -Body @{
        characterId = $characterId
    } | Out-Null

Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/characters/$characterId/inventory" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET inventory after loot" | Out-Null

$currency = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/characters/$characterId/currency" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET currency"
if ($currency.gold -lt 4) {
    throw "Expected at least 4 gold after auto-granted combat reward."
}

Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/characters/$characterId/currency/add" `
    -Headers $authHeaders `
    -ExpectedStatus @(200) `
    -Name "POST currency/add" `
    -Body @{
        amount = 1
        reason = "Reward-loop smoke add currency."
    } | Out-Null

Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/characters/$characterId/currency/spend" `
    -Headers $authHeaders `
    -ExpectedStatus @(200) `
    -Name "POST currency/spend" `
    -Body @{
        amount = 1
        reason = "Reward-loop smoke spend currency."
    } | Out-Null

Write-Step "XP and quest reward"
Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/characters/$characterId/xp" `
    -Headers $authHeaders `
    -ExpectedStatus @(200) `
    -Name "POST character/xp" `
    -Body @{
        experience = 5
        reason = "Reward-loop smoke XP alias."
    } | Out-Null

$quest = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/world/quests" `
    -Headers $authHeaders `
    -ExpectedStatus @(201) `
    -Name "POST world/quests" `
    -Body @{
        title = "Reward Smoke Quest"
        description = "Quest created by reward-loop smoke test."
        status = "active"
        rewardExperience = 10
        rewardGold = 2
    }

$questId = $quest.id
if ([string]::IsNullOrWhiteSpace($questId)) {
    throw "Create quest did not return id."
}

Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/quests/$questId/complete" -Headers $authHeaders -ExpectedStatus @(200) -Name "POST quest complete" | Out-Null
Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/quests/$questId/rewards/grant" `
    -Headers $authHeaders `
    -ExpectedStatus @(200) `
    -Name "POST quest rewards/grant" `
    -Body @{
        characterId = $characterId
    } | Out-Null

Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/quests/$questId/rewards/grant" `
    -Headers $authHeaders `
    -ExpectedStatus @(409) `
    -Name "POST quest rewards/grant duplicate" `
    -Body @{
        characterId = $characterId
    } | Out-Null

Write-Step "Play state and logout"
Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/play/status" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET play/status" | Out-Null
Invoke-Api -Method "POST" -Path "/api/auth/logout" -Headers $authHeaders -ExpectedStatus @(200) -Name "POST auth/logout" -Body @{ refreshToken = $refresh.refreshToken } | Out-Null

Write-Host ""
Write-Host "Reward loop smoke test passed." -ForegroundColor Green

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

    $url = "$BaseUrl$Path"

    try {
        $response = Invoke-WebRequest `
            -Method $Method `
            -Uri $url `
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
function Assert-RouteExists {
    param(
        [object]$Swagger,
        [string]$Route
    )

    if (-not $Swagger.paths.PSObject.Properties.Name.Contains($Route)) {
        throw "Swagger route missing: $Route"
    }

    Write-Host "OK route - $Route" -ForegroundColor Green
}

function Assert-NoRoute {
    param(
        [object]$Swagger,
        [string]$Route
    )

    if ($Swagger.paths.PSObject.Properties.Name.Contains($Route)) {
        throw "Forbidden old route exists: $Route"
    }

    Write-Host "OK route absent - $Route" -ForegroundColor Green
}

Write-Step "Health"
Invoke-Api -Method "GET" -Path "/health" -ExpectedStatus @(200) -Name "GET /health" | Out-Null
Invoke-Api -Method "GET" -Path "/health/db" -ExpectedStatus @(200) -Name "GET /health/db" | Out-Null

Write-Step "Swagger routes"
$swagger = Invoke-Api -Method "GET" -Path "/swagger/v1/swagger.json" -ExpectedStatus @(200) -Name "GET swagger"

$importantRoutes = @(
    "/health",
    "/health/db",

    "/api/auth/register",
    "/api/auth/login",
    "/api/auth/refresh",
    "/api/auth/logout",
    "/api/auth/me",

    "/api/campaigns",
    "/api/campaigns/{id}",

    "/api/game-states",
    "/api/game-states/{gameStateId}",

    "/api/game-states/{gameStateId}/characters",
    "/api/game-states/{gameStateId}/characters/{characterId}",

    "/api/game-states/{gameStateId}/ai-context",

    "/api/game-states/{gameStateId}/play/start",
    "/api/game-states/{gameStateId}/play/message",

    "/api/game-states/{gameStateId}/turns",
    "/api/game-states/{gameStateId}/turns/{turnId}",

    "/api/game-states/{gameStateId}/world/locations",
    "/api/game-states/{gameStateId}/world/locations/{locationId}",
    "/api/game-states/{gameStateId}/world/objects",
    "/api/game-states/{gameStateId}/world/objects/{objectId}",

    "/api/game-states/{gameStateId}/memory",
    "/api/game-states/{gameStateId}/memory/summarize",

    "/api/game-states/{gameStateId}/changes",
    "/api/game-states/{gameStateId}/mechanic-requests",

    "/api/game-states/{gameStateId}/combat"
)

foreach ($route in $importantRoutes) {
    Assert-RouteExists -Swagger $swagger -Route $route
}

Assert-NoRoute -Swagger $swagger -Route "/api/players"

Write-Step "Unauthorized protection"
Invoke-Api -Method "GET" -Path "/api/game-states" -ExpectedStatus @(401) -Name "GET /api/game-states without token" | Out-Null

Write-Step "Auth register/login/refresh/me"
$stamp = Get-Date -Format "yyyyMMddHHmmss"
$email = "api-smoke-$stamp@example.com"
$username = "api_smoke_$stamp"
$password = "Password123!"

$register = Invoke-Api `
    -Method "POST" `
    -Path "/api/auth/register" `
    -ExpectedStatus @(200) `
    -Name "POST /api/auth/register" `
    -Body @{
        email = $email
        username = $username
        password = $password
        displayName = "API Smoke User"
    }

if ([string]::IsNullOrWhiteSpace($register.accessToken)) {
    throw "register did not return accessToken."
}

if ([string]::IsNullOrWhiteSpace($register.refreshToken)) {
    throw "register did not return refreshToken."
}

$login = Invoke-Api `
    -Method "POST" `
    -Path "/api/auth/login" `
    -ExpectedStatus @(200) `
    -Name "POST /api/auth/login" `
    -Body @{
        emailOrUsername = $email
        password = $password
    }

if ([string]::IsNullOrWhiteSpace($login.accessToken)) {
    throw "login did not return accessToken."
}

$refresh = Invoke-Api `
    -Method "POST" `
    -Path "/api/auth/refresh" `
    -ExpectedStatus @(200) `
    -Name "POST /api/auth/refresh" `
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

$me = Invoke-Api `
    -Method "GET" `
    -Path "/api/auth/me" `
    -Headers $authHeaders `
    -ExpectedStatus @(200) `
    -Name "GET /api/auth/me"

Write-Host "Current account: $($me.email)" -ForegroundColor Green

Write-Step "Campaigns public read"
Invoke-Api -Method "GET" -Path "/api/campaigns" -ExpectedStatus @(200) -Name "GET /api/campaigns" | Out-Null

Write-Step "GameState create/list/get"
$game = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states" `
    -Headers $authHeaders `
    -ExpectedStatus @(200, 201) `
    -Name "POST /api/game-states" `
    -Body @{
        name = "API smoke game $stamp"
    }

$gameStateId = $game.id
if ([string]::IsNullOrWhiteSpace($gameStateId)) {
    throw "Create game-state did not return id."
}

Invoke-Api -Method "GET" -Path "/api/game-states" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET /api/game-states" | Out-Null
Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET /api/game-states/{id}" | Out-Null

Write-Step "Characters create/list/get/update"

$characterJson = @'
{
  "\u0438\u043c\u044f": "\u0422\u043e\u0440\u0432\u0435\u043d",
  "\u0432\u0438\u0434": "\u0447\u0435\u043b\u043e\u0432\u0435\u043a",
  "\u043a\u043b\u0430\u0441\u0441": "\u0432\u043e\u0438\u043d",
  "\u043f\u0440\u0435\u0434\u044b\u0441\u0442\u043e\u0440\u0438\u044f": "Smoke test mercenary.",
  "\u043e\u043f\u0438\u0441\u0430\u043d\u0438\u0435": "Runtime smoke-test character.",
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
    -Name "POST /api/game-states/{id}/characters" `
    -Json $characterJson

$characterId = $character.id
if ([string]::IsNullOrWhiteSpace($characterId)) {
    throw "Create character did not return id."
}

Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/characters" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET characters" | Out-Null
Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/characters/$characterId" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET character by id" | Out-Null

$updateCharacterJson = @'
{
  "\u0438\u043c\u044f": "\u0422\u043e\u0440\u0432\u0435\u043d Smoke Updated",
  "\u0432\u0438\u0434": "\u0447\u0435\u043b\u043e\u0432\u0435\u043a",
  "\u043a\u043b\u0430\u0441\u0441": "\u0432\u043e\u0438\u043d",
  "\u043e\u043f\u0438\u0441\u0430\u043d\u0438\u0435": "Updated by API smoke test."
}
'@

Invoke-ApiRawJson `
    -Method "PUT" `
    -Path "/api/game-states/$gameStateId/characters/$characterId" `
    -Headers $authHeaders `
    -ExpectedStatus @(200) `
    -Name "PUT character" `
    -Json $updateCharacterJson | Out-Null

Write-Step "AI context"
Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/ai-context" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET ai-context" | Out-Null

Write-Step "World API"
$locations = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/world/locations" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET world locations"

$location = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/world/locations" `
    -Headers $authHeaders `
    -ExpectedStatus @(201) `
    -Name "POST world location" `
    -Body @{
        name = "Smoke Location $stamp"
        description = "Location created by smoke test."
    }

$locationId = $location.id
if ([string]::IsNullOrWhiteSpace($locationId)) {
    throw "Create location did not return id."
}

Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/world/locations/$locationId" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET world location" | Out-Null

$worldObject = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/world/objects" `
    -Headers $authHeaders `
    -ExpectedStatus @(201) `
    -Name "POST world object" `
    -Body @{
        locationId = $locationId
        name = "Smoke Object $stamp"
        objectType = "note"
        description = "Object created by smoke test."
        state = "normal"
        tags = @("smoke")
    }

$objectId = $worldObject.id
if ([string]::IsNullOrWhiteSpace($objectId)) {
    throw "Create world object did not return id."
}

Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/world/objects" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET world objects" | Out-Null

Write-Step "Campaign memory"
Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/memory" -Headers $authHeaders -ExpectedStatus @(200, 404) -Name "GET memory" | Out-Null

Write-Step "Mechanic requests and changes"
Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/mechanic-requests" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET mechanic requests" | Out-Null
Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/changes" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET changes" | Out-Null

Write-Step "Play start/message"
$start = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/play/start" `
    -Headers $authHeaders `
    -ExpectedStatus @(200, 503) `
    -Name "POST play/start" `
    -Body @{}

if ($start -is [string]) {
    Write-Host "play/start returned raw content."
}

$message = Invoke-Api `
    -Method "POST" `
    -Path "/api/game-states/$gameStateId/play/message" `
    -Headers $authHeaders `
    -ExpectedStatus @(200, 409, 503) `
    -Name "POST play/message" `
    -Body @{
        message = "I look around and try to find someone to talk to."
    }

Write-Step "Turns"
$turns = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/turns" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET turns"

if ($turns -is [array] -and $turns.Count -gt 0) {
    $turnId = $turns[0].id
    if (-not [string]::IsNullOrWhiteSpace($turnId)) {
        Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/turns/$turnId" -Headers $authHeaders -ExpectedStatus @(200) -Name "GET turn by id" | Out-Null
    }
}

Write-Step "Combat read"
Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/combat" -Headers $authHeaders -ExpectedStatus @(200, 404) -Name "GET combat" | Out-Null

Write-Step "Logout"
Invoke-Api `
    -Method "POST" `
    -Path "/api/auth/logout" `
    -Headers $authHeaders `
    -ExpectedStatus @(200) `
    -Name "POST /api/auth/logout" `
    -Body @{
        refreshToken = $refresh.refreshToken
    } | Out-Null

Write-Step "Done"
Write-Host "Critical API smoke test passed." -ForegroundColor Green
Write-Host "GameStateId: $gameStateId"
Write-Host "CharacterId: $characterId"

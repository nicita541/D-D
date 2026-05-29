$ErrorActionPreference = "Stop"

$BaseUrl = "http://localhost:8080"

function Write-Step($Text) {
    Write-Host ""
    Write-Host "== $Text ==" -ForegroundColor Cyan
}

function Invoke-Json {
    param(
        [string]$Method,
        [string]$Url,
        [object]$Body = $null,
        [hashtable]$Headers = @{}
    )

    $params = @{
        Method = $Method
        Uri = $Url
        Headers = $Headers
    }

    if ($null -ne $Body) {
        $params.ContentType = "application/json; charset=utf-8"
        $params.Body = ($Body | ConvertTo-Json -Depth 30)
    }

    return Invoke-RestMethod @params
}

function Invoke-RawJson {
    param(
        [string]$Method,
        [string]$Url,
        [string]$Json,
        [hashtable]$Headers = @{}
    )

    return Invoke-RestMethod `
        -Method $Method `
        -Uri $Url `
        -Headers $Headers `
        -ContentType "application/json; charset=utf-8" `
        -Body $Json
}

Write-Step "Health"
Invoke-RestMethod "$BaseUrl/health" | ConvertTo-Json -Depth 10
Invoke-RestMethod "$BaseUrl/health/db" | ConvertTo-Json -Depth 10

$stamp = Get-Date -Format "yyyyMMddHHmmss"
$email = "solo-smoke-$stamp@example.com"
$username = "solo_smoke_$stamp"
$password = "Password123!"

Write-Step "Register"
$register = Invoke-Json `
    -Method "POST" `
    -Url "$BaseUrl/api/auth/register" `
    -Body @{
        email = $email
        username = $username
        password = $password
        displayName = "Solo Smoke User"
    }

$token = $register.accessToken
if ([string]::IsNullOrWhiteSpace($token)) {
    throw "Register did not return accessToken."
}

$authHeaders = @{
    Authorization = "Bearer $token"
}

Write-Host "Registered: $email" -ForegroundColor Green

Write-Step "Create game-state"
$game = Invoke-Json `
    -Method "POST" `
    -Url "$BaseUrl/api/game-states" `
    -Headers $authHeaders `
    -Body @{
        name = "Solo smoke game $stamp"
    }

$gameStateId = $game.id
if ([string]::IsNullOrWhiteSpace($gameStateId)) {
    throw "Create game-state did not return id."
}

Write-Host "GameStateId: $gameStateId" -ForegroundColor Green

Write-Step "Create character"

$characterJson = @'
{
  "\u0438\u043c\u044f": "\u0422\u043e\u0440\u0432\u0435\u043d",
  "\u0432\u0438\u0434": "\u0447\u0435\u043b\u043e\u0432\u0435\u043a",
  "\u043a\u043b\u0430\u0441\u0441": "\u0432\u043e\u0438\u043d",
  "\u043f\u0440\u0435\u0434\u044b\u0441\u0442\u043e\u0440\u0438\u044f": "\u041d\u0430\u0435\u043c\u043d\u0438\u043a, \u0438\u0449\u0443\u0449\u0438\u0439 \u0440\u0430\u0431\u043e\u0442\u0443 \u0438 \u0441\u043b\u0430\u0432\u0443.",
  "\u043e\u043f\u0438\u0441\u0430\u043d\u0438\u0435": "\u041e\u0441\u0442\u043e\u0440\u043e\u0436\u043d\u044b\u0439, \u043d\u043e \u0440\u0435\u0448\u0438\u0442\u0435\u043b\u044c\u043d\u044b\u0439 \u0438\u0441\u043a\u0430\u0442\u0435\u043b\u044c \u043f\u0440\u0438\u043a\u043b\u044e\u0447\u0435\u043d\u0438\u0439.",
  "\u043c\u0438\u0440\u043e\u0432\u043e\u0437\u0437\u0440\u0435\u043d\u0438\u0435": "\u043d\u0435\u0439\u0442\u0440\u0430\u043b\u044c\u043d\u044b\u0439 \u0434\u043e\u0431\u0440\u044b\u0439",
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

$character = Invoke-RawJson `
    -Method "POST" `
    -Url "$BaseUrl/api/game-states/$gameStateId/characters" `
    -Headers $authHeaders `
    -Json $characterJson

$characterId = $character.id
if ([string]::IsNullOrWhiteSpace($characterId)) {
    throw "Create character did not return id."
}

Write-Host "CharacterId: $characterId" -ForegroundColor Green

Write-Step "Play start"
$start = Invoke-Json `
    -Method "POST" `
    -Url "$BaseUrl/api/game-states/$gameStateId/play/start" `
    -Headers $authHeaders `
    -Body @{}

$start | ConvertTo-Json -Depth 30

Write-Step "Play message"
$message = Invoke-Json `
    -Method "POST" `
    -Url "$BaseUrl/api/game-states/$gameStateId/play/message" `
    -Headers $authHeaders `
    -Body @{
        message = "I look around and try to find someone to talk to."
    }

$message | ConvertTo-Json -Depth 30

Write-Step "Done"
Write-Host "Solo gameplay smoke test passed." -ForegroundColor Green
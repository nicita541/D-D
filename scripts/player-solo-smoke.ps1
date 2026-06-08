param(
    [string]$BaseUrl = "http://localhost:8080"
)

$ErrorActionPreference = "Stop"

function Write-Step($Text) {
    Write-Host ""
    Write-Host "== $Text ==" -ForegroundColor Cyan
}

function Read-ErrorResponseBody($Response) {
    if ($null -eq $Response) { return "" }
    try {
        $stream = $Response.GetResponseStream()
        if ($null -eq $stream) { return "" }
        $reader = New-Object System.IO.StreamReader($stream)
        return $reader.ReadToEnd()
    }
    catch {
        return ""
    }
}

function Assert-Status {
    param([int]$Actual, [int[]]$Expected, [string]$Name, [string]$Body = "")
    if ($Expected -notcontains $Actual) {
        Write-Host "FAILED: $Name" -ForegroundColor Red
        Write-Host "Expected: $($Expected -join ', '), Actual: $Actual" -ForegroundColor Red
        if (-not [string]::IsNullOrWhiteSpace($Body)) { Write-Host $Body }
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
        $params.Body = ($Body | ConvertTo-Json -Depth 50)
    }

    try {
        $response = Invoke-WebRequest @params
        $statusCode = [int]$response.StatusCode
        $content = $response.Content
    }
    catch [System.Net.WebException] {
        $response = $_.Exception.Response
        if ($null -eq $response) { throw }
        $statusCode = [int]$response.StatusCode
        $content = Read-ErrorResponseBody $response
    }

    Assert-Status -Actual $statusCode -Expected $ExpectedStatus -Name $Name -Body $content
    if ([string]::IsNullOrWhiteSpace($content)) { return $null }
    return $content | ConvertFrom-Json
}

Write-Step "Health"
Invoke-Api -Method "GET" -Path "/health/db" -ExpectedStatus @(200) -Name "GET /health/db" | Out-Null

$stamp = Get-Date -Format "yyyyMMddHHmmss"
$auth = Invoke-Api `
    -Method "POST" `
    -Path "/api/auth/register" `
    -ExpectedStatus @(200) `
    -Name "register solo user" `
    -Body @{
        email = "solo-$stamp@example.com"
        username = "solo_$stamp"
        password = "Password123!"
        displayName = "Solo smoke"
    }
$headers = @{ Authorization = "Bearer $($auth.accessToken)" }

Write-Step "Generate account character"
$character = Invoke-Api -Method "POST" -Path "/api/characters/generate" -Headers $headers -ExpectedStatus @(200) -Name "generate character" -Body @{
    name = "Solo Hero $stamp"
    className = "воин"
}
if ([string]::IsNullOrWhiteSpace([string]$character.id)) { throw "generated character did not return id." }

$characters = Invoke-Api -Method "GET" -Path "/api/characters" -Headers $headers -ExpectedStatus @(200) -Name "list account characters"
if (($characters | Measure-Object).Count -lt 1) { throw "character list is empty." }

Write-Step "Choose story and start solo session"
$campaigns = Invoke-Api -Method "GET" -Path "/api/campaigns" -Headers $headers -ExpectedStatus @(200) -Name "list campaigns"
if (($campaigns | Measure-Object).Count -lt 1) { throw "no seeded campaign templates found." }
$campaignId = [string]$campaigns[0].id

$session = Invoke-Api -Method "POST" -Path "/api/game-sessions/start" -Headers $headers -ExpectedStatus @(200) -Name "start solo session" -Body @{
    accountCharacterId = [string]$character.id
    campaignTemplateId = $campaignId
    mode = "solo"
}
$gameStateId = [string]$session.gameStateId
$characterId = [string]$session.characterId
if ([string]::IsNullOrWhiteSpace($gameStateId)) { throw "session did not return gameStateId." }
if ([string]::IsNullOrWhiteSpace($characterId)) { throw "session did not return characterId." }

$status = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/play/status" -Headers $headers -ExpectedStatus @(200) -Name "play status"
if (-not $status.permissions.canPlay) { throw "solo host cannot play." }
if ([string]$status.currentPartyMember.characterId -ne $characterId) { throw "solo party member is not assigned to imported character." }

Write-Step "Start story and act"
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/play/bootstrap" -Headers $headers -ExpectedStatus @(200, 201, 409, 503) -Name "bootstrap" -Body @{} | Out-Null
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/play/start" -Headers $headers -ExpectedStatus @(200, 409, 503) -Name "first master message" -Body @{} | Out-Null
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/play/act" -Headers $headers -ExpectedStatus @(200, 409, 503) -Name "player action" -Body @{
    characterId = $characterId
    message = "I look around and ask the game master what I notice first."
    autoApplySafeChanges = $true
} | Out-Null

Write-Step "Verify account character remains portable"
$updated = Invoke-Api -Method "GET" -Path "/api/characters/$($character.id)" -Headers $headers -ExpectedStatus @(200) -Name "get updated account character"
if ([string]$updated.id -ne [string]$character.id) { throw "account character id changed unexpectedly." }

if (($campaigns | Measure-Object).Count -gt 1) {
    $secondCampaignId = [string]$campaigns[1].id
    $secondSession = Invoke-Api -Method "POST" -Path "/api/game-sessions/start" -Headers $headers -ExpectedStatus @(200) -Name "start second story with same hero" -Body @{
        accountCharacterId = [string]$character.id
        campaignTemplateId = $secondCampaignId
        mode = "solo"
    }
    $secondStatus = Invoke-Api -Method "GET" -Path "/api/game-states/$($secondSession.gameStateId)/play/status" -Headers $headers -ExpectedStatus @(200) -Name "second story status"
    if (-not $secondStatus.permissions.canPlay) { throw "second story is not playable." }
}

Write-Step "Done"
Write-Host "Player solo smoke passed." -ForegroundColor Green

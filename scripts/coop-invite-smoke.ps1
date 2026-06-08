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

function Register-SmokeUser {
    param([string]$Prefix, [string]$Stamp)

    $response = Invoke-Api `
        -Method "POST" `
        -Path "/api/auth/register" `
        -ExpectedStatus @(200) `
        -Name "register $Prefix" `
        -Body @{
            email = "$Prefix-$Stamp@example.com"
            username = "${Prefix}_$Stamp"
            password = "Password123!"
            displayName = "$Prefix smoke"
        }

    if ([string]::IsNullOrWhiteSpace($response.accessToken)) {
        throw "register $Prefix did not return accessToken."
    }

    return @{
        Authorization = "Bearer $($response.accessToken)"
    }
}

function Assert-Truthy {
    param([object]$Value, [string]$Message)
    if (-not $Value) { throw $Message }
}

Write-Step "Health"
Invoke-Api -Method "GET" -Path "/health/db" -ExpectedStatus @(200) -Name "GET /health/db" | Out-Null

$stamp = Get-Date -Format "yyyyMMddHHmmss"

Write-Step "Register host and friend"
$hostHeaders = Register-SmokeUser -Prefix "coop-host" -Stamp $stamp
$friendHeaders = Register-SmokeUser -Prefix "coop-friend" -Stamp $stamp

Write-Step "Host creates game and character"
$game = Invoke-Api -Method "POST" -Path "/api/game-states" -Headers $hostHeaders -ExpectedStatus @(200, 201) -Name "host create game" -Body @{
    name = "Coop invite smoke $stamp"
}
$gameStateId = [string]$game.id
if ([string]::IsNullOrWhiteSpace($gameStateId)) { throw "create game did not return id." }

$hostCharacter = Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/characters" -Headers $hostHeaders -ExpectedStatus @(200, 201) -Name "host create character" -Body @{}
$hostCharacterId = [string]$hostCharacter.id
if ([string]::IsNullOrWhiteSpace($hostCharacterId)) { throw "host character did not return id." }

$hostStatus = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/play/status" -Headers $hostHeaders -ExpectedStatus @(200) -Name "host play/status"
Assert-Truthy $hostStatus.permissions.canManage "host must have canManage=true."
Assert-Truthy $hostStatus.currentPartyMember.isHost "host currentPartyMember must be host."

Write-Step "Invite preview and accept"
$invite = Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/invites" -Headers $hostHeaders -ExpectedStatus @(200) -Name "host create invite" -Body @{
    role = "player"
    maxUses = 1
    expiresInHours = 24
}
$token = [string]$invite.token
if ([string]::IsNullOrWhiteSpace($token)) { throw "invite did not return token." }

$preview = Invoke-Api -Method "GET" -Path "/api/invites/$token" -ExpectedStatus @(200) -Name "public invite preview"
if ([string]$preview.gameStateId -ne $gameStateId) { throw "preview returned wrong gameStateId." }

$accepted = Invoke-Api -Method "POST" -Path "/api/invites/$token/accept" -Headers $friendHeaders -ExpectedStatus @(200) -Name "friend accept invite" -Body @{}
if ([string]$accepted.gameStateId -ne $gameStateId) { throw "accept returned wrong gameStateId." }

Write-Step "Friend creates and receives assigned character"
$friendStatusBefore = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/play/status" -Headers $friendHeaders -ExpectedStatus @(200) -Name "friend play/status before character"
Assert-Truthy $friendStatusBefore.permissions.canPlay "friend must have canPlay=true."
if ($friendStatusBefore.permissions.canManage) { throw "friend player must not have canManage=true." }

$friendCharacter = Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/characters" -Headers $friendHeaders -ExpectedStatus @(200, 201) -Name "friend create character" -Body @{}
$friendCharacterId = [string]$friendCharacter.id
if ([string]::IsNullOrWhiteSpace($friendCharacterId)) { throw "friend character did not return id." }

$friendStatusAfter = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/play/status" -Headers $friendHeaders -ExpectedStatus @(200) -Name "friend play/status after character"
if ([string]$friendStatusAfter.currentPartyMember.characterId -ne $friendCharacterId) {
    throw "friend currentPartyMember.characterId was not assigned to created character."
}

Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/party" -Headers $hostHeaders -ExpectedStatus @(200) -Name "host party read" | Out-Null

Write-Step "Done"
Write-Host "Co-op invite smoke passed." -ForegroundColor Green
Write-Host "GameStateId: $gameStateId"
Write-Host "HostCharacterId: $hostCharacterId"
Write-Host "FriendCharacterId: $friendCharacterId"

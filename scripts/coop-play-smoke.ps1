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

    return @{ Authorization = "Bearer $($response.accessToken)" }
}

Write-Step "Health"
Invoke-Api -Method "GET" -Path "/health/db" -ExpectedStatus @(200) -Name "GET /health/db" | Out-Null

$stamp = Get-Date -Format "yyyyMMddHHmmss"
$hostHeaders = Register-SmokeUser -Prefix "coop-play-host" -Stamp $stamp
$friendHeaders = Register-SmokeUser -Prefix "coop-play-friend" -Stamp $stamp

Write-Step "Set up co-op game"
$game = Invoke-Api -Method "POST" -Path "/api/game-states" -Headers $hostHeaders -ExpectedStatus @(200, 201) -Name "host create game" -Body @{
    name = "Coop play smoke $stamp"
}
$gameStateId = [string]$game.id
if ([string]::IsNullOrWhiteSpace($gameStateId)) { throw "create game did not return id." }

$hostCharacter = Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/characters" -Headers $hostHeaders -ExpectedStatus @(200, 201) -Name "host create character" -Body @{}
$hostCharacterId = [string]$hostCharacter.id

$invite = Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/invites" -Headers $hostHeaders -ExpectedStatus @(200) -Name "host create invite" -Body @{
    role = "player"
    maxUses = 1
}
Invoke-Api -Method "POST" -Path "/api/invites/$($invite.token)/accept" -Headers $friendHeaders -ExpectedStatus @(200) -Name "friend accept invite" -Body @{} | Out-Null

$friendCharacter = Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/characters" -Headers $friendHeaders -ExpectedStatus @(200, 201) -Name "friend create character" -Body @{}
$friendCharacterId = [string]$friendCharacter.id

Write-Step "Snapshots"
$snapshot = Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/snapshots" -Headers $hostHeaders -ExpectedStatus @(200, 201) -Name "host create snapshot" -Body @{
    reason = "coop play smoke baseline"
}
Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/snapshots" -Headers $friendHeaders -ExpectedStatus @(200) -Name "friend list snapshots" | Out-Null

Write-Step "Bootstrap and action"
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/play/bootstrap" -Headers $hostHeaders -ExpectedStatus @(200, 201, 409, 503) -Name "host bootstrap" -Body @{} | Out-Null
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/play/act" -Headers $friendHeaders -ExpectedStatus @(200, 409, 503) -Name "friend play act" -Body @{
    characterId = $friendCharacterId
    message = "I inspect the room and report what I see."
} | Out-Null

$hostStatus = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/play/status" -Headers $hostHeaders -ExpectedStatus @(200) -Name "host play/status"
$friendStatus = Invoke-Api -Method "GET" -Path "/api/game-states/$gameStateId/play/status" -Headers $friendHeaders -ExpectedStatus @(200) -Name "friend play/status"
if (-not $hostStatus.permissions.canManage) { throw "host lost canManage permission." }
if ($friendStatus.permissions.canManage) { throw "friend unexpectedly has canManage permission." }
if ([string]$friendStatus.currentPartyMember.characterId -ne $friendCharacterId) { throw "friend status is not locked to friend character." }

Write-Step "Travel, rest and time"
$location = Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/world/locations" -Headers $hostHeaders -ExpectedStatus @(201) -Name "host create location" -Body @{
    name = "Coop smoke waypoint $stamp"
    description = "Waypoint for co-op smoke."
}
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/play/travel" -Headers $friendHeaders -ExpectedStatus @(200, 409) -Name "friend travel" -Body @{
    targetLocationId = $location.id
    note = "Co-op smoke travel."
} | Out-Null
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/rest/short" -Headers $friendHeaders -ExpectedStatus @(200, 409) -Name "friend short rest" -Body @{
    characterId = $friendCharacterId
} | Out-Null
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/time/advance" -Headers $hostHeaders -ExpectedStatus @(200) -Name "host advance time" -Body @{
    minutes = 60
    reason = "Co-op smoke."
} | Out-Null

Write-Step "Restore snapshot"
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/snapshots/$($snapshot.id)/restore" -Headers $hostHeaders -ExpectedStatus @(200) -Name "host restore snapshot" -Body @{} | Out-Null
Invoke-Api -Method "POST" -Path "/api/game-states/$gameStateId/snapshots/$($snapshot.id)/restore" -Headers $friendHeaders -ExpectedStatus @(403) -Name "friend cannot restore snapshot" -Body @{} | Out-Null

Write-Step "Done"
Write-Host "Co-op play smoke passed." -ForegroundColor Green
Write-Host "GameStateId: $gameStateId"
Write-Host "HostCharacterId: $hostCharacterId"
Write-Host "FriendCharacterId: $friendCharacterId"

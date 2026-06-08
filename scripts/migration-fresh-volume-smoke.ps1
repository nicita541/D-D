param(
    [switch]$KeepResources
)

$ErrorActionPreference = "Stop"

$stamp = Get-Date -Format "yyyyMMddHHmmss"
$network = "dnd_migration_fresh_$stamp"
$volume = "dnd_migration_fresh_data_$stamp"
$dbContainer = "dnd-migration-fresh-db-$stamp"
$dbImage = "dnd-migration-fresh-db:$stamp"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$databasePath = Join-Path $repoRoot "database"

function Write-Step($Text) {
    Write-Host ""
    Write-Host "== $Text ==" -ForegroundColor Cyan
}

function Assert-LastExit($Name) {
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE."
    }
}

function Wait-Postgres {
    for ($i = 0; $i -lt 120; $i++) {
        $logs = cmd /c "docker logs $dbContainer 2>&1" | Out-String
        if ($logs -match "PostgreSQL init process complete") {
            docker exec -e PGPASSWORD=dnd_password $dbContainer psql -U dnd_user -d dnd -Atc "SELECT 1;" *> $null
            if ($LASTEXITCODE -eq 0) {
                return
            }
        }

        if ((docker inspect -f "{{.State.Running}}" $dbContainer 2>$null) -ne "true") {
            cmd /c "docker logs $dbContainer 2>&1"
            throw "PostgreSQL container exited before becoming ready."
        }

        Start-Sleep -Seconds 1
    }

    cmd /c "docker logs $dbContainer 2>&1"
    throw "PostgreSQL did not become ready."
}

function Invoke-Migrations {
    docker run --rm `
        --network $network `
        -e POSTGRES_HOST=$dbContainer `
        -e POSTGRES_PORT=5432 `
        -e POSTGRES_DB=dnd `
        -e POSTGRES_USER=dnd_user `
        -e POSTGRES_PASSWORD=dnd_password `
        -v "$($databasePath):/migrations:ro" `
        postgres:16-alpine `
        /bin/sh /migrations/migrate.sh
    Assert-LastExit "migrations"
}

function Read-Sql($Sql) {
    $result = docker exec -e PGPASSWORD=dnd_password $dbContainer psql -U dnd_user -d dnd -Atc $Sql
    Assert-LastExit "psql"
    return $result
}

try {
    Write-Step "Create isolated Docker resources"
    docker network create $network | Out-Null
    Assert-LastExit "docker network create"
    docker volume create $volume | Out-Null
    Assert-LastExit "docker volume create"
    docker build -q -t $dbImage $databasePath | Out-Null
    Assert-LastExit "docker build"

    Write-Step "Start fresh database volume"
    docker run -d `
        --name $dbContainer `
        --network $network `
        -e POSTGRES_DB=dnd `
        -e POSTGRES_USER=dnd_user `
        -e POSTGRES_PASSWORD=dnd_password `
        -v "$($volume):/var/lib/postgresql/data" `
        $dbImage | Out-Null
    Assert-LastExit "docker run postgres"

    Wait-Postgres

    Write-Step "Run idempotent migrations"
    Invoke-Migrations

    Write-Step "Verify co-op and snapshot tables"
    $checks = Read-Sql "SELECT to_regclass('game.invites') IS NOT NULL, to_regclass('game.party_members') IS NOT NULL, to_regclass('game.snapshots') IS NOT NULL;"
    if ($checks -notmatch "t\|t\|t") {
        throw "Expected co-op tables were not created. Result: $checks"
    }

    Write-Step "Done"
    Write-Host "Fresh migration smoke passed." -ForegroundColor Green
}
finally {
    if (-not $KeepResources) {
        docker rm -f $dbContainer *> $null
        docker network rm $network *> $null
        docker volume rm $volume *> $null
    }
    else {
        Write-Host "Kept resources: $dbContainer, $network, $volume" -ForegroundColor Yellow
    }
}

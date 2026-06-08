param(
    [switch]$KeepResources
)

$ErrorActionPreference = "Stop"

$stamp = Get-Date -Format "yyyyMMddHHmmss"
$network = "dnd_migration_existing_$stamp"
$volume = "dnd_migration_existing_data_$stamp"
$dbContainer = "dnd-migration-existing-db-$stamp"
$dbImage = "dnd-migration-existing-db:$stamp"
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
    Write-Step "Create isolated existing-volume simulation"
    docker network create $network | Out-Null
    Assert-LastExit "docker network create"
    docker volume create $volume | Out-Null
    Assert-LastExit "docker volume create"
    docker build -q -t $dbImage $databasePath | Out-Null
    Assert-LastExit "docker build"

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

    Write-Step "First migration pass"
    Invoke-Migrations
    $firstCount = [int](Read-Sql "SELECT count(*) FROM infra.schema_migrations;")

    Write-Step "Second migration pass on same volume"
    Invoke-Migrations
    $secondCount = [int](Read-Sql "SELECT count(*) FROM infra.schema_migrations;")

    if ($firstCount -ne $secondCount) {
        throw "Second migration pass changed schema_migrations count: $firstCount -> $secondCount."
    }

    $checks = Read-Sql "SELECT to_regclass('game.invites') IS NOT NULL, to_regclass('game.snapshots') IS NOT NULL;"
    if ($checks -notmatch "t\|t") {
        throw "Expected existing-volume tables were not present. Result: $checks"
    }

    Write-Step "Done"
    Write-Host "Existing-volume migration smoke passed." -ForegroundColor Green
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

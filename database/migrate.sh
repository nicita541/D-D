#!/bin/sh
set -eu

export PGPASSWORD="${POSTGRES_PASSWORD}"

psql_args="-h ${POSTGRES_HOST:-postgres} -p ${POSTGRES_PORT:-5432} -U ${POSTGRES_USER} -d ${POSTGRES_DB} -v ON_ERROR_STOP=1"

psql ${psql_args} <<'SQL'
CREATE SCHEMA IF NOT EXISTS infra;
CREATE TABLE IF NOT EXISTS infra.schema_migrations (
    name text PRIMARY KEY,
    applied_at timestamptz NOT NULL DEFAULT now()
);
SQL

find /migrations/init -maxdepth 1 -type f -name '*.sql' | sort | while IFS= read -r migration; do
    name="$(basename "${migration}")"
    case "${name}" in
        001_*) continue ;;
    esac

    applied="$(psql ${psql_args} -Atc "SELECT EXISTS (SELECT 1 FROM infra.schema_migrations WHERE name = '${name}');")"
    if [ "${applied}" = "t" ]; then
        echo "Migration ${name} already applied."
        continue
    fi

    echo "Applying migration ${name}..."
    psql ${psql_args} -f "${migration}"
    psql ${psql_args} -c "INSERT INTO infra.schema_migrations (name) VALUES ('${name}');"
done

echo "Database migrations are up to date."

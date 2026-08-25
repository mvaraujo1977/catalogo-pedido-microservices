#!/bin/bash
set -e

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
    CREATE DATABASE catalog_db;
    CREATE DATABASE orders_db;
EOSQL

echo "Bancos catalog_db e orders_db criados com sucesso."

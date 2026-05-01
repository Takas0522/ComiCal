# SQL Server 2022 with Full-Text Search
# Repo policy: 本番・ローカルとも検索は SQL Server Full-Text Search を必須とする。
# 公式 mcr.microsoft.com/mssql/server:2022-latest は FT 非搭載のため、
# `mssql-server-fts` パッケージを追加インストールする。
#
# NOTE: ARM64 ホストでは linux/amd64 (qemu/rosetta) 経由でビルド/実行する必要がある
#       （Microsoft は ARM64 向けに mssql-server-fts を提供していない）。
FROM --platform=linux/amd64 mcr.microsoft.com/mssql/server:2022-latest

USER root

# mssql-server-fts は mssql-server と同じ MS リポジトリ（packages.microsoft.com）に存在する。
# Ubuntu 22.04 ベースイメージには microsoft-prod.list が同梱されているが、
# それは prod/jammy であり mssql-server-2022 とは別チャネル。
# 既存 mssql-server バイナリのバージョンと一致させるため、mssql-server-2022 リポジトリを追加する。
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl gpg ca-certificates \
    && curl -fsSL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor -o /usr/share/keyrings/microsoft.gpg \
    && echo "deb [arch=amd64 signed-by=/usr/share/keyrings/microsoft.gpg] https://packages.microsoft.com/ubuntu/22.04/mssql-server-2022 jammy main" > /etc/apt/sources.list.d/mssql-server-2022.list \
    && apt-get update \
    && ACCEPT_EULA=Y apt-get install -y --no-install-recommends mssql-server-fts \
    && rm -rf /var/lib/apt/lists/*

USER mssql

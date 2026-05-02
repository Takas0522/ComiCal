# ComiCal DevContainer
# Base: Microsoft devcontainers/dotnet (.NET 10 をベースに、Node/Azure CLI/Bicep は features で追加)
# 注: .NET 10 の devcontainer イメージは noble (Ubuntu 24.04) バリアントのみ公開されている (bookworm は無し)
FROM mcr.microsoft.com/devcontainers/dotnet:1-10.0-noble

ARG USERNAME=vscode

# ベースイメージに含まれる Yarn の apt source は GPG 鍵 (62D54FD4003F6525) が
# 失効しており apt-get update が NO_PUBKEY で失敗するため除去する。
# 本プロジェクトは pnpm を採用しており Yarn は使用しない。
RUN rm -f /etc/apt/sources.list.d/yarn.list /etc/apt/sources.list.d/yarn.sources

# 必要パッケージ
RUN apt-get update \
    && export DEBIAN_FRONTEND=noninteractive \
    && apt-get install -y --no-install-recommends \
        ca-certificates \
        curl \
        wget \
        gnupg \
        lsb-release \
        unzip \
        jq \
        git \
        zsh \
        build-essential \
        libicu-dev \
    && rm -rf /var/lib/apt/lists/*

# ----- Microsoft repository (sqlcmd / sqlpackage / mssql-tools18) -----
RUN curl -fsSL https://packages.microsoft.com/keys/microsoft.asc \
        | gpg --dearmor -o /usr/share/keyrings/microsoft-prod.gpg \
    && echo "deb [arch=amd64,arm64 signed-by=/usr/share/keyrings/microsoft-prod.gpg] https://packages.microsoft.com/ubuntu/24.04/prod noble main" \
        > /etc/apt/sources.list.d/mssql-release.list \
    && apt-get update \
    && ACCEPT_EULA=Y apt-get install -y --no-install-recommends \
        mssql-tools18 \
        unixodbc-dev \
    && rm -rf /var/lib/apt/lists/*

ENV PATH="/opt/mssql-tools18/bin:${PATH}"

# ----- Azure Functions Core Tools v4 -----
RUN curl -fsSL https://packages.microsoft.com/config/ubuntu/24.04/packages-microsoft-prod.deb -o /tmp/ms-prod.deb \
    && dpkg -i /tmp/ms-prod.deb \
    && apt-get update \
    && apt-get install -y --no-install-recommends azure-functions-core-tools-4 \
    && rm -rf /var/lib/apt/lists/* /tmp/ms-prod.deb

# ----- .NET global tools (vscode ユーザーで利用) -----
# sqlpackage は dotnet global tool としてインストールするとホストアーキテクチャ (linux-x64 / linux-arm64) に自動追従されるため、
# Apple Silicon など ARM64 ホストでも rosetta エラーにならずに動作する。
# (aka.ms/sqlpackage-linux は x64 固定 zip を返すため ARM64 では使用不可)
USER ${USERNAME}
ENV PATH="/home/${USERNAME}/.dotnet/tools:${PATH}"

RUN dotnet tool install --global dotnet-ef \
    && dotnet tool install --global dotnet-format \
    && dotnet tool install --global dotnet-outdated-tool \
    && dotnet tool install --global Microsoft.SqlPackage

USER root

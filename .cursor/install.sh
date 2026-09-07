#!/usr/bin/env bash
# Cloud Agent bootstrap for Android TV Hub.
#
# Android TV Hub is a WinUI 3 / Windows App SDK desktop app targeting
# net8.0-windows10.0.19041.0 and can only be *built to completion* and *run*
# on Windows (the WinUI XAML markup compiler and Windows App SDK runtime are
# Windows-only native binaries). This script provisions the most capable
# development environment possible on a Linux Cloud Agent VM: the .NET 8 SDK
# for editing, IntelliSense, package restore, and compiling the project's
# platform-agnostic logic.
set -euo pipefail

DOTNET_DIR="$HOME/.dotnet"
DOTNET_CHANNEL="8.0"

export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

if [ ! -x "$DOTNET_DIR/dotnet" ]; then
  echo "Installing .NET SDK ${DOTNET_CHANNEL} into ${DOTNET_DIR}..."
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --channel "$DOTNET_CHANNEL" --install-dir "$DOTNET_DIR"
else
  echo ".NET SDK already present at ${DOTNET_DIR}."
fi

# Make the SDK available to interactive and non-interactive shells on this VM.
if ! grep -q 'ANDROID_TV_HUB_DOTNET' "$HOME/.bashrc" 2>/dev/null; then
  cat >> "$HOME/.bashrc" <<'EOF'

# ANDROID_TV_HUB_DOTNET
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
EOF
fi

export DOTNET_ROOT="$DOTNET_DIR"
export PATH="$DOTNET_DIR:$PATH"

echo "dotnet: $(dotnet --version)"

# Restore packages. EnableWindowsTargeting lets the Windows-targeted project
# restore on a non-Windows host (build-to-completion still requires Windows).
echo "Restoring NuGet packages (EnableWindowsTargeting=true)..."
dotnet restore src/AndroidTvHub/AndroidTvHub.csproj -p:EnableWindowsTargeting=true

echo "Environment ready."

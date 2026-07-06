#!/bin/bash
# BONELAB-Fusion standard build script
# Usage: ./build.sh [deploy]
#   - no args: just build
#   - deploy: build + copy to Mods folder

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# Add dotnet to PATH if not there
if ! command -v dotnet &> /dev/null; then
    export PATH="$HOME/.dotnet:$PATH"
fi

# Verify dotnet
echo "[build] dotnet $(dotnet --version)"

# Set BONELAB_DIR if not set
export BONELAB_DIR="${BONELAB_DIR:-/mnt/d/SteamLibrary/steamapps/common/BONELAB}"
echo "[build] BONELAB_DIR=$BONELAB_DIR"

# Verify game DLLs exist
if [ ! -f "$BONELAB_DIR/MelonLoader/Il2CppAssemblies/Assembly-CSharp.dll" ]; then
    echo "[build] ERROR: BONELAB_DIR DLLs not found at $BONELAB_DIR"
    echo "[build] Set BONELAB_DIR to your BONELAB installation path"
    exit 1
fi

# Restore and build
echo "[build] Restoring packages..."
dotnet restore LabFusion/LabFusion.csproj -q

echo "[build] Building Release..."
dotnet build -c Release LabFusion/LabFusion.csproj --no-restore 2>&1 | tail -5

OUTPUT="LabFusion/bin/Release/net6.0/LabFusion.dll"
if [ ! -f "$OUTPUT" ]; then
    echo "[build] ERROR: Build succeeded but $OUTPUT not found"
    exit 1
fi

echo "[build] Success: $OUTPUT"
ls -la "$OUTPUT"

# Deploy if requested
if [ "$1" = "deploy" ]; then
    MODS_DIR="$BONELAB_DIR/Mods"
    echo "[deploy] Copying to $MODS_DIR/"
    cp "$OUTPUT" "$MODS_DIR/LabFusion.dll"
    echo "[deploy] Done"
fi

#!/bin/bash
# Deploy LabFusion.dll to Mods folder
# Usage: ./deploy.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

export BONELAB_DIR="${BONELAB_DIR:-/mnt/d/SteamLibrary/steamapps/common/BONELAB}"
MODS_DIR="$BONELAB_DIR/Mods"
OUTPUT="LabFusion/bin/Release/net6.0/LabFusion.dll"

if [ ! -f "$OUTPUT" ]; then
    echo "[deploy] ERROR: Run ./build.sh first — $OUTPUT not found"
    exit 1
fi

# Don't overwrite .original/.stock files
cp "$OUTPUT" "$MODS_DIR/LabFusion.dll"
echo "[deploy] $OUTPUT → $MODS_DIR/LabFusion.dll"
ls -la "$MODS_DIR/LabFusion.dll"

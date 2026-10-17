#!/usr/bin/env bash
set -euo pipefail
project_root="$(cd "$(dirname "$0")/.." && pwd)"
artifacts="$project_root/Artifacts/CombatTests"
mkdir -p "$artifacts"
sources=("$project_root/Assets/Ashbound/Scripts/Core/CombatSession.cs" "$project_root/Tests/CombatSessionTests.cs")
if command -v mcs >/dev/null && command -v mono >/dev/null; then
    mcs -warnaserror -out:"$artifacts/CombatTests.exe" "${sources[@]}"
    mono "$artifacts/CombatTests.exe"
elif [[ -x /workspace/.yorgame-setup/mono/usr/bin/mono-sgen ]]; then
    mono_root=/workspace/.yorgame-setup/mono
    export MONO_CFG_DIR="$mono_root/etc"
    export MONO_GAC_PREFIX="$mono_root/usr"
    export MONO_PATH="$mono_root/usr/lib/mono/4.5"
    "$mono_root/usr/bin/mono-sgen" "$MONO_PATH/mcs.exe" -warnaserror -out:"$artifacts/CombatTests.exe" "${sources[@]}"
    "$mono_root/usr/bin/mono-sgen" "$artifacts/CombatTests.exe"
else
    echo "Install Mono (mcs + mono) to run the standalone combat tests." >&2
    exit 1
fi

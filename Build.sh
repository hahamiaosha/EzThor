#!/usr/bin/env bash

# SPDX-FileCopyrightText: Copyright (c) 2026 QNAP Systems, Inc.
# SPDX-License-Identifier: BSD-2-Clause-Patent

###############################################################################
# Build.sh — Docker-based UEFI build for NVIDIA Tegra platforms (WSL/Linux)
#
# Usage:
#   ./Build.sh                          # Build QAI-TH1250 (default)
#   ./Build.sh <defconfig>              # Build with a specific defconfig name
#   ./Build.sh --debug-only             # Build DEBUG only
#   ./Build.sh --release-only           # Build RELEASE only
#   ./Build.sh --clean                  # Remove build artifacts and venv
#   ./Build.sh --shell                  # Open an interactive shell in the container
#
# Environment variables (optional):
#   EDK2_DEV_IMAGE   — Docker image (default: ghcr.io/tianocore/containers/ubuntu-22-dev:latest)
#   DEFCONFIG        — Defconfig name without path (default: t26x_general)
###############################################################################

set -euo pipefail

#------------------------------------------------------------------------------
# Configuration
#------------------------------------------------------------------------------
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORKSPACE_ROOT="${SCRIPT_DIR}"

EDK2_DEV_IMAGE="${EDK2_DEV_IMAGE:-ghcr.io/tianocore/containers/ubuntu-22-dev:latest}"
DEFAULT_DEFCONFIG="t26x_general"

# Build targets
BUILD_DEBUG=1
BUILD_RELEASE=1
DO_CLEAN=0
DO_SHELL=0
DEFCONFIG="${DEFCONFIG:-${DEFAULT_DEFCONFIG}}"

#------------------------------------------------------------------------------
# Parse arguments
#------------------------------------------------------------------------------
while [[ $# -gt 0 ]]; do
    case "$1" in
        --debug-only)
            BUILD_DEBUG=1; BUILD_RELEASE=0; shift ;;
        --release-only)
            BUILD_DEBUG=0; BUILD_RELEASE=1; shift ;;
        --clean)
            DO_CLEAN=1; shift ;;
        --shell)
            DO_SHELL=1; shift ;;
        --help|-h)
            sed -n '/^# Usage:/,/^###/p' "$0" | head -n -1 | sed 's/^# \?//'
            exit 0 ;;
        -*)
            echo "Unknown option: $1" >&2; exit 1 ;;
        *)
            DEFCONFIG="$1"; shift ;;
    esac
done

#------------------------------------------------------------------------------
# Validate workspace
#------------------------------------------------------------------------------
if [[ ! -d "${WORKSPACE_ROOT}/edk2" ]] || [[ ! -d "${WORKSPACE_ROOT}/edk2-nvidia" ]]; then
    echo "ERROR: This script must be placed at the root of the NVIDIA EDK2 workspace." >&2
    echo "       Expected to find edk2/ and edk2-nvidia/ in: ${WORKSPACE_ROOT}" >&2
    exit 1
fi

DEFCONFIG_DIR="edk2-nvidia/Platform/NVIDIA/Tegra/DefConfigs"
DEFCONFIG_FILE="${DEFCONFIG_DIR}/${DEFCONFIG}.defconfig"

if [[ "${DO_SHELL}" -eq 0 ]] && [[ "${DO_CLEAN}" -eq 0 ]]; then
    if [[ ! -f "${WORKSPACE_ROOT}/${DEFCONFIG_FILE}" ]]; then
        echo "ERROR: Defconfig not found: ${DEFCONFIG_FILE}" >&2
        echo "Available defconfigs:" >&2
        ls "${WORKSPACE_ROOT}/${DEFCONFIG_DIR}"/*.defconfig 2>/dev/null \
            | xargs -I{} basename {} .defconfig | sed 's/^/  /' >&2
        exit 1
    fi
fi

#------------------------------------------------------------------------------
# Ensure Docker is available
#------------------------------------------------------------------------------
if ! command -v docker &>/dev/null; then
    echo "ERROR: docker is not installed or not in PATH." >&2
    echo "       Please install Docker Desktop for Windows and enable WSL integration." >&2
    exit 1
fi

#------------------------------------------------------------------------------
# Pull image if not present
#------------------------------------------------------------------------------
echo "==> Ensuring Docker image: ${EDK2_DEV_IMAGE}"
if ! docker image inspect "${EDK2_DEV_IMAGE}" &>/dev/null; then
    docker pull "${EDK2_DEV_IMAGE}"
fi

#------------------------------------------------------------------------------
# Docker run helper
#------------------------------------------------------------------------------
DOCKER_RUN=(
    docker run --rm
    -v "${WORKSPACE_ROOT}":"${WORKSPACE_ROOT}"
    -v "${HOME}":"${HOME}"
    -w "${WORKSPACE_ROOT}"
    -e WORKSPACE="${WORKSPACE_ROOT}"
    -e HOME="${HOME}"
    -e EDK2_DOCKER_USER_HOME="${HOME}"
    -e CROSS_COMPILER_PREFIX=/usr/bin/aarch64-linux-gnu-
    -e PYTHONPATH="${WORKSPACE_ROOT}/edk2-nvidia/Silicon/NVIDIA:${PYTHONPATH:-}"
)

run_in_docker() {
    "${DOCKER_RUN[@]}" "${EDK2_DEV_IMAGE}" bash -c "$1"
}

#------------------------------------------------------------------------------
# --clean: remove build artifacts
#------------------------------------------------------------------------------
if [[ "${DO_CLEAN}" -eq 1 ]]; then
    echo "==> Cleaning build artifacts..."
    rm -rf "${WORKSPACE_ROOT}/Build"
    rm -rf "${WORKSPACE_ROOT}/venv"
    rm -rf "${WORKSPACE_ROOT}/images"
    rm -rf "${WORKSPACE_ROOT}/reports"
    rm -rf "${WORKSPACE_ROOT}/nvidia-config"
    find "${WORKSPACE_ROOT}/edk2/BaseTools/Source/C" -name "*.d" -delete 2>/dev/null || true
    find "${WORKSPACE_ROOT}/edk2/BaseTools/Source/C" -name "*.o" -delete 2>/dev/null || true
    rm -rf "${WORKSPACE_ROOT}/edk2/BaseTools/Source/C/bin"
    echo "==> Clean complete."
    exit 0
fi

#------------------------------------------------------------------------------
# --shell: interactive container
#------------------------------------------------------------------------------
if [[ "${DO_SHELL}" -eq 1 ]]; then
    echo "==> Opening interactive shell in Docker container..."
    "${DOCKER_RUN[@]}" -it "${EDK2_DEV_IMAGE}" bash
    exit 0
fi

#------------------------------------------------------------------------------
# Build
#------------------------------------------------------------------------------
PLATFORM_BUILD="edk2-nvidia/Platform/NVIDIA/Tegra/PlatformBuild.py"

# Compose environment flags for debug/release control
ENV_FLAGS=""
if [[ "${BUILD_DEBUG}" -eq 1 ]] && [[ "${BUILD_RELEASE}" -eq 0 ]]; then
    ENV_FLAGS="export UEFI_DEBUG_ONLY=1; "
elif [[ "${BUILD_DEBUG}" -eq 0 ]] && [[ "${BUILD_RELEASE}" -eq 1 ]]; then
    ENV_FLAGS="export UEFI_RELEASE_ONLY=1; "
fi

BUILD_CMD=$(cat <<'INNEREOF'
set -e

echo "==> [1/3] Preparing build environment..."

# Create virtualenv and install dependencies
if [ ! -e venv/bin/activate ]; then
    virtualenv -p python3 venv
fi
. venv/bin/activate

pip install --upgrade "setuptools<81"
pip install --upgrade -r edk2/pip-requirements.txt
pip install --upgrade kconfiglib

echo "==> [2/3] Running stuart_update..."
stuart_update -c ${PLATFORM_BUILD}

echo "==> [3/3] Building BaseTools..."
python edk2/BaseTools/Edk2ToolsBuild.py -t GCC

echo "==> Building firmware (defconfig: ${DEFCONFIG})..."
STUART_BUILD_OPTIONS="--verbose"

if [ -z "${UEFI_RELEASE_ONLY:-}" ]; then
    echo "--- Building DEBUG target ---"
    stuart_build -c ${PLATFORM_BUILD} \
        --init-defconfig ${DEFCONFIG_FILE} \
        ${STUART_BUILD_OPTIONS} --target DEBUG
fi

if [ -z "${UEFI_DEBUG_ONLY:-}" ]; then
    echo "--- Building RELEASE target ---"
    stuart_build -c ${PLATFORM_BUILD} \
        --init-defconfig ${DEFCONFIG_FILE} \
        ${STUART_BUILD_OPTIONS} --target RELEASE
fi

echo ""
echo "========================================="
echo "  Build complete!"
echo "========================================="
echo "  Firmware images:  images/"
echo "  Build reports:    reports/"
echo "========================================="
INNEREOF
)

echo "==> Starting NVIDIA UEFI build in Docker container"
echo "    Image:     ${EDK2_DEV_IMAGE}"
echo "    Workspace: ${WORKSPACE_ROOT}"
echo "    Defconfig: ${DEFCONFIG}"
echo ""

run_in_docker "${ENV_FLAGS} export PLATFORM_BUILD=${PLATFORM_BUILD}; export DEFCONFIG=${DEFCONFIG}; export DEFCONFIG_FILE=${DEFCONFIG_FILE}; ${BUILD_CMD}"

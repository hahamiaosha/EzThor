#!/bin/bash

# SPDX-FileCopyrightText: Copyright (c) 2026 QNAP Systems, Inc.
# SPDX-License-Identifier: BSD-2-Clause-Patent

###############################################################################
# Send_Cap_and_Update.sh — Send Capsule to Thor and trigger OTA update
#
# Usage:
#   ./Send_Cap_and_Update.sh                        # Use defaults
#   ./Send_Cap_and_Update.sh <cap_file>             # Specify Cap file
#   ./Send_Cap_and_Update.sh --host <build_host>    # Specify build host
#   ./Send_Cap_and_Update.sh --verify               # Only check Thor status
#
# Defaults:
#   Build host:   haha@10.33.76.23   (THOR HOST, where send_build.sh generates capsule)
#   THOR target:  qnap@10.33.76.128
#   THOR NAS:     haha@10.33.76.113  (repo path: /home/haha/qai-th1250)
#   Cap file:     ~/Linux_for_Tegra/TEGRA_BL.Cap (on build host)
#
# Notes:
#   - If <cap_file> exists locally, this script uploads that file directly.
#   - Otherwise <cap_file> is treated as a path on the build host.
#   - When running under bash/WSL, a Windows path like C:\Users\...\file.Cap
#     is accepted and translated to /mnt/c/Users/.../file.Cap.
#   - This script sends a capsule (.Cap), not a raw firmware binary (.bin).
#   - Use ./send_build.sh first if you need to generate TEGRA_BL.Cap.
###############################################################################

set -euo pipefail

#------------------------------------------------------------------------------
# Configuration
#------------------------------------------------------------------------------
BUILD_USER="haha"
BUILD_HOST="10.33.76.23"
BUILD_PASS="1234"
BUILD_CAP_PATH="~/Linux_for_Tegra/TEGRA_BL.Cap"

THOR_USER="qnap"
THOR_HOST="10.33.76.128"
THOR_PASS="1111"

SSH_OPTS="-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=10"
VERIFY_ONLY=0
CAP_FILE=""

usage_error() {
    echo "ERROR: $1" >&2
    exit 1
}

normalize_local_cap_path() {
    local path="$1"

    if [[ "${path}" =~ ^([A-Za-z]):\\ ]]; then
        local drive_letter="${BASH_REMATCH[1],,}"
        path="${path:2}"
        path="${path//\\//}"
        printf '/mnt/%s%s\n' "${drive_letter}" "${path}"
        return
    fi

    printf '%s\n' "${path}"
}

#------------------------------------------------------------------------------
# Parse arguments
#------------------------------------------------------------------------------
while [[ $# -gt 0 ]]; do
    case "$1" in
        --host)
            BUILD_HOST="$2"; shift 2 ;;
        --build-user)
            BUILD_USER="$2"; shift 2 ;;
        --build-pass)
            BUILD_PASS="$2"; shift 2 ;;
        --thor-host)
            THOR_HOST="$2"; shift 2 ;;
        --thor-user)
            THOR_USER="$2"; shift 2 ;;
        --thor-pass)
            THOR_PASS="$2"; shift 2 ;;
        --verify)
            VERIFY_ONLY=1; shift ;;
        --help|-h)
            sed -n '/^# Usage:/,/^###/p' "$0" | head -n -1 | sed 's/^# \?//'
            exit 0 ;;
        -*)
            echo "Unknown option: $1" >&2; exit 1 ;;
        *)
            CAP_FILE="$1"; shift ;;
    esac
done

if [[ -n "${CAP_FILE}" ]]; then
    CAP_FILE="$(normalize_local_cap_path "${CAP_FILE}")"
fi

#------------------------------------------------------------------------------
# Validate input
#------------------------------------------------------------------------------
if [[ -n "${CAP_FILE}" ]]; then
    case "${CAP_FILE}" in
        *.bin|*.BIN)
            usage_error "This script expects a capsule file (.Cap), not a raw .bin. Run ./send_build.sh first to generate ~/Linux_for_Tegra/TEGRA_BL.Cap."
            ;;
    esac
fi

#------------------------------------------------------------------------------
# Helper functions
#------------------------------------------------------------------------------
ssh_build() {
    sshpass -p "${BUILD_PASS}" ssh ${SSH_OPTS} "${BUILD_USER}@${BUILD_HOST}" "$1"
}

scp_from_build() {
    sshpass -p "${BUILD_PASS}" scp ${SSH_OPTS} "${BUILD_USER}@${BUILD_HOST}:$1" "$2"
}

ssh_thor() {
    sshpass -p "${THOR_PASS}" ssh ${SSH_OPTS} "${THOR_USER}@${THOR_HOST}" "$1"
}

scp_to_thor() {
    sshpass -p "${THOR_PASS}" scp ${SSH_OPTS} "$1" "${THOR_USER}@${THOR_HOST}:$2"
}

sudo_thor() {
    ssh_thor "echo ${THOR_PASS} | sudo -S -p '' $1 2>&1"
}

#------------------------------------------------------------------------------
# Verify mode
#------------------------------------------------------------------------------
if [[ "${VERIFY_ONLY}" -eq 1 ]]; then
    echo "==> Checking THOR target status (${THOR_HOST})..."
    sudo_thor "nvbootctrl dump-slots-info"
    echo ""
    echo "BIOS version: $(ssh_thor 'cat /sys/class/dmi/id/bios_version' 2>/dev/null)"
    echo "BIOS date:    $(ssh_thor 'cat /sys/class/dmi/id/bios_date' 2>/dev/null)"
    exit 0
fi

#------------------------------------------------------------------------------
# Pre-flight: check THOR target is online
#------------------------------------------------------------------------------
echo "==> Checking THOR target is online (${THOR_HOST})..."
if ! ssh_thor "echo ONLINE" &>/dev/null; then
    echo "ERROR: Cannot connect to THOR target at ${THOR_USER}@${THOR_HOST}" >&2
    exit 1
fi
echo "    THOR target is online."

#------------------------------------------------------------------------------
# Get current slot info
#------------------------------------------------------------------------------
echo "==> Current THOR target slot status:"
sudo_thor "nvbootctrl dump-slots-info"
echo ""

#------------------------------------------------------------------------------
# Transfer Cap file to THOR target
#------------------------------------------------------------------------------
CAP_INPUT="${CAP_FILE:-${BUILD_CAP_PATH}}"

if [[ -n "${CAP_FILE}" && -f "${CAP_FILE}" ]]; then
    case "${CAP_FILE}" in
        *.Cap|*.cap) ;;
        *)
            usage_error "Local file '${CAP_FILE}' does not look like a capsule (.Cap)."
            ;;
    esac
    echo "==> Using local Cap file: ${CAP_FILE}"
    cp "${CAP_FILE}" /tmp/TEGRA_BL.Cap
    echo "    Capsule source: local file"
    ls -l /tmp/TEGRA_BL.Cap
else
    echo "==> Pulling Cap file from ${BUILD_HOST}:${CAP_INPUT} ..."
    if ! scp_from_build "${CAP_INPUT}" /tmp/TEGRA_BL.Cap; then
        usage_error "Unable to fetch capsule from ${BUILD_HOST}:${CAP_INPUT}. This script expects a .Cap file. If you only have a .bin, run ./send_build.sh first."
    fi
    echo "    Capsule source: ${BUILD_USER}@${BUILD_HOST}:${CAP_INPUT}"
    ssh_build "ls -l --time-style=long-iso ${CAP_INPUT}" || true
fi

CAP_SIZE=$(ls -lh /tmp/TEGRA_BL.Cap | awk '{print $5}')
echo "    Cap file size: ${CAP_SIZE}"
echo "    Local staging hash: $(sha256sum /tmp/TEGRA_BL.Cap | awk '{print $1}')"

echo "==> Sending Cap file to THOR target (${THOR_HOST})..."
scp_to_thor /tmp/TEGRA_BL.Cap /tmp/TEGRA_BL.Cap
echo "    Transfer complete."

#------------------------------------------------------------------------------
# Stage capsule update
#------------------------------------------------------------------------------
echo "==> Staging capsule update on THOR target..."
sudo_thor "bash -c 'mkdir -p /opt/ota_package && cp /tmp/TEGRA_BL.Cap /opt/ota_package/ && ls -l --time-style=long-iso /opt/ota_package/TEGRA_BL.Cap && sha256sum /opt/ota_package/TEGRA_BL.Cap && nv_bootloader_capsule_updater.sh -q /opt/ota_package/TEGRA_BL.Cap'"

echo ""
echo "==> Capsule staged. Rebooting THOR target..."
sudo_thor "reboot" || true

#------------------------------------------------------------------------------
# Wait for THOR target to come back
#------------------------------------------------------------------------------
echo "==> Waiting for THOR target to reboot..."
sleep 30

for i in $(seq 1 20); do
    if ssh_thor "echo ONLINE" &>/dev/null; then
        echo "    THOR target is back online."
        break
    fi
    if [[ $i -eq 20 ]]; then
        echo "ERROR: THOR target did not come back after 5 minutes." >&2
        exit 1
    fi
    echo "    Waiting... (attempt ${i}/20)"
    sleep 15
done

#------------------------------------------------------------------------------
# Verify
#------------------------------------------------------------------------------
echo ""
echo "==> Post-update verification:"
sudo_thor "nvbootctrl dump-slots-info"
echo ""
BIOS_VER=$(ssh_thor 'cat /sys/class/dmi/id/bios_version' 2>/dev/null)
BIOS_DATE=$(ssh_thor 'cat /sys/class/dmi/id/bios_date' 2>/dev/null)

echo "========================================="
echo "  Capsule Update Complete!"
echo "========================================="
echo "  BIOS version:  ${BIOS_VER}"
echo "  BIOS date:     ${BIOS_DATE}"
echo "========================================="

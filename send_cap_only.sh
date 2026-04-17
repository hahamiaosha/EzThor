#!/bin/bash

# SPDX-FileCopyrightText: Copyright (c) 2026 QNAP Systems, Inc.
# SPDX-License-Identifier: BSD-2-Clause-Patent

###############################################################################
# send_cap_only.sh - Copy a .Cap file from THOR HOST to THOR target
#
# Usage:
#   ./send_cap_only.sh
#   ./send_cap_only.sh <cap_path_on_thor_host>
#   ./send_cap_only.sh --verify
#
# Defaults:
#   THOR HOST:   haha@10.33.76.23
#   THOR target: qnap@10.33.76.128
#   Cap file:    ~/Linux_for_Tegra/TEGRA_BL.Cap
#   Target path: /tmp/TEGRA_BL.Cap
#
# Notes:
#   - This script only copies the capsule.
#   - It does not call nv_bootloader_capsule_updater.sh.
#   - It does not reboot the THOR target.
###############################################################################

set -euo pipefail

HOST_USER="haha"
HOST_ADDR="10.33.76.23"
HOST_PASS="1234"

TARGET_USER="qnap"
TARGET_ADDR="10.33.76.128"
TARGET_PASS="1111"

CAP_PATH="${1:-~/Linux_for_Tegra/TEGRA_BL.Cap}"
TARGET_PATH="/tmp/TEGRA_BL.Cap"
VERIFY_ONLY=0

SSH_OPTS="-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o ConnectTimeout=10"

usage_error() {
    echo "ERROR: $1" >&2
    exit 1
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --host)
            HOST_ADDR="$2"; shift 2 ;;
        --host-user)
            HOST_USER="$2"; shift 2 ;;
        --host-pass)
            HOST_PASS="$2"; shift 2 ;;
        --target-host)
            TARGET_ADDR="$2"; shift 2 ;;
        --target-user)
            TARGET_USER="$2"; shift 2 ;;
        --target-pass)
            TARGET_PASS="$2"; shift 2 ;;
        --target-path)
            TARGET_PATH="$2"; shift 2 ;;
        --verify)
            VERIFY_ONLY=1; shift ;;
        --help|-h)
            sed -n '/^# Usage:/,/^###/p' "$0" | head -n -1 | sed 's/^# \?//'
            exit 0 ;;
        -*)
            usage_error "Unknown option: $1" ;;
        *)
            CAP_PATH="$1"; shift ;;
    esac
done

case "${CAP_PATH}" in
    *.Cap|*.cap) ;;
    *)
        usage_error "Expected a .Cap file path on THOR HOST. Got: ${CAP_PATH}" ;;
esac

ssh_host() {
    sshpass -p "${HOST_PASS}" ssh ${SSH_OPTS} "${HOST_USER}@${HOST_ADDR}" "$1"
}

ssh_target() {
    sshpass -p "${TARGET_PASS}" ssh ${SSH_OPTS} "${TARGET_USER}@${TARGET_ADDR}" "$1"
}

scp_from_host() {
    sshpass -p "${HOST_PASS}" scp ${SSH_OPTS} "${HOST_USER}@${HOST_ADDR}:$1" "$2"
}

scp_to_target() {
    sshpass -p "${TARGET_PASS}" scp ${SSH_OPTS} "$1" "${TARGET_USER}@${TARGET_ADDR}:$2"
}

if [[ "${VERIFY_ONLY}" -eq 1 ]]; then
    echo "==> THOR HOST capsule:"
    ssh_host "ls -l --time-style=long-iso ${CAP_PATH} && sha256sum ${CAP_PATH}"
    echo ""
    echo "==> THOR target destination:"
    ssh_target "ls -l --time-style=long-iso ${TARGET_PATH} && sha256sum ${TARGET_PATH}" || true
    exit 0
fi

echo "==> Checking THOR HOST capsule exists..."
if ! ssh_host "test -f ${CAP_PATH}"; then
    usage_error "Capsule not found on THOR HOST: ${CAP_PATH}"
fi

echo "==> Pulling capsule from THOR HOST ${HOST_ADDR}:${CAP_PATH}"
scp_from_host "${CAP_PATH}" /tmp/TEGRA_BL.Cap

echo "==> Local staging:"
ls -l /tmp/TEGRA_BL.Cap
sha256sum /tmp/TEGRA_BL.Cap

echo "==> Sending capsule to THOR target ${TARGET_ADDR}:${TARGET_PATH}"
scp_to_target /tmp/TEGRA_BL.Cap "${TARGET_PATH}"

echo "==> THOR target file:"
ssh_target "ls -l --time-style=long-iso ${TARGET_PATH} && sha256sum ${TARGET_PATH}"

echo ""
echo "========================================="
echo "  Capsule Copy Complete"
echo "========================================="
echo "  Source: ${HOST_USER}@${HOST_ADDR}:${CAP_PATH}"
echo "  Target: ${TARGET_USER}@${TARGET_ADDR}:${TARGET_PATH}"
echo "========================================="

#!/bin/bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CONFIG_PATH="${SCRIPT_DIR}/env/target_config.sh"

if [ ! -f "${CONFIG_PATH}" ]; then
    echo "ERROR: ${CONFIG_PATH} not found"
    echo "ThorFlasher expects a tokenized script config file here."
    exit 1
fi

# shellcheck source=/dev/null
source "${CONFIG_PATH}"

REMOTE_HOST="${THOR_HOST_IP:-}"
TARGET_HOST="${THOR_TARGET_IP:-}"
SSH_OPTS="-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null"

if [ -z "${REMOTE_HOST}" ]; then
    echo "ERROR: THOR host IP is not configured."
    exit 1
fi

if [ -z "${REMOTE_BSP_ROOT:-}" ]; then
    echo "ERROR: REMOTE_BSP_ROOT is not configured."
    exit 1
fi

echo "==> THOR Host: ${REMOTE_HOST}"
echo "==> THOR Target IP: ${TARGET_HOST:-Not configured}"
echo "==> Flash mode: qspi-only"
echo "==> Remote BSP root (Linux_for_Tegra): ${REMOTE_BSP_ROOT}"
echo ""
echo "Only qspi-only flash is supported at the moment."
echo "This script assumes send_build.sh has already uploaded the selected UEFI binary."
echo "Before flashing, manually place the THOR target into recovery mode."
echo "The flash command is executed on the THOR host inside:"
echo "    ${REMOTE_USER}@${REMOTE_HOST}:${REMOTE_BSP_ROOT}"
echo "Remote command:"
echo "    cd ${REMOTE_BSP_ROOT} && sudo ./l4t_initrd_flash.sh --qspi-only jetson-agx-thor-devkit mmcblk0p1"
echo ""
echo "Waiting for manual recovery-mode preparation is not automated."
echo "If the target is not already in recovery mode, the host-side flash command will fail."

echo "==> Running qspi-only flash on THOR Host ..."
sshpass -p "${REMOTE_PASS}" ssh ${SSH_OPTS} "${REMOTE_USER}@${REMOTE_HOST}" \
    "cd ${REMOTE_BSP_ROOT} && \
     echo '${REMOTE_PASS}' | sudo -S -p '' ./l4t_initrd_flash.sh --qspi-only jetson-agx-thor-devkit mmcblk0p1"

echo ""
echo "========================================="
echo "  QSPI Flash Complete"
echo "========================================="
echo "  THOR Host: ${REMOTE_HOST}"
echo "  THOR Target IP: ${TARGET_HOST:-Not configured}"
echo "  Directory: ${REMOTE_BSP_ROOT}"
echo "  Command:   cd ${REMOTE_BSP_ROOT} && sudo ./l4t_initrd_flash.sh --qspi-only jetson-agx-thor-devkit mmcblk0p1"
echo "========================================="

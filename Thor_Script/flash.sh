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

FLASH_MODE="qspi"
FLASH_SLOT="A"

#------------------------------------------------------------------------------
# Parse arguments
#------------------------------------------------------------------------------
while [[ $# -gt 0 ]]; do
    case "$1" in
        --mode)  FLASH_MODE="${2,,}"; shift 2 ;;
        --slot)  FLASH_SLOT="${2^^}"; shift 2 ;;
        --help|-h)
            echo "Usage: flash.sh [--mode qspi|uefi|bpmp] [--slot A|B]"
            exit 0 ;;
        *)  shift ;;
    esac
done

#------------------------------------------------------------------------------
# Validate
#------------------------------------------------------------------------------
if [ -z "${REMOTE_HOST}" ]; then
    echo "ERROR: THOR host IP is not configured."
    exit 1
fi

if [ -z "${REMOTE_BSP_ROOT:-}" ]; then
    echo "ERROR: REMOTE_BSP_ROOT is not configured."
    exit 1
fi

if [[ "${FLASH_MODE}" != "qspi" && "${FLASH_MODE}" != "uefi" && "${FLASH_MODE}" != "bpmp" ]]; then
    echo "ERROR: Unknown flash mode '${FLASH_MODE}'. Use: qspi, uefi, bpmp"
    exit 1
fi

if [[ "${FLASH_SLOT}" != "A" && "${FLASH_SLOT}" != "B" ]]; then
    echo "ERROR: Unknown flash slot '${FLASH_SLOT}'. Use: A, B"
    exit 1
fi

#------------------------------------------------------------------------------
# Print banner
#------------------------------------------------------------------------------
echo "==> THOR Host: ${REMOTE_HOST}"
echo "==> THOR Target IP: ${TARGET_HOST:-Not configured}"
echo "==> Flash mode: ${FLASH_MODE}"
[[ "${FLASH_MODE}" != "qspi" ]] && echo "==> Flash slot: ${FLASH_SLOT}"
echo "==> Remote BSP root (Linux_for_Tegra): ${REMOTE_BSP_ROOT}"
echo ""
echo "This script assumes send_build.sh has already uploaded the selected UEFI binary."
echo "Before flashing, manually place the THOR target into recovery mode."
echo ""

DEVICE="jetson-agx-thor-devkit"
STORAGE="mmcblk0p1"

run_on_host() {
    sshpass -p "${REMOTE_PASS}" ssh ${SSH_OPTS} "${REMOTE_USER}@${REMOTE_HOST}" \
        "cd ${REMOTE_BSP_ROOT} && echo '${REMOTE_PASS}' | sudo -S -p '' $1"
}

#------------------------------------------------------------------------------
# Execute flash
#------------------------------------------------------------------------------
case "${FLASH_MODE}" in
    qspi)
        echo "==> Running QSPI flash on THOR Host ..."
        echo "    Command: cd ${REMOTE_BSP_ROOT} && sudo ./l4t_initrd_flash.sh --qspi-only ${DEVICE} ${STORAGE}"
        echo ""
        run_on_host "./l4t_initrd_flash.sh --qspi-only ${DEVICE} ${STORAGE}"
        FLASH_LABEL="QSPI Flash"
        ;;

    uefi)
        PARTITION_KEY="cpu-bootloader"
        echo "==> Step 1: Preparing UEFI flash (slot ${FLASH_SLOT}) on THOR Host ..."
        echo "    Command: sudo ./l4t_initrd_flash.sh --no-flash --qspi-only --boot-chain-flash ${FLASH_SLOT} -k ${FLASH_SLOT}_${PARTITION_KEY} ${DEVICE} ${STORAGE}"
        echo ""
        run_on_host "./l4t_initrd_flash.sh --no-flash --qspi-only --boot-chain-flash ${FLASH_SLOT} -k ${FLASH_SLOT}_${PARTITION_KEY} ${DEVICE} ${STORAGE}"

        echo ""
        echo "==> Step 2: Flashing UEFI (slot ${FLASH_SLOT}) on THOR Host ..."
        echo "    Command: sudo ./l4t_initrd_flash.sh -k ${FLASH_SLOT}_${PARTITION_KEY} ${DEVICE} ${STORAGE}"
        echo ""
        run_on_host "./l4t_initrd_flash.sh -k ${FLASH_SLOT}_${PARTITION_KEY} ${DEVICE} ${STORAGE}"
        FLASH_LABEL="UEFI Flash (Slot ${FLASH_SLOT})"
        ;;

    bpmp)
        PARTITION_KEY="bpmp-fw-dtb"
        echo "==> Step 1: Preparing BPMP flash (slot ${FLASH_SLOT}) on THOR Host ..."
        echo "    Command: sudo ./l4t_initrd_flash.sh --no-flash --qspi-only --boot-chain-flash ${FLASH_SLOT} -k ${FLASH_SLOT}_${PARTITION_KEY} ${DEVICE} ${STORAGE}"
        echo ""
        run_on_host "./l4t_initrd_flash.sh --no-flash --qspi-only --boot-chain-flash ${FLASH_SLOT} -k ${FLASH_SLOT}_${PARTITION_KEY} ${DEVICE} ${STORAGE}"

        echo ""
        echo "==> Step 2: Flashing BPMP (slot ${FLASH_SLOT}) on THOR Host ..."
        echo "    Command: sudo ./l4t_initrd_flash.sh -k ${FLASH_SLOT}_${PARTITION_KEY} ${DEVICE} ${STORAGE}"
        echo ""
        run_on_host "./l4t_initrd_flash.sh -k ${FLASH_SLOT}_${PARTITION_KEY} ${DEVICE} ${STORAGE}"
        FLASH_LABEL="BPMP Flash (Slot ${FLASH_SLOT})"
        ;;
esac

echo ""
echo "========================================="
echo "  ${FLASH_LABEL} Complete"
echo "========================================="
echo "  THOR Host: ${REMOTE_HOST}"
echo "  THOR Target IP: ${TARGET_HOST:-Not configured}"
echo "  Directory: ${REMOTE_BSP_ROOT}"
echo "========================================="

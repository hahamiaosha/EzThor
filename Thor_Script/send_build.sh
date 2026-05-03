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
SRC="${SELECTED_FILE_PATH:-}"

SSH_OPTS="-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null"

normalize_local_path() {
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

if [ -z "${REMOTE_HOST}" ]; then
    echo "ERROR: THOR host IP is not configured."
    exit 1
fi

if [ -z "${SRC}" ]; then
    echo "ERROR: SELECTED_FILE_PATH is not configured."
    exit 1
fi

SRC="$(normalize_local_path "${SRC}")"

SKIP_CAPSULE=false

case "${SRC}" in
    *.bin|*.BIN)
        # UEFI: upload .bin to uefi_bins and multi_signed (A + B cpu-bootloader), no capsule.
        REMOTE_DIR="${REMOTE_BSP_ROOT}/bootloader/uefi_bins"
        TARGET_NAME="uefi_t26x_general.bin"
        SKIP_CAPSULE=true
        ;;
    *.dtb|*.DTB)
        # BPMP: rename to the canonical dtb name and upload to bootloader/, skip capsule generation.
        REMOTE_DIR="${REMOTE_BSP_ROOT}/bootloader/generic"
        TARGET_NAME="tegra264-bpmp-3834-0008-4071-xxxx.dtb"
        SKIP_CAPSULE=true
        ;;
    *)
        echo "ERROR: send_build.sh supports .bin and .dtb files."
        echo "       Selected file: ${SRC}"
        exit 1
        ;;
esac

if [ ! -f "${SRC}" ]; then
    echo "ERROR: ${SRC} not found"
    exit 1
fi

echo "==> THOR Host: ${REMOTE_HOST}"
echo "==> THOR Target: ${TARGET_HOST:-not used in send_build step}"
echo "==> Sending $(basename "${SRC}") to ${REMOTE_HOST}:${REMOTE_DIR}/${TARGET_NAME} ..."
echo "    Local file: ${SRC}"
echo "    Remote file: ${REMOTE_DIR}/${TARGET_NAME}"

if [ "${SKIP_CAPSULE}" = true ]; then
    # bootloader/ is root-owned; scp to /tmp first, then sudo cp to the targets.
    REMOTE_TMP="/tmp/${TARGET_NAME}"
    MULTI_SIGNED_DIR="${REMOTE_BSP_ROOT}/bootloader/multi_signed/3834-000-0008--1-0-jetson-agx-thor-devkit-"
    sshpass -p "${REMOTE_PASS}" scp ${SSH_OPTS} \
        "${SRC}" "${REMOTE_USER}@${REMOTE_HOST}:${REMOTE_TMP}"

    case "${SRC}" in
        *.bin|*.BIN)
            sshpass -p "${REMOTE_PASS}" ssh ${SSH_OPTS} "${REMOTE_USER}@${REMOTE_HOST}" \
                "echo '${REMOTE_PASS}' | sudo -S -p '' cp '${REMOTE_TMP}' ${REMOTE_DIR}/${TARGET_NAME} && \
                 echo '${REMOTE_PASS}' | sudo -S -p '' cp '${REMOTE_TMP}' ${MULTI_SIGNED_DIR}/A_cpu-bootloader && \
                 echo '${REMOTE_PASS}' | sudo -S -p '' cp '${REMOTE_TMP}' ${MULTI_SIGNED_DIR}/B_cpu-bootloader && \
                 rm -f '${REMOTE_TMP}'"
            echo "==> Transfer complete."
            echo "==> Replaced ${REMOTE_DIR}/${TARGET_NAME}"
            echo "==> Replaced ${MULTI_SIGNED_DIR}/A_cpu-bootloader"
            echo "==> Replaced ${MULTI_SIGNED_DIR}/B_cpu-bootloader"
            ;;
        *.dtb|*.DTB)
            sshpass -p "${REMOTE_PASS}" ssh ${SSH_OPTS} "${REMOTE_USER}@${REMOTE_HOST}" \
                "echo '${REMOTE_PASS}' | sudo -S -p '' cp '${REMOTE_TMP}' ${REMOTE_DIR}/${TARGET_NAME} && \
                 echo '${REMOTE_PASS}' | sudo -S -p '' cp '${REMOTE_TMP}' ${MULTI_SIGNED_DIR}/A_bpmp-fw-dtb && \
                 echo '${REMOTE_PASS}' | sudo -S -p '' cp '${REMOTE_TMP}' ${MULTI_SIGNED_DIR}/B_bpmp-fw-dtb && \
                 rm -f '${REMOTE_TMP}'"
            echo "==> Transfer complete."
            echo "==> Replaced ${REMOTE_DIR}/${TARGET_NAME}"
            echo "==> Replaced ${MULTI_SIGNED_DIR}/A_bpmp-fw-dtb"
            echo "==> Replaced ${MULTI_SIGNED_DIR}/B_bpmp-fw-dtb"
            ;;
    esac
    echo ""
    echo "========================================="
    echo "  send_build.sh completed successfully"
    echo "  Uploaded: ${REMOTE_DIR}/${TARGET_NAME}"
    echo "========================================="
    exit 0
fi

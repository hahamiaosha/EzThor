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

case "${SRC}" in
    *.bin|*.BIN) ;;
    *)
        echo "ERROR: send_build.sh currently expects a .bin package for the upload/build flow."
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
echo "    Local build bin: ${SRC}"
echo "    Remote UEFI bin: ${REMOTE_DIR}/${TARGET_NAME}"

sshpass -p "${REMOTE_PASS}" scp ${SSH_OPTS} \
    "${SRC}" "${REMOTE_USER}@${REMOTE_HOST}:${REMOTE_DIR}/${TARGET_NAME}"

echo "==> Transfer complete."

echo "==> Generating capsule on ${REMOTE_HOST} ..."
echo "    Capsule output: ${REMOTE_BSP_ROOT}/${CAPSULE_OUTPUT}"
echo "    Capsule source: ${REMOTE_BSP_ROOT}/bootloader/payloads_t26x/bl_only_payload"

sshpass -p "${REMOTE_PASS}" ssh ${SSH_OPTS} "${REMOTE_USER}@${REMOTE_HOST}" \
    "echo '${REMOTE_PASS}' | sudo -S -p '' rm -f ${REMOTE_BSP_ROOT}/${CAPSULE_OUTPUT} && \
     echo '${REMOTE_PASS}' | sudo -S -p '' rm -rf ${REMOTE_BSP_ROOT}/bootloader/payloads_t26x && \
     echo '${REMOTE_PASS}' | sudo -S -p '' rm -rf ${REMOTE_BSP_ROOT}/bootloader/signed && \
     mkdir -p /tmp/pybin && ln -sf /usr/bin/python3 /tmp/pybin/python && \
     export PATH=/tmp/pybin:\$PATH && \
     cd ${REMOTE_BSP_ROOT} && \
     echo '${REMOTE_PASS}' | sudo -S -p '' FAB=000 BOARDID=3834 FUSELEVEL=fuselevel_production \
        BOARDSKU=0008 CHIP_SKU='00:00:00:A0' \
        ./build_l4t_bup.sh jetson-agx-thor-devkit internal && \
     ${CAPSULE_DIR}/l4t_generate_soc_capsule.sh \
        -i bootloader/payloads_t26x/bl_only_payload \
        -o ./${CAPSULE_OUTPUT} \
        t264"

echo "==> Remote file details:"
sshpass -p "${REMOTE_PASS}" ssh ${SSH_OPTS} "${REMOTE_USER}@${REMOTE_HOST}" \
    "ls -l --time-style=long-iso ${REMOTE_DIR}/${TARGET_NAME} \
        ${REMOTE_BSP_ROOT}/bootloader/payloads_t26x/bl_only_payload \
        ${REMOTE_BSP_ROOT}/${CAPSULE_OUTPUT}"

echo ""
echo "========================================="
echo "  send_build.sh completed successfully"
echo "  Capsule generated: ${REMOTE_BSP_ROOT}/${CAPSULE_OUTPUT}"
echo "========================================="

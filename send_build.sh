#!/bin/bash

REMOTE_USER="haha"
REMOTE_HOST="10.33.76.23"
REMOTE_PASS="1234"
REMOTE_BSP_ROOT="~/Linux_for_Tegra"
REMOTE_DIR="~/Linux_for_Tegra/bootloader/uefi_bins"
CAPSULE_DIR="~/Linux_for_Tegra/generate_capsule"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SRC="${SCRIPT_DIR}/images/uefi_t26x_qnap_qai_th1250_RELEASE.bin"
TARGET_NAME="uefi_t26x_general.bin"
CAPSULE_OUTPUT="TEGRA_BL.Cap"

SSH_OPTS="-o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null"

if [ ! -f "$SRC" ]; then
    echo "ERROR: $SRC not found"
    exit 1
fi

echo "==> Sending $(basename "$SRC") to ${REMOTE_HOST}:${REMOTE_DIR}/${TARGET_NAME} ..."
echo "    Local build bin: ${SRC}"
echo "    Remote UEFI bin: ${REMOTE_DIR}/${TARGET_NAME}"

sshpass -p "$REMOTE_PASS" scp $SSH_OPTS \
    "$SRC" "${REMOTE_USER}@${REMOTE_HOST}:${REMOTE_DIR}/${TARGET_NAME}"

if [ $? -ne 0 ]; then
    echo "ERROR: Transfer failed."
    exit 1
fi
echo "==> Transfer complete."

echo "==> Generating capsule on ${REMOTE_HOST} ..."
echo "    Capsule output: ~/Linux_for_Tegra/${CAPSULE_OUTPUT}"
echo "    Capsule source: ${REMOTE_BSP_ROOT}/bootloader/payloads_t26x/bl_only_payload"

sshpass -p "$REMOTE_PASS" ssh $SSH_OPTS "${REMOTE_USER}@${REMOTE_HOST}" \
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

if [ $? -ne 0 ]; then
    echo "ERROR: Capsule generation failed."
    exit 1
fi

echo "==> Remote file details:"
sshpass -p "$REMOTE_PASS" ssh $SSH_OPTS "${REMOTE_USER}@${REMOTE_HOST}" \
    "ls -l --time-style=long-iso ${REMOTE_DIR}/${TARGET_NAME} \
        ${REMOTE_BSP_ROOT}/bootloader/payloads_t26x/bl_only_payload \
        ${REMOTE_BSP_ROOT}/${CAPSULE_OUTPUT}"

echo ""
echo "========================================="
echo "  Done!"
echo "  UEFI binary: ${REMOTE_DIR}/${TARGET_NAME}"
echo "  Capsule:     ~/Linux_for_Tegra/${CAPSULE_OUTPUT}"
echo "========================================="

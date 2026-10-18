#!/bin/sh
# Alpine Linux Kubernetes node setup (control-plane or worker).
# Based on the Alpine wiki guide for containerd + kubelet/kubeadm/kubectl + Flannel.
# Run as root. Review SWAP_MODE below before executing.
#
# SWAP_MODE=disable   -> disable swap by commenting it out in /etc/fstab (default)
# SWAP_MODE=keep      -> keep swap/ZRAM; kubelet config must set failSwapOn: false
#                         with memorySwap.swapBehavior: NoSwap or LimitedSwap
: "${SWAP_MODE:=disable}"

set -eu

log() { printf '\n==> %s\n' "$*"; }

[ "$(id -u)" -eq 0 ] || { echo "This script must be run as root." >&2; exit 1; }

# ------------------------------------------------------------------
# 1. Packages
# ------------------------------------------------------------------
log "Installing containerd, Kubernetes and CNI packages"
apk update
apk add containerd kubelet kubeadm kubectl cni-plugins cni-plugin-flannel uuidgen

# Pin Kubernetes to the current minor release (1.36 on Alpine 3.24).
# Keeps package revisions and patch releases but blocks minor-version jumps.
log "Pinning kubelet/kubeadm/kubectl to the 1.36 release series"
apk add 'kubelet=~1.36' 'kubeadm=~1.36' 'kubectl=~1.36'

echo "Constraints now in /etc/apk/world:"
grep -E '(=|<|>|~|@)' /etc/apk/world || true

# ------------------------------------------------------------------
# 2. Kernel module: br_netfilter
# ------------------------------------------------------------------
log "Configuring br_netfilter kernel module"
cat > /etc/modules-load.d/kubernetes.conf <<'EOF'
br_netfilter
EOF
modprobe br_netfilter

# ------------------------------------------------------------------
# 3. Network sysctls
# ------------------------------------------------------------------
log "Applying network sysctls"
cat > /etc/sysctl.d/90-kubernetes.conf <<'EOF'
net.bridge.bridge-nf-call-iptables=1
net.ipv4.ip_forward=1
EOF
sysctl -p /etc/sysctl.d/90-kubernetes.conf

echo "Verifying sysctls (both should be 1):"
sysctl net.bridge.bridge-nf-call-iptables
sysctl net.ipv4.ip_forward

# ------------------------------------------------------------------
# 4. Mount propagation (shared root mount)
# ------------------------------------------------------------------
log "Making / recursively shared (mount propagation)"
mount --make-rshared /

cat > /etc/local.d/mount-rshared.start <<'EOF'
#!/bin/sh
mount --make-rshared /
EOF
chmod +x /etc/local.d/mount-rshared.start
rc-update add local default

# ------------------------------------------------------------------
# 5. Cgroups (v2, cgroupfs driver)
# ------------------------------------------------------------------
log "Enabling cgroups service"
rc-update add cgroups
rc-service cgroups start

echo "Checking cgroup v2 mount:"
grep ' cgroup2 ' /proc/mounts || true

# OpenRC (not systemd): both containerd and kubelet must use the cgroupfs driver.
log "Ensuring containerd uses SystemdCgroup = false"
if grep -q 'SystemdCgroup' /etc/containerd/config.toml; then
    sed -i 's/SystemdCgroup = true/SystemdCgroup = false/' /etc/containerd/config.toml
fi

# ------------------------------------------------------------------
# 6. Swap
# ------------------------------------------------------------------
if [ "$SWAP_MODE" = "disable" ]; then
    log "Disabling swap"
    [ -f /etc/fstab ] && cp -a /etc/fstab /etc/fstab.bak
    [ -f /etc/fstab ] && sed -i '/[[:space:]]swap[[:space:]]/s/^/#/' /etc/fstab
    swapoff -a || true
    echo "Active swap (should be empty):"
    swapon --show || true
    echo "Note: if swap comes from ZRAM or another service, disable that service too."
else
    log "Keeping swap; remember to set in KubeletConfiguration:"
    echo "  failSwapOn: false"
    echo "  memorySwap:"
    echo "    swapBehavior: NoSwap   # or LimitedSwap"
fi

# ------------------------------------------------------------------
# 7. Machine ID
# ------------------------------------------------------------------
log "Checking /etc/machine-id"
if [ ! -s /etc/machine-id ]; then
    uuidgen > /etc/machine-id
    echo "Generated new /etc/machine-id."
else
    echo "/etc/machine-id already set (not regenerating)."
fi

# ------------------------------------------------------------------
# 8. Containerd checks: CNI path and sandbox image
# ------------------------------------------------------------------
log "Containerd CNI path (Alpine uses /usr/libexec/cni):"
grep -n '/usr/libexec/cni' /etc/containerd/config.toml || true

log "Pause image expected by kubeadm:"
kubeadm config images list --kubernetes-version "$(kubeadm version -o short)" | grep pause || true
echo "Compare with the sandbox image in /etc/containerd/config.toml:"
grep -n 'sandbox' /etc/containerd/config.toml || true
echo "If the versions differ, edit /etc/containerd/config.toml to match before kubeadm init/join."

# ------------------------------------------------------------------
# 9. Time synchronization
# ------------------------------------------------------------------
if ! rc-service --status ntpd >/dev/null 2>&1 \
   && ! rc-service --status chronyd >/dev/null 2>&1 \
   && ! rc-service --status openntpd >/dev/null 2>&1; then
    log "No NTP service detected; starting setup-ntp"
    setup-ntp || true
fi

# ------------------------------------------------------------------
# 10. Services
# ------------------------------------------------------------------
log "Enabling containerd and kubelet at boot"
rc-update add containerd
rc-update add kubelet

log "Starting containerd"
rc-service containerd start

echo
echo "Done. Next steps:"
echo "  - Control plane: kubeadm init (with cgroupDriver: cgroupfs in KubeletConfiguration)"
echo "  - Workers:       kubeadm join <control-plane>"
echo "  - If keeping swap: add failSwapOn/memorySwap to the KubeletConfiguration."

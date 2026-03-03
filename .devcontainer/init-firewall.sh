#!/bin/bash
# ==========================================================================
# init-firewall.sh — DNS-based network firewall for the dev container
#
# Uses dnsmasq as a local DNS proxy that only resolves allowed domain
# patterns. Unmatched domains get REFUSED — no IP chasing needed.
# ==========================================================================
set -euo pipefail
IFS=$'\n\t'

DNSMASQ_LISTEN="127.0.0.53"

# --------------------------------------------------------------------------
# 1. Upstream DNS for allowed domains
# --------------------------------------------------------------------------
UPSTREAM_DNS="8.8.8.8"
echo "Upstream DNS: ${UPSTREAM_DNS}"

# --------------------------------------------------------------------------
# 2. Configure dnsmasq as allowlist DNS proxy
# --------------------------------------------------------------------------
mkdir -p /etc/dnsmasq.d

cat > /etc/dnsmasq.d/allowlist.conf << EOF
# Listen on local address only
listen-address=${DNSMASQ_LISTEN}
bind-interfaces
port=53

# Don't read /etc/resolv.conf or poll for changes
no-resolv
no-poll

# =====================================================
# ALLOWED DOMAIN PATTERNS
# Each server= line forwards that domain + all subdomains
# to the upstream DNS. Unmatched domains get REFUSED.
# =====================================================

# GitHub (git, gh CLI, PR workflows, raw content)
server=/github.com/${UPSTREAM_DNS}
server=/githubusercontent.com/${UPSTREAM_DNS}

# GitHub Copilot CLI / editor endpoints
server=/githubcopilot.com/${UPSTREAM_DNS}
server=/api.business.githubcopilot.com/${UPSTREAM_DNS}
server=/exp-tas.com/${UPSTREAM_DNS}
server=/ghe.com/${UPSTREAM_DNS}

# Certificate revocation/OCSP endpoints used by Copilot TLS chains
server=/digicert.com/${UPSTREAM_DNS}
server=/symantec.com/${UPSTREAM_DNS}
server=/symcb.com/${UPSTREAM_DNS}
server=/symcd.com/${UPSTREAM_DNS}
server=/geotrust.com/${UPSTREAM_DNS}
server=/thawte.com/${UPSTREAM_DNS}
server=/verisign.com/${UPSTREAM_DNS}
server=/globalsign.com/${UPSTREAM_DNS}
server=/ssl.com/${UPSTREAM_DNS}
server=/identrust.com/${UPSTREAM_DNS}
server=/sectigo.com/${UPSTREAM_DNS}
server=/usertrust.com/${UPSTREAM_DNS}

# npm (Claude Code install/updates)
server=/npmjs.org/${UPSTREAM_DNS}
server=/npmjs.com/${UPSTREAM_DNS}

# Anthropic (API, feature flags)
server=/anthropic.com/${UPSTREAM_DNS}

# Error reporting & feature flags
server=/sentry.io/${UPSTREAM_DNS}
server=/statsig.com/${UPSTREAM_DNS}

# VS Code (marketplace, extensions, updates)
server=/visualstudio.com/${UPSTREAM_DNS}

# Azure Blob Storage — covers ALL *.blob.core.windows.net
server=/core.windows.net/${UPSTREAM_DNS}

# NuGet
server=/nuget.org/${UPSTREAM_DNS}

# Anthropic
server=/claude.com/${UPSTREAM_DNS}

# Azure DevOps (Uno Features feed)
server=/dev.azure.com/${UPSTREAM_DNS}

# Azure CDN (.NET workloads, SDK downloads)
server=/azureedge.net/${UPSTREAM_DNS}

# Azure AD auth (required for DevOps feed auth)
server=/microsoftonline.com/${UPSTREAM_DNS}

# .NET SDK & workloads
server=/dotnet.microsoft.com/${UPSTREAM_DNS}

# Microsoft Learn (MCP documentation server)
server=/learn.microsoft.com/${UPSTREAM_DNS}

# Uno domains
server=/platform.uno/${UPSTREAM_DNS}
server=/unoplatform.net/${UPSTREAM_DNS}

# Figma MCP
server=/figma.com/${UPSTREAM_DNS}

# Playwright
server=/cdn.playwright.dev/${UPSTREAM_DNS}
server=/storage.googleapis.com/${UPSTREAM_DNS}
EOF

# --------------------------------------------------------------------------
# 3. Start dnsmasq
# --------------------------------------------------------------------------
echo "Starting dnsmasq DNS proxy..."
dnsmasq --conf-file=/etc/dnsmasq.d/allowlist.conf --test 2>&1 && echo "  dnsmasq config OK"
pkill dnsmasq 2>/dev/null || true
sleep 0.2
dnsmasq --conf-file=/etc/dnsmasq.d/allowlist.conf
sleep 0.2
if pgrep dnsmasq >/dev/null; then
    echo "  dnsmasq running on ${DNSMASQ_LISTEN} (pid $(pgrep dnsmasq))"
else
    echo "ERROR: dnsmasq failed to start"
    exit 1
fi

# --------------------------------------------------------------------------
# 4. Point resolver at dnsmasq
# --------------------------------------------------------------------------
# Docker bind-mounts resolv.conf; take control for DNS filtering
cp /etc/resolv.conf /etc/resolv.conf.docker.bak
umount /etc/resolv.conf 2>/dev/null || true
echo "nameserver ${DNSMASQ_LISTEN}" > /etc/resolv.conf

# Verify resolv.conf was actually changed — fail hard if not
if grep -q "${DNSMASQ_LISTEN}" /etc/resolv.conf; then
    echo "  DNS resolver pointed to dnsmasq"
else
    echo "ERROR: resolv.conf was not updated — DNS filtering is NOT active"
    echo "  Contents:"
    cat /etc/resolv.conf
    exit 1
fi

echo ""
echo "========================================="
echo " DNS-based firewall configured"
echo "========================================="

# --------------------------------------------------------------------------
# 5. Verification
# --------------------------------------------------------------------------
echo ""
echo "Verifying..."

# Should be BLOCKED (DNS refuses resolution)
if curl --connect-timeout 5 https://example.com >/dev/null 2>&1; then
    echo "ERROR: Firewall verification failed — was able to reach https://example.com"
    exit 1
else
    echo "  PASS: https://example.com is blocked (DNS refused)"
fi

# Should be ALLOWED
if ! curl --connect-timeout 5 https://api.github.com/zen >/dev/null 2>&1; then
    echo "ERROR: Firewall verification failed — unable to reach https://api.github.com"
    exit 1
else
    echo "  PASS: https://api.github.com is reachable"
fi

if ! curl --connect-timeout 5 https://api.nuget.org/v3/index.json >/dev/null 2>&1; then
    echo "ERROR: Firewall verification failed — unable to reach https://api.nuget.org"
    exit 1
else
    echo "  PASS: https://api.nuget.org is reachable"
fi

if ! curl --connect-timeout 5 https://api.githubcopilot.com/_ping >/dev/null 2>&1; then
    echo "ERROR: Firewall verification failed — unable to reach https://api.githubcopilot.com"
    exit 1
else
    echo "  PASS: https://api.githubcopilot.com is reachable"
fi

if ! curl --connect-timeout 5 https://copilot-proxy.githubusercontent.com/_ping >/dev/null 2>&1; then
    echo "ERROR: Firewall verification failed — unable to reach https://copilot-proxy.githubusercontent.com"
    exit 1
else
    echo "  PASS: https://copilot-proxy.githubusercontent.com is reachable"
fi

if ! curl --connect-timeout 5 https://default.exp-tas.com >/dev/null 2>&1; then
    echo "ERROR: Firewall verification failed — unable to reach https://default.exp-tas.com"
    exit 1
else
    echo "  PASS: https://default.exp-tas.com is reachable"
fi

echo ""
echo "Firewall verification passed"

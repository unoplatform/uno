#!/usr/bin/env bash
set -euo pipefail

sudo /usr/local/bin/init-firewall.sh

mkdir -p "$HOME/.local/share/Uno Platform"
if [ -d /tmp/uno-platform-host ]; then
  find /tmp/uno-platform-host -maxdepth 1 -type f -exec cp {} "$HOME/.local/share/Uno Platform/" \;
else
  echo "Note: /tmp/uno-platform-host not found — skipping Uno Platform license copy"
fi

dotnet dev-certs https --trust || true

printf 'claude --dangerously-skip-permissions\n' >> "$HOME/.bash_history"

# Alias so bare `claude` always starts in bypass-permissions mode (safe in devcontainer)
for rc in "$HOME/.bashrc" "$HOME/.zshrc"; do
  if [ -f "$rc" ] && ! grep -q 'alias claude=' "$rc"; then
    printf '\nalias claude="claude --dangerously-skip-permissions"\n' >> "$rc"
  fi
done

echo "Registering Claude MCPs for Uno Platform: uno (HTTP docs server) and uno-app (stdio app tooling)."
echo "To verify, run: claude mcp list"

claude mcp add --scope user --transport http uno https://mcp.platform.uno/v1 || true
claude mcp add --scope user --transport stdio uno-app -- dotnet dnx -y uno.devserver --mcp-app || true

echo "Claude MCP registration complete. If you encounter issues, run 'claude mcp list' or 'claude mcp inspect uno' / 'claude mcp inspect uno-app'."

# ---------------------------------------------------------------------------
# Claude Code status line
# ---------------------------------------------------------------------------
cat > "$HOME/.claude/statusline.sh" << 'STATUSLINE'
#!/bin/bash
export LANG=C.UTF-8
input=$(cat)

MODEL=$(echo "$input" | jq -r '.model.display_name')
DIR=$(echo "$input" | jq -r '.workspace.current_dir')
COST=$(echo "$input" | jq -r '.cost.total_cost_usd // 0')
PCT=$(echo "$input" | jq -r '.context_window.used_percentage // 0' | cut -d. -f1)
DURATION_MS=$(echo "$input" | jq -r '.cost.total_duration_ms // 0')

CYAN='\033[36m'; GREEN='\033[32m'; YELLOW='\033[33m'; RED='\033[31m'; RESET='\033[0m'

# Pick bar color based on context usage
if [ "$PCT" -ge 90 ]; then BAR_COLOR="$RED"
elif [ "$PCT" -ge 70 ]; then BAR_COLOR="$YELLOW"
else BAR_COLOR="$GREEN"; fi

FILLED=$((PCT / 10)); EMPTY=$((10 - FILLED))
BAR=$(printf "%${FILLED}s" | tr ' ' '=')$(printf "%${EMPTY}s" | tr ' ' '-')

MINS=$((DURATION_MS / 60000)); SECS=$(((DURATION_MS % 60000) / 1000))

BRANCH=""
git rev-parse --git-dir > /dev/null 2>&1 && BRANCH=" | 🌿 $(git branch --show-current 2>/dev/null)"

echo -e "${CYAN}[$MODEL]${RESET} 📁 ${DIR##*/}$BRANCH"
COST_FMT=$(printf '$%.2f' "$COST")
echo -e "${BAR_COLOR}${BAR}${RESET} ${PCT}% | ${YELLOW}${COST_FMT}${RESET} | ⏱️ ${MINS}m ${SECS}s"
STATUSLINE
chmod +x "$HOME/.claude/statusline.sh"

# Deep-merge statusLine config into Claude settings (preserves existing nested keys)
CLAUDE_SETTINGS="$HOME/.claude/settings.json"
STATUSLINE_CFG='{"skipDangerousModePermissionPrompt":true,"permissions":{"defaultMode":"bypassPermissions"},"statusLine":{"type":"command","command":"~/.claude/statusline.sh","padding":2}}'
if [ -f "$CLAUDE_SETTINGS" ]; then
  if jq empty "$CLAUDE_SETTINGS" 2>/dev/null; then
    jq ". * $STATUSLINE_CFG" "$CLAUDE_SETTINGS" > "${CLAUDE_SETTINGS}.tmp" && mv "${CLAUDE_SETTINGS}.tmp" "$CLAUDE_SETTINGS"
  else
    echo "Warning: $CLAUDE_SETTINGS contains invalid JSON. Backing up to ${CLAUDE_SETTINGS}.bak" >&2
    cp "$CLAUDE_SETTINGS" "${CLAUDE_SETTINGS}.bak"
    echo "$STATUSLINE_CFG" | jq . > "$CLAUDE_SETTINGS"
  fi
else
  echo "$STATUSLINE_CFG" | jq . > "$CLAUDE_SETTINGS"
fi

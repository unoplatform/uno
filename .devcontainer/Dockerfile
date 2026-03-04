FROM mcr.microsoft.com/dotnet/sdk:10.0

ARG TZ
ENV TZ="$TZ"

ARG CLAUDE_CODE_VERSION=latest

# ---------------------------------------------------------------------------
# System packages: core dev tools + firewall utilities
# ---------------------------------------------------------------------------
RUN apt-get update && apt-get install -y --no-install-recommends \
  less \
  git \
  procps \
  sudo \
  fzf \
  zsh \
  man-db \
  unzip \
  gnupg2 \
  jq \
  nano \
  vim \
  # DNS-based firewall
  dnsmasq \
  iptables \
  iproute2 \
  dnsutils \
  # Uno / Skia desktop dependencies (from build/ci/templates/linux-install-deps.yml)
  xvfb \
  fluxbox \
  vlc \
  libvlc-dev \
  libgtk-3-dev \
  libwebkit2gtk-4.1-dev \
  # Misc
  curl \
  wget \
  ca-certificates \
  && apt-get clean && rm -rf /var/lib/apt/lists/*

# ---------------------------------------------------------------------------
# GitHub CLI (not in bookworm defaults)
# ---------------------------------------------------------------------------
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg \
      | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg \
  && chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg \
  && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" \
      > /etc/apt/sources.list.d/github-cli.list \
  && apt-get update && apt-get install -y --no-install-recommends gh \
  && apt-get clean && rm -rf /var/lib/apt/lists/*

# ---------------------------------------------------------------------------
# Node.js 20 (required for Claude Code npm install)
# ---------------------------------------------------------------------------
RUN curl -fsSL https://deb.nodesource.com/setup_20.x | bash - \
  && apt-get install -y --no-install-recommends nodejs \
  && apt-get clean && rm -rf /var/lib/apt/lists/*

# ---------------------------------------------------------------------------
# Playwright system dependencies (Chromium needs libnss3, libnspr4, etc.)
# ---------------------------------------------------------------------------
RUN npx playwright install-deps chromium

# ---------------------------------------------------------------------------
# Non-root user: developer (uid 1000)
# Container is the security boundary, not in-container privileges.
# ---------------------------------------------------------------------------
ARG USERNAME=developer
RUN existing_user=$(getent passwd 1000 | cut -d: -f1 || true) && \
  if [ -n "$existing_user" ] && [ "$existing_user" != "${USERNAME}" ]; then \
    usermod -l ${USERNAME} -d /home/${USERNAME} -m "$existing_user" && \
    groupmod -n ${USERNAME} "$existing_user" 2>/dev/null || true; \
  elif [ -z "$existing_user" ]; then \
    useradd -m -s /bin/zsh -u 1000 ${USERNAME}; \
  fi && \
  chsh -s /bin/zsh ${USERNAME} && \
  echo "${USERNAME} ALL=(ALL) NOPASSWD:ALL" > /etc/sudoers.d/${USERNAME} && \
  chmod 0440 /etc/sudoers.d/${USERNAME}

# Persist shell history (env vars are set in the zsh-in-docker -a flag below)
RUN mkdir -p /commandhistory && \
  touch /commandhistory/.bash_history && \
  chown -R ${USERNAME} /commandhistory

# Set DEVCONTAINER env
ENV DEVCONTAINER=true
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1

# Ensure non-root user owns /usr/local/share for npm global installs
RUN mkdir -p /usr/local/share/npm-global && \
  chown -R ${USERNAME}:${USERNAME} /usr/local/share

# Create workspace and config directories
RUN mkdir -p /uno /home/${USERNAME}/.claude /home/${USERNAME}/.nuget && \
  chown -R ${USERNAME}:${USERNAME} /uno /home/${USERNAME}/.claude /home/${USERNAME}/.nuget

WORKDIR /uno

# ---------------------------------------------------------------------------
# zsh + powerlevel10k (shell UX from reference)
# ---------------------------------------------------------------------------
ARG ZSH_IN_DOCKER_VERSION=1.2.0
USER ${USERNAME}
RUN sh -c "$(wget -O- https://github.com/deluan/zsh-in-docker/releases/download/v${ZSH_IN_DOCKER_VERSION}/zsh-in-docker.sh)" -- \
  -p git \
  -p fzf \
  -a "source /usr/share/doc/fzf/examples/key-bindings.zsh 2>/dev/null || true" \
  -a "source /usr/share/doc/fzf/examples/completion.zsh 2>/dev/null || true" \
  -a "export PROMPT_COMMAND='history -a' && export HISTFILE=/commandhistory/.bash_history" \
  -x

ENV SHELL=/bin/zsh
ENV EDITOR=nano
ENV VISUAL=nano

# npm global prefix — writable by non-root user (matches Anthropic reference)
ENV NPM_CONFIG_PREFIX=/usr/local/share/npm-global
ENV PATH=$PATH:/usr/local/share/npm-global/bin

# ---------------------------------------------------------------------------
# Claude Code CLI
# ---------------------------------------------------------------------------
RUN npm install -g @anthropic-ai/claude-code@${CLAUDE_CODE_VERSION}

# ---------------------------------------------------------------------------
# .NET workloads (pre-install so builds work immediately)
# ---------------------------------------------------------------------------
USER root
RUN dotnet workload install wasm-tools

# ---------------------------------------------------------------------------
# uno-check (validate environment, from build/ci/scripts/run-uno-check.sh)
# Installed as developer so the tool is usable interactively.
# ---------------------------------------------------------------------------
USER ${USERNAME}
ENV PATH="${PATH}:/home/${USERNAME}/.dotnet/tools"
RUN dotnet tool install --global uno.check
USER root
RUN uno-check -v --ci --non-interactive --fix \
  --skip xcode --skip gtk3 --skip vswin --skip vswinworkloads --skip unosdk \
  --skip dotnetnewunotemplates --skip vsmac --skip androidsdk --skip openjdk --skip androidemulator \
  --tfm net10.0-desktop --tfm net10.0-browserwasm \
  || true

# ---------------------------------------------------------------------------
# Neutralise VS Code git-credential forwarding
# dpkg-divert moves the real binary to /usr/bin/git.distrib so that even
# direct /usr/bin/git calls go through the wrapper.
# ---------------------------------------------------------------------------
RUN dpkg-divert --rename --add /usr/bin/git && \
  printf '#!/bin/sh\nunset GIT_ASKPASS VSCODE_GIT_ASKPASS_MAIN VSCODE_GIT_ASKPASS_NODE VSCODE_GIT_IPC_HANDLE SSH_AUTH_SOCK\nexec /usr/bin/git.distrib -c credential.helper= "$@"\n' \
  > /usr/bin/git && \
  chmod +x /usr/bin/git

# ---------------------------------------------------------------------------
# DNS allowlist filter
# ---------------------------------------------------------------------------
COPY init-firewall.sh /usr/local/bin/
RUN chmod +x /usr/local/bin/init-firewall.sh && \
  echo "${USERNAME} ALL=(root) NOPASSWD: /usr/local/bin/init-firewall.sh" > /etc/sudoers.d/${USERNAME}-firewall && \
  chmod 0440 /etc/sudoers.d/${USERNAME}-firewall

USER ${USERNAME}

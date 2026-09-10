---
uid: Uno.GetStarted.AI.Claude
---

# Get Started with Claude Code

This guide will walk you through the setup process for getting started with Claude Code.

## Check your environment

[!include[getting-help](includes/use-uno-check-inline-noheader.md)]

## Setting up Uno Platform MCPs

1. Install [Claude Code](https://code.claude.com/docs/en/overview) from the CLI
1. Register the Uno Platform MCPs:

    ```bash
    claude mcp add --scope user --transport http uno https://mcp.platform.uno/v1
    claude mcp add --scope user --transport stdio "uno-app" -- dotnet dnx -y uno.devserver --mcp-app
    ```

1. Start Claude Code in your terminal and then run:

    ```bash
    /mcp
    ```

    This will show the Uno Platform MCPs available to the agent.

    > [!IMPORTANT]
    > The `uno-app` MCP [may fail to load](https://github.com/anthropics/claude-code/issues/4384) unless Claude is opened in a folder containing an Uno Platform app.

## Setting up the Uno Platform Skills

Uno Platform ships a catalog of agent skills — covering areas such as MVUX, navigation, Uno Toolkit controls, theming, and UI testing — bundled in the `uno-platform-studio` plugin. Once installed, Claude Code automatically selects the relevant skills as it works on your prompts.

1. In Claude Code, install the plugin:

    ```text
    /plugin marketplace add unoplatform/studio
    /plugin install uno-platform-studio@uno-platform
    ```

1. If Claude Code prompts you to run `/reload-plugins`, do so to apply the new plugin.

For the full skill catalog, update instructions, and other installation options, see [Skills & Plugins](xref:Uno.PlatformStudio.Skills).

## Using Claude Code in a cloud environment

[Claude Code on the web](https://code.claude.com/docs/en/claude-code-on-the-web) runs each session on a fresh Ubuntu 24.04 virtual machine. The .NET SDK is not pre-installed there, so the `uno-app` MCP fails to start with `ENOENT: Executable not found in $PATH: dotnet`. Install the SDK with a [setup script](https://code.claude.com/docs/en/cloud-environments#setup-scripts), which runs once before Claude Code launches.

1. Open [claude.ai/code](https://claude.ai/code), then open the environment selector and edit the environment your sessions use.

1. In the **Setup script** field, enter:

    ```bash
    #!/bin/bash
    export DEBIAN_FRONTEND=noninteractive DOTNET_CLI_TELEMETRY_OPTOUT=1

    # Disable third-party PPAs that are not on the default network allowlist
    grep -rlE 'launchpadcontent\.net|ppa\.launchpad' /etc/apt/sources.list.d/ 2>/dev/null | xargs -r -I{} mv {} {}.disabled

    apt-get update -qq || true
    apt-get install -y -qq dotnet-sdk-10.0
    dotnet --version
    ```

1. If any project in your repository targets `net9.0`, add the following to the **Environment variables** field. Only the .NET 10 runtime is available from the Ubuntu 24.04 feed, so `net9.0` assemblies need to roll forward:

    ```text
    DOTNET_ROLL_FORWARD=LatestMajor
    ```

1. Save the environment, then start a **new** session. Sessions that are already running keep the configuration they started with.

1. In the session, run `dotnet --version` to confirm the SDK is present, then run `/mcp` to confirm the Uno Platform MCPs are connected.

The script writes to disk what later sessions need. Setup scripts must exit zero or the session fails to start, which is why `apt-get update` ends in `|| true` and the script ends in `dotnet --version` so a failed install surfaces immediately. After the script completes, the filesystem is snapshotted and reused, so later sessions start with the SDK already installed and skip the script.

> [!NOTE]
> .NET 10 is published in the Ubuntu 24.04 package feed, which the default **Trusted** network access level reaches through `archive.ubuntu.com`. .NET 9 is published only in the `ppa:dotnet/backports` repository, which is not on the default allowlist. Target `net10.0`, or set `DOTNET_ROLL_FORWARD` as shown above.

> [!TIP]
> Installing a workload, for example `dotnet workload install android`, adds several minutes to the script. Setup scripts should finish within roughly five minutes, so add workloads on their own line ending in `|| true` and confirm the base install works first.

## Next Steps

Now that you are set up, let's [create your first app](xref:Uno.GettingStarted.CreateAnApp.AI.Claude).

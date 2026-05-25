---
uid: Uno.Features.Uno.MCPs
---

# The Uno Platform MCPs

Uno Platform provides two [MCPs](https://modelcontextprotocol.io/docs/getting-started/intro):

- The Uno Platform Remote MCP, providing prompts and up-to-date documentation
- The Uno Platform Local App MCP, providing interactive access to your running application

This document explains how to interact with both those MCPs. You can find further below descriptions of the provided tools and prompts.

## MCP (Remote)

This is a remotely hosted publicly and provides:

- A set of tools to search and fetch Uno Platform documentation
- A set of prompts to create and develop Uno Platform applications.

### Predefined Prompts

The prompts provided by the MCP are automatically registered in your environment when supported by your AI agent (e.g., Claude, Codex, Copilot, etc.).

Here are the currently supported prompts:

- `/new`, used to create a new Uno Platform app with the best practices in mind.
- `/init`, used to "prime" your current chat with Uno's best practices. It's generally used in an existing app when adding new features.

### Sample Prompts for Uno MCP Servers

You can find common prompts to use with agents in our [getting started](xref:Uno.BuildYourApp.AI.Agents) section.

### Uno MCP Tools

The Uno MCP tools are the following:

- `uno_platform_docs_search` used by Agents to search for specific topics. It returns snippets of relevant information.
- `uno_platform_docs_fetch` used by Agents to get a specific document, grabbed through `uno_platform_docs_search`.
- `uno_platform_agent_rules_init` used by Agents to "prime" the environment on how to interact with Uno Platform apps during development.
- `uno_platform_usage_rules_init` used by Agents to "prime" the environment on how to Uno Platform's APIs in the best way possible

Those tools are suggested to the agent on how to be used best. In general, asking the agent "Make sure to search the Uno Platform docs to answer" will hint it to use those tools.

> [!NOTE]
> You can unselect `uno_platform_agent_rules_init` and `uno_platform_usage_rules_init` in your agent to avoid implicit priming, and you can use the `/init` prompt to achieve a similar result.

## App MCP (Local)

This MCP is running locally and provides agents with the ability to interact with a running app, in order to click, type, analyze or screenshot its content.

These tools give "eyes" and "hands" to Agents in order to validate their assumptions regarding the actions they take, and the code they generate.

> [!NOTE]
> If using Visual Studio 2022/2026, sometimes the Uno App MCP does not appear in the Visual Studio tools list. See [how to make the App MCP appear in Visual Studio](xref:Uno.UI.CommonIssues.AIAgents#the-app-mcp-does-not-appear-in-visual-studio).

### App MCP Tools

The following diagnostic tool is always available, even before the app connects:

- `uno_health`, used to get the health status of the DevServer MCP bridge, including connection state, tool count, discovered solutions, and any issues detected during startup

The Community license MCP app tools are:

- `uno_app_get_runtime_info`, used to get general information about the running app, such as its PID, OS, Platform, etc...
- `uno_app_get_screenshot`, used to get a screenshot of the running app
- `uno_app_pointer_click`, used to click at an X,Y coordinates in the app
- `uno_app_key_press`, used to type individual keys (possibly with modifiers)
- `uno_app_type_text`, used to type long strings of text in controls
- `uno_app_visualtree_snapshot`, used to get a textual representation of the visual tree of the app
- `uno_app_element_peer_default_action`, used to execute the default automation peer action on a UI element
- `uno_app_close`, used to close the running app
- `uno_app_start`, used to start the app with Hot Reload support

The Pro license App MCP app tools are:

- `uno_app_element_peer_action`, used to invoke a specific element automation peer action
- `uno_app_get_element_datacontext`, used to get a textual representation of the DataContext on a FrameworkElement

## Registering and diagnosing Uno MCPs

The `uno-devserver` CLI (see the full [Dev Server reference](xref:Uno.DevServer)) is the recommended way to register Uno MCPs. The `uno-devserver mcp install` command automatically picks the best registration strategy for each agent:

- **CLI-first**: For agents that provide their own MCP CLI (`claude`, `codex`, `gemini`), the tool delegates registration to the agent's own command. This ensures the config format is always correct and forward-compatible.
- **File-based fallback**: For agents without a CLI (or when the CLI is not installed), the tool writes directly to the agent's config file.

The fallback is transparent — if the agent CLI is not found in PATH or returns an error, file-based registration is used automatically.

### Agents with CLI support

| Agent | CLI executable | What `mcp install` does |
|---|---|---|
| Claude Code | `claude` | Runs `claude mcp add --scope project` |
| Codex CLI | `codex` | Runs `codex mcp add` |
| Gemini CLI | `gemini` | Runs `gemini mcp add -s project` |
| Cursor | `cursor-agent` | File-based (CLI is read-only) |
| OpenCode | `opencode` | File-based for install, CLI for uninstall |
| JetBrains Air | — | File-based (`.air/mcp.json`) |
| All others | — | File-based |

> [!TIP]
> You can always use the agent's own CLI directly instead of `uno-devserver mcp install`. Both approaches produce compatible registrations that `mcp status` can detect.

### Dev Server MCP setup commands

The `uno-devserver mcp` commands are useful when you want to:

- Register Uno MCPs across multiple agents at once (`--all-ides`)
- Inspect the current registration state across all supported clients
- Remove Uno MCP entries that were previously written

For the full list of Dev Server commands and flags, see the [Dev Server reference](xref:Uno.DevServer). To diagnose environment issues, see [Diagnostics (disco)](xref:Uno.Features.DevServerDisco).

The `Uno.DevServer` tool exposes the following MCP setup commands:

| Command | Purpose |
|---|---|
| `uno-devserver mcp status` | Inspect Uno MCP registrations across supported clients and config files |
| `uno-devserver mcp install <client>` | Register the Uno Platform MCPs for a supported client |
| `uno-devserver mcp uninstall <client>` | Remove Uno Platform MCP registrations for a supported client |

When running `mcp status`, the tool reports the detected clients, the config file path, transport, and the currently detected variant for each Uno MCP entry.

If you do not have `uno-devserver` installed globally, you can run the same commands transiently with `dotnet dnx -y uno.devserver`.

> [!NOTE]
> `uno-devserver mcp install <client>` is an alternative to manual JSON editing for supported file-backed clients. Native client-specific flows documented elsewhere in the docs remain fully valid.

### Common examples

```bash
# Check registration state across all detected clients
uno-devserver mcp status

# Install for a specific client
uno-devserver mcp install copilot-vscode
uno-devserver mcp install gemini-antigravity
uno-devserver mcp install copilot-vs

# Uninstall from a specific client
uno-devserver mcp uninstall cursor

# Install for all detected clients at once
uno-devserver mcp install --all-ides

# Preview changes without writing anything
uno-devserver mcp install cursor --dry-run

# Get machine-readable JSON output
uno-devserver mcp status --json
```

Equivalent transient usage (when `uno-devserver` is not installed globally):

```bash
dotnet dnx -y uno.devserver mcp status
dotnet dnx -y uno.devserver mcp install copilot-vscode
```

You can also control which Uno Dev Server package variant is expected when comparing or writing definitions:

```bash
uno-devserver mcp status --channel prerelease
uno-devserver mcp install copilot-vscode --tool-version 6.0.0-dev.123
```

Those flags select the Uno MCP definition variant. Any `dnx --prerelease` or `dnx --version` flags that appear in generated config files are derived from that selected variant.

> [!TIP]
> The legacy `uno-devserver --mcp-app` entry point remains valid. It is still the command ultimately used to expose the local App MCP over stdio.

### Command reference

#### Shared options

| Option | Applies to | Description |
|---|---|---|
| `<client>` | install, uninstall | Client identifier (positional). Required for install/uninstall unless `--all-ides` is used. Optional for status (reports all clients when omitted). |
| `--workspace <path>` | status, install, uninstall | Workspace root directory. Defaults to the current working directory. Must be an existing non-root directory. |
| `--channel <stable\|prerelease>` | status, install | Select the expected Uno MCP definition channel. Auto-detected from the tool version when omitted. |
| `--tool-version <ver>` | status, install | Pin a specific tool version in the server definition. Mutually exclusive with `--channel`. |
| `--servers <list>` | install, uninstall | Comma-separated server names (e.g. `UnoApp,UnoDocs`). Defaults to all servers. |
| `--all-ides` | install, uninstall | Target all detected clients instead of a single one. |
| `--all-scopes` | uninstall | Remove matching registrations from every config path, not just the write target. |
| `--dry-run` | install, uninstall | Preview operations without writing any files. |
| `--json` | status, install, uninstall | Emit JSON output to stdout instead of human-readable text. |

#### Exit codes

| Code | Meaning |
|---|---|
| 0 | Success (at least one operation succeeded, or status completed) |
| 1 | Runtime error (unknown client, definitions file not found, all operations failed) |
| 2 | Usage error (missing argument, invalid option, empty value, mutually exclusive flags) |

#### Reading `mcp status` output

When run without `--json`, `mcp status` prints a human-readable table showing:

- **Detected clients**: clients whose config directory exists on disk
- **Per-server, per-client status**: `registered` (matches expected), `outdated` (found but different), or `missing`
- **Locations**: the config file path and variant for each found entry
- **Warnings**: duplicate registrations across files or within the same file

Use `--json` for machine-readable output suitable for automation and IDE extensions.

### MCP Roots Compatibility

The App MCP uses [MCP roots](https://modelcontextprotocol.io/docs/concepts/roots) to determine which workspace directory to scan for solutions. Not all agents support this capability:

| Agent | Roots Support |
|-------|:------------:|
| Claude Code | Yes |
| copilot-vscode | Yes |
| copilot-vs | Yes |
| Cursor | Yes |
| Kiro | Yes |
| gemini-cli | Yes |
| copilot-cli | Yes |
| codex-cli | Yes |
| gemini-antigravity | No |
| Claude Desktop | No |
| Windsurf | No |
| junie-rider | No |
| JetBrains Air | No |
| OpenCode | Unknown |

For agents without roots support, the DevServer CLI auto-detects the missing capability and exposes the `uno_app_initialize` tool, allowing the agent to specify the workspace directory manually. No additional configuration is required. The legacy `--force-roots-fallback` flag is still accepted as an explicit override, but is rarely needed.

## Troubleshooting MCP Servers

You can find additional information about [troubleshooting AI Agents](xref:Uno.UI.CommonIssues.AIAgents) in our docs. For environment diagnostics, run `uno-devserver disco` — see [Diagnostics (disco)](xref:Uno.Features.DevServerDisco).

## See also

- [Dev Server](xref:Uno.DevServer) — the Dev Server CLI reference covering `disco`, `mcp`, and runtime flags.
- [Diagnostics (disco)](xref:Uno.Features.DevServerDisco) — inspect your local environment and Uno tool resolution.
- [Supported agents and features](xref:Uno.GetStarted#supported-agents-features) — per-agent capability summary.
- Per-agent setup guides: [Claude Code](xref:Uno.GetStarted.AI.Claude), [Codex CLI](xref:Uno.GetStarted.AI.Codex), [Cursor](xref:Uno.GetStarted.AI.Cursor), [GitHub Copilot CLI](xref:Uno.GetStarted.AI.CopilotCLI), [Google Antigravity](xref:Uno.GetStarted.AI.GoogleAntigravity).
- [Troubleshooting AI Agents](xref:Uno.UI.CommonIssues.AIAgents).

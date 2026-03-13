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

There are two valid ways to register the Uno Platform MCPs:

- Use the native MCP registration flow of your client when it provides one, such as Claude Code, Codex CLI, or GitHub Copilot CLI
- Use the `uno-devserver mcp` commands as a diagnostic tool and as an alternative registration flow for file-backed clients such as `copilot-vscode`, `copilot-vs`, `cursor`, `gemini-antigravity`, `opencode`, or `claude-desktop`

The `uno-devserver mcp` commands are particularly useful when you want to:

- Inspect the current registration state across supported clients
- Install the Uno MCP entries into a supported config file without editing JSON by hand
- Remove Uno MCP entries that were previously written to a supported config file

### Dev Server MCP setup commands

The `Uno.DevServer` tool exposes the following MCP setup commands:

| Command | Purpose |
|---|---|
| `uno-devserver mcp status` | Inspect Uno MCP registrations across supported clients and config files |
| `uno-devserver mcp install <client>` | Register the Uno Platform MCPs for a supported client |
| `uno-devserver mcp uninstall <client>` | Remove Uno Platform MCP registrations for a supported client |

When running `mcp status`, the tool reports the detected clients, the config file path, transport, and the currently detected variant for each Uno MCP entry.

If you do not have `uno-devserver` installed globally, you can run the same commands transiently with `dnx -y uno.devserver`.

> [!NOTE]
> `uno-devserver mcp install <client>` is an alternative to manual JSON editing for supported file-backed clients. Native client-specific flows documented elsewhere in the docs remain fully valid.

### Common examples

```bash
uno-devserver mcp status
uno-devserver mcp install copilot-vscode
uno-devserver mcp install gemini-antigravity
uno-devserver mcp install copilot-vs
uno-devserver mcp uninstall cursor
```

Equivalent transient usage:

```bash
dnx -y uno.devserver mcp status
dnx -y uno.devserver mcp install copilot-vscode
```

You can also control which Uno Dev Server package variant is expected when comparing or writing definitions:

```bash
uno-devserver mcp status --channel prerelease
uno-devserver mcp install copilot-vscode --tool-version 6.0.0-dev.123
```

Those flags select the Uno MCP definition variant. Any `dnx --prerelease` or `dnx --version` flags that appear in generated config files are derived from that selected variant.

> [!TIP]
> The legacy `uno-devserver --mcp-app` entry point remains valid. It is still the command ultimately used to expose the local App MCP over stdio.

### MCP Roots Compatibility

The App MCP uses [MCP roots](https://modelcontextprotocol.io/docs/concepts/roots) to determine which workspace directory to scan for solutions. Not all agents support this capability:

| Agent | Roots Support |
|-------|:------------:|
| Claude Code | Yes |
| copilot-vscode | Yes |
| copilot-vs | Yes |
| Cursor | Yes |
| gemini-antigravity | No |
| Claude Desktop | No |
| Windsurf | No |
| junie-rider | No |

For agents without roots support, the DevServer CLI uses the `--force-roots-fallback` flag to expose a `uno_app_set_roots` tool, allowing the agent to specify the workspace directory manually.

## Troubleshooting MCP Servers

You can find additional information about [troubleshooting AI Agents](xref:Uno.UI.CommonIssues.AIAgents) in our docs.

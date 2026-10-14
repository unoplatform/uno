---
uid: Uno.Features.DevServerAppCommands
---

# Driving the app from a shell (`app`)

The `uno-devserver app` command calls one [App MCP](xref:Uno.Features.Uno.MCPs#app-mcp-local) tool and exits with a code that a script can test. Shell scripts, CI jobs, and agent hooks get the same tools as an MCP client: start the app, inspect its visual tree, find elements, click, type, take screenshots, and check whether Hot Reload applied an edit.

```bash
uno-devserver app start --projectPath src/MyApp/MyApp.csproj --targetFramework net10.0-desktop
uno-devserver app wait --ready
uno-devserver app find --automationId SaveButton --waitForMs 5000
uno-devserver app screenshot --fileType png --out shot.png
uno-devserver app close
```

Run `uno-devserver app --help` for the same reference in the terminal.

## When to use it

- **Agent hooks**: a hook cannot call an MCP tool, but it can run a command and read its exit code. For example, run `app hot-reload-status --no-start --file <edited file>` after each edit.
- **CI and scripts**: exit codes act as assertions, for example `app find` exits 1 when nothing matched.
- **Reproducing what an agent did**: an MCP tool call becomes a command line that a person can run again.

## Prerequisites

- The `Uno.DevServer` tool (`uno-devserver`). See [Dev Server](xref:Uno.DevServer#command-line-advanced-usage-for-specific-scenarios).
- An Uno Platform solution in the current folder, or passed with `--solution-dir <path>`.
- The App MCP add-in. The tools on offer depend on its version and on your license: see [App MCP Tools](xref:Uno.Features.Uno.MCPs#app-mcp-tools). Run `uno-devserver app tools` to list them.

## Syntax

```bash
uno-devserver [--solution-dir <path>] app <verb|tool> [--param value]... [--json] [--out <file>] [--no-start]
```

When no DevServer runs for the solution, `app` starts one the same way `uno-devserver start` does, unless you pass `--no-start`. The tool result goes to stdout and logs go to stderr.

> [!NOTE]
> `uno-devserver start` starts the DevServer. `uno-devserver app start` builds and starts your app through the DevServer.

## Discovering tools and parameters

| Command | Output |
|---|---|
| `app tools` | One line per tool: `verb (tool name): description` |
| `app tools --json` | The tools with their input schemas: parameter names, types, and which ones are required |

Use `app tools --json` to find the parameters of a tool before calling it.

## Verbs

Each verb is a short name for one tool. Any other name is sent to the DevServer unchanged, so `app uno_app_get_screenshot --fileType png` works too, and so do tools that an add-in adds.

| Verb | Tool | Purpose |
|---|---|---|
| `start` | `uno_app_start` | Build and start the app with Hot Reload |
| `close` | `uno_app_close` | Close the running app |
| `restart` | `uno_app_restart` | Restart the app with the arguments of the last `start`, for example after a rude edit |
| `info` | `uno_app_get_runtime_info` | Process id, OS, platform, and Hot Reload state of the running app |
| `hot-reload-status` | `uno_app_hot_reload_status` | Whether Hot Reload applied a saved edit (saving the file triggers Hot Reload) |
| `screenshot` | `uno_app_get_screenshot` | Save a screenshot of the app window |
| `tree` | `uno_app_visualtree_snapshot` | Text snapshot of the visual tree, with an element reference (`^ref`) and `#Name` per element |
| `find` | `uno_app_find_elements` | Find elements, for example by automation id |
| `invoke` | `uno_app_element_peer_default_action` | Run the default automation peer action of an element |
| `action` | `uno_app_element_peer_action` | Run a specific automation peer action of an element |
| `click` | `uno_app_pointer_click` | Click at a position in the app |
| `key` | `uno_app_key_press` | Press a key, with modifiers |
| `type` | `uno_app_type_text` | Type a string |
| `datacontext` | `uno_app_get_element_datacontext` | Show the DataContext of an element |
| `memory` | `uno_app_get_memory_counters` | Memory counters of the running app |
| `diag` | `uno_devserver_diagnostics` | DevServer diagnostics |

### Script verbs

These verbs combine tool calls on the CLI side:

- `app wait [--timeoutSeconds 120] [--ready]` waits until an app is connected. With `--ready`, it then waits until the app's Hot Reload is Ready. It exits with 1 on timeout, or when Hot Reload is disabled.
- `app assert <find parameters> [--state tok,..] [--absent] [--count N]` runs `find`, then checks the result:
  - `--state`: each token must appear in the `{...}` state group of every match. A `!tok` token must not appear.
  - `--absent`: passes when nothing matched.
  - `--count N`: passes when exactly N elements matched.

  It prints the find result, then `assert: pass` or `assert: fail (<reason>)`.

## Parameters

| Option | Meaning |
|---|---|
| `--<name> <value>` | Sent to the tool as the `<name>` parameter. A value that parses as JSON (a number, `true`, `false`, `null`, `{...}`, `[...]`) is sent typed, anything else as text. A parameter that the tool declares as a string always gets text. |
| `--<name>` | Without a value (last, or followed by another `--option`): `true`. |
| `--json` | Print the raw MCP `CallToolResult` as JSON instead of its text. |
| `--out <file>` | Where an image result is saved. Default: `uno-app-<timestamp>.png` in the current folder. The saved path is printed. |
| `--no-start` | Never start a DevServer, and do not wait for tools it does not list yet: exit with 2 instead. Use it in hooks that run on every edit. |

Older App MCP versions identify elements by `--elementRef` only, using the `^ref` value from `app tree`. With those versions, `--name <x:Name>` is resolved through the visual tree for you. `--automationId` cannot be resolved this way, because the tree does not show automation ids.

## Exit codes

| Code | Meaning |
|---|---|
| 0 | The tool succeeded. |
| 1 | The tool reported an error, `find` matched nothing, `assert` failed, `hot-reload-status` reports the edit as not applied, `wait` timed out, or the DevServer could not start. |
| 2 | Bad arguments, no DevServer with `--no-start`, or the tool is not offered. |
| 3 | `hot-reload-status`: a rude edit that only `app restart` can apply. |

## Examples

Start the app and wait until it accepts Hot Reload edits:

```bash
uno-devserver app start --projectPath src/MyApp/MyApp.csproj --targetFramework net10.0-desktop
uno-devserver app wait --ready --timeoutSeconds 180
```

Check that an element appears within five seconds:

```bash
uno-devserver app find --automationId SaveButton --waitForMs 5000 || echo "Save button missing"
```

Check that exactly two rows are selected:

```bash
uno-devserver app assert --automationId Row --state selected --count 2
```

From a hook that runs after each file edit, report whether Hot Reload applied the edit, without starting a DevServer:

```bash
uno-devserver app hot-reload-status --file src/MyApp/MainPage.xaml --waitForMs 10000 --no-start
case $? in
  0) echo "applied" ;;
  3) uno-devserver app restart ;;
  *) echo "not applied, see the output above" ;;
esac
```

Save a screenshot:

```bash
uno-devserver app screenshot --fileType png --out artifacts/main.png
```

## Known limitations

- When `app` starts the DevServer itself, the DevServer inherits the CLI's stdout. A pipe such as `uno-devserver app tools | tail` then waits until the DevServer exits. Run `uno-devserver start` first, pass `--no-start`, or redirect stdout to a file.
- Each `app` call is a separate tool call. The CLI does not keep state between calls, apart from the DevServer and the running app.

## See also

- [The Uno Platform MCPs](xref:Uno.Features.Uno.MCPs): the App MCP tools and how to register them with an agent.
- [Dev Server](xref:Uno.DevServer): the Dev Server CLI reference.
- [Diagnostics (disco)](xref:Uno.Features.DevServerDisco): inspect your environment when the DevServer does not start.

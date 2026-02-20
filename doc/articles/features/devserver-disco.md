---
uid: Uno.Features.DevServerDisco
---

# Dev Server Diagnostics (`disco`)

The `disco` command inspects your development environment and reports what it finds — without invoking MSBuild. It is the fastest way to verify that the Dev Server can locate all the components it needs: the Uno SDK, the DevServer host process, add-ins, and the .NET runtime.

## When to use it

- The Dev Server does not start or Hot Reload is not working
- The App MCP fails to connect
- You want to verify your environment after installing or updating packages
- You need machine-readable output for CI or AI agents

## Prerequisites

The `disco` command is part of the Dev Server CLI. You can run it in two ways:

### [**.NET 10+**](#tab/net10)

No installation needed — use `dnx` to run it directly:

```bash
dnx -y uno.devserver disco
```

### [**.NET 9**](#tab/net9)

Install the CLI as a global tool, then run it:

```bash
dotnet tool install -g uno.devserver
uno-devserver disco
```

To update an existing installation:

```bash
dotnet tool update -g uno.devserver
```

---

> [!IMPORTANT]
> Keep the CLI up to date. An older version may not report all fields or may not resolve add-ins correctly.

## Syntax

```bash
uno-devserver disco [--json] [--addins-only]
```

| Option | Description |
|--------|-------------|
| *(none)* | Display a human-readable table with colored output |
| `--json` | Emit the full discovery result as a JSON object |
| `--addins-only` | Output only resolved add-in paths (semicolon-separated, or JSON array with `--json`) |

## Reading the output

The table is grouped into sections. Each row shows a **key** and a **value**. When a value could not be resolved, it appears as `<null>` with a hint about what is missing.

### Uno SDK

| Key | What it tells you |
|-----|-------------------|
| `source` | How the SDK was found (typically `global.json`) |
| `sourcePath` | Full path to the file that declares the SDK |
| `globalJsonPath` | Full path to the nearest `global.json` |
| `package` | SDK package id (`Uno.Sdk` or `Uno.Sdk.Private`) |
| `version` | SDK version declared in `global.json` |
| `sdkPath` | Resolved path in the NuGet cache |
| `packagesJsonPath` | Path to the SDK's `packages.json` manifest |

### DevServer

| Key | What it tells you |
|-----|-------------------|
| `devServerPackageVersion` | `Uno.WinUI.DevServer` version listed in `packages.json` |
| `devServerPackagePath` | Resolved package path in the NuGet cache |
| `hostPath` | Path to the host executable (`Uno.UI.RemoteControl.Host`) the Dev Server will launch |

### Settings

| Key | What it tells you |
|-----|-------------------|
| `settingsPackageVersion` | `uno.settings.devserver` version listed in `packages.json` |
| `settingsPackagePath` | Resolved package path in the NuGet cache |
| `settingsPath` | Path to `Uno.Settings.dll` |

### .NET

| Key | What it tells you |
|-----|-------------------|
| `dotNetVersion` | Raw output of `dotnet --version` |
| `dotNetTfm` | Computed target framework moniker (e.g. `net10.0`) used to locate the host |

### Add-Ins

| Key | What it tells you |
|-----|-------------------|
| `discoveryMethod` | How add-ins were discovered (e.g. `targets`) |
| `discoveryDurationMs` | Time taken for add-in discovery |
| *(add-in rows)* | Each resolved add-in shows its package name and DLL path |

### Warnings and errors

Warnings (yellow) indicate non-critical issues — for example, the Settings package is not present. Errors (red) indicate problems that will prevent the Dev Server from starting.

## Interpreting `<null>` values

A `<null>` value means the component could not be resolved. Use the hint next to it to understand what is missing.

| Key shows `<null>` | What to check |
|---------------------|---------------|
| `globalJsonPath` | No `global.json` found — run `disco` from your solution directory |
| `package` / `version` | `global.json` exists but does not declare `Uno.Sdk` (or `Uno.Sdk.Private`) under `msbuild-sdks` |
| `sdkPath` | The SDK package is not restored — run `dotnet restore` |
| `packagesJsonPath` | SDK package is restored but `packages.json` is missing inside it — the package may be corrupted |
| `devServerPackageVersion` | `Uno.WinUI.DevServer` is not listed in `packages.json` |
| `devServerPackagePath` | The DevServer package is not in the NuGet cache — run `dotnet restore` |
| `hostPath` | The host executable is not found for the current .NET TFM — your .NET version may not match the package |
| `dotNetVersion` / `dotNetTfm` | `dotnet --version` failed — verify your .NET SDK installation |
| `settingsPackageVersion` | `uno.settings.devserver` is not listed in `packages.json` (non-critical) |

## Exit code

| Code | Meaning |
|------|---------|
| `0` | All checks passed — no errors detected |
| `1` | One or more errors were reported |

## JSON mode

Pass `--json` to get the full discovery result as a JSON object, suitable for scripting, CI pipelines, or consumption by AI agents:

```bash
uno-devserver disco --json
```

The output contains all the same keys as the table, plus the `warnings` and `errors` arrays.

## Add-ins only mode

Pass `--addins-only` to output only the resolved add-in DLL paths:

```bash
# Semicolon-separated (default)
uno-devserver disco --addins-only

# JSON array
uno-devserver disco --addins-only --json
```

## Common diagnostic scenarios

### No `global.json` found

```
globalJsonPath     <null> (missing global.json in working directory or parents)
```

You are running `disco` outside of an Uno Platform project directory. Change to the directory containing your `.sln` or `.slnx` file.

### SDK package not restored

```
sdkPath            <null> (missing restored Uno.Sdk package in NuGet cache)
```

The Uno SDK version declared in `global.json` has not been restored yet. Run `dotnet restore` and try again.

### Host not found for current .NET version

```
hostPath           <null> (missing Uno.WinUI.DevServer host for current dotnet TFM)
dotNetTfm          net11.0
```

The DevServer package does not contain a host for the .NET version you are running. Verify that the `dotNetTfm` shown matches the .NET version supported by your Uno SDK version.

### DevServer package version missing

```
devServerPackageVersion  <null> (missing Uno.WinUI.DevServer entry in packages.json)
```

The SDK's `packages.json` does not reference `Uno.WinUI.DevServer`. This may indicate a corrupted or incomplete SDK package.

## See also

- [Dev Server overview](xref:Uno.DevServer)
- [Hot Reload](xref:Uno.Features.HotReload)
- [Issues related to AI Agents](xref:Uno.UI.CommonIssues.AIAgents)

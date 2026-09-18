---
description: Build, install, and run runtime tests against the WinUI (WinAppSDK) SamplesApp on Windows. Use when testing against native WinUI to validate parity with Uno.
---

## User Input

```text
$ARGUMENTS
```

You **MUST** consider the user input before proceeding (if not empty).

---

## Overview

You are executing the **WinUI Runtime Tests Skill**. It builds the WinAppSDK SamplesApp and runs runtime tests against **native WinUI** — the reference implementation Uno Platform targets.

The default path uses the **Windows App Development CLI** (`winapp run`): it registers the plain build output as a development (loose-layout) package and launches it. No MSIX packaging, no signing certificate, no admin elevation, no `Add-AppxPackage`. The app's console output streams straight to the terminal, and `winapp` waits for the app to exit.

An **MSIX fallback** remains for the rare cases that need a real package (see *MSIX fallback path*).

**Requirements**: Windows with **Developer Mode enabled** (loose-layout registration needs it), MSBuild (Visual Studio), and PowerShell (`pwsh` preferred). `winapp.exe` is **bundled** in the `Microsoft.Windows.SDK.BuildTools.WinApp` package the head already references, so `run-tests.ps1` finds it even with nothing installed globally (`winget install Microsoft.WinAppCli` also works).

**Helper scripts** live beside this file (`.claude/skills/winui-runtime-tests/`):

| Script | Purpose |
|--------|---------|
| `build-app.ps1` | Build the head (SDK pin + `global.json` swap + Graphics3DGL handled for you) |
| `run-tests.ps1` | Register the build output via `winapp` and run the tests **(default path)** |
| `parse-results.ps1` | Summarize the UTF-16 NUnit results, including Inconclusive |
| `cleanup.ps1` | Remove the package (MSIX-installed or `winapp`-registered) |
| `dotnet-root.ps1` | Dot-sourced by both runners — points `DOTNET_ROOT_<ARCH>` at the install holding the app's exact runtime in the app's architecture |
| `setup-cert.ps1` | *MSIX fallback only* — generate + trust a signing certificate (admin, once) |
| `install-msix.ps1` | *MSIX fallback only* — remove old package + install the built MSIX |
| `run-tests-msix.ps1` | *MSIX fallback only* — launch via execution alias, poll for results |

---

## Critical Pitfalls (Read First)

Every item below was hit in practice, not theorized:

1. **`crosstargeting_override.props` MUST be set to the windows TFM.** `src/crosstargeting_override.props` must contain `<UnoTargetFrameworkOverride>net11.0-windows10.0.19041.0</UnoTargetFrameworkOverride>`. Without it MSBuild resolves every TFM and drags in Skia/Wasm, producing hundreds of `CS0535: does not implement interface member 'DependencyObject.XXX'`. `build-app.ps1` refuses to build if it is missing or non-windows. **Restore the user's previous value when you are done.**

2. **The build needs the pinned preview SDK.** `master` targets net11, while the repo-root `global.json` sets `allowPrerelease: false` and would select a .NET 10 SDK (`NETSDK1045`, then a wall of `NETSDK1004`). CI copies `build/ci/net11/_global.json` over `global.json`; `build-app.ps1` does the same and **always restores it**. The SDK lives side-by-side at `%LOCALAPPDATA%\Microsoft\dotnet` and does *not* show up in `dotnet --list-sdks`.

3. **A previously MSIX-installed SamplesApp blocks `winapp`**: `A package with the same identity … is already installed as a non-development-mode package`. `run-tests.ps1` removes it automatically; `cleanup.ps1` also clears both kinds.

4. **Point `winapp` at the folder holding the app exe** (`bin\x64\Release\<tfm>\win-x64`), never at the `ForBundle` or `AppX` staging folders the packaging targets leave behind. Registering those fails with `0x80073B17: … ms-resource:PublisherDisplayName … NamedResource Not Found`. `run-tests.ps1` resolves this for you.

5. **`winapp run` re-stages the layout on every run** (into `<output>\AppX`, which is a *copy*). So after rebuilding, **always go through `run-tests.ps1` again** — launching `unosamplesapp.exe` directly would silently run the previously staged binaries.

6. **The app is framework-dependent on the preview runtime.** An alias launch inherits the environment, so `DOTNET_ROOT_<ARCH>` must point at an install carrying that .NET version in the exe's architecture (on ARM64, the x64 app needs the emulated `%ProgramFiles%\dotnet\x64` install, not the native one), or the app dies at startup before writing any results. Both runner scripts set it when needed.

7. **Inconclusive results are usually expected.** WinUI cannot change `Application.RequestedTheme` at runtime, so theme tests report *Inconclusive* unless the **OS theme** already matches (e.g. 9 of 21 `Given_Border`/`Given_Ellipse` cases are Inconclusive on a Dark-themed machine). Switch the OS theme and re-run to exercise them; do not report them as failures.

8. **Results are UTF-16 XML** — the `Read` tool blows its token budget and `head`/`cat` print garbage. Use `parse-results.ps1`.

9. **Don't count Inconclusive as passed.** `<test-run>` carries `total`, `passed`, `failed`, `inconclusive` and `skipped`; a summary that ignores `inconclusive` silently loses results.

10. **ParseArgs base64 truncation**: `App.Tests.cs:ParseArgs` must split on the *first* `=` (`Split('=', 2)`) or base64 filters ending in `=` padding are dropped and **all** tests run. If a filtered run executes everything, check this first.

11. **MSBuild switch syntax in bash**: forward-slash switches (`/r`, `/p:`) are read as Unix paths. Use `-restore`, `-t:Publish`, `-p:Configuration=Release` — or just call the helper scripts.

12. **PowerShell from bash**: complex inline `-Command` strings get mangled by bash escaping. Always run a `.ps1` file with `pwsh -NoProfile -ExecutionPolicy Bypass -File …`.

13. **MAX_PATH (260 chars)**: the PRI generator uses Win32 APIs with the 260-char limit. `PRI175`/`PRI252` errors mean the repo path is too long — shorten it or use a `subst` drive.

14. **Use folder mode, not project mode.** `winapp run <csproj>` (and therefore `dotnet run`, which the head's `Microsoft.Windows.SDK.BuildTools.WinApp` reference routes through `winapp`) drives `dotnet build` and failed here with `PRI175` / `PRI252: … .xbf not found`, while the same sources built fine through `MSBuild.exe -t:Build` — project mode's deeper intermediate paths run into the MAX_PATH-sensitive PRI generator. Build with `build-app.ps1`, then point `run-tests.ps1` at the **output folder**.

---

## Execution Workflow

### Phase 0: Parse User Input

Determine what to run:
- **All tests**: no filter
- **Test class**: e.g. `Given_Button` → resolve to the fully qualified name
- **Test method**: e.g. `Given_Button.When_ContentSet` → fully qualified
- **Multiple**: pipe-separated fully qualified names

Resolve partial names against `src/Uno.UI.RuntimeTests/Tests/`.

Keywords in the user input:

| Keyword | Effect |
|---------|--------|
| `strict` | Build with full CI analyzer coverage (`build-app.ps1 -Strict`, passes `UnoFastDevBuild=false`) |
| `debug` | Run with `-DebugOutput` — captures `OutputDebugString`, first-chance exceptions and, on a crash, a stowed-exception triage pass |
| `msix` | Use the MSIX fallback path instead of `winapp` |

### Phase 1: Prerequisites

1. **Set `src/crosstargeting_override.props`** (mandatory — see pitfall 1). Save the previous contents so you can restore them afterwards; create it from `crosstargeting_override.props.sample` if absent:
   ```xml
   <Project>
     <PropertyGroup>
       <UnoTargetFrameworkOverride>net11.0-windows10.0.19041.0</UnoTargetFrameworkOverride>
     </PropertyGroup>
   </Project>
   ```
2. **Confirm Developer Mode** is on (`AllowDevelopmentWithoutDevLicense` under `HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock`). Without it the loose-layout registration is refused.

No certificate and no elevation are needed on this path.

### Phase 2: Build

**Set the timeout to 600000 ms (10 min). NEVER cancel builds.**

```bash
pwsh -NoProfile -ExecutionPolicy Bypass -File .claude/skills/winui-runtime-tests/build-app.ps1
```

Add `-Strict` for `strict` mode. The script pins the SDK, swaps and restores `global.json`, passes `-p:BuildGraphics3DGLForWindows=true` (so the add-in gets its Windows TFM — no separate restore step) and `-p:UnoFastDevBuild=true` (`false` with `-Strict`; the command-line value wins over one set in `crosstargeting_override.props`). Concurrent runs in the same checkout wait for each other, since they share `global.json` and the head's `obj` folders.

#### Build failure diagnostics

| Error | Cause | Fix |
|-------|-------|-----|
| `CS0535: does not implement 'DependencyObject.XXX'` | `crosstargeting_override.props` missing/wrong TFM — Skia is being built | Set the windows TFM (Phase 1) |
| `NETSDK1045: does not support targeting .NET 11` | Repo-root `global.json` selected a .NET 10 SDK | Use `build-app.ps1` (pitfall 2) |
| `MSB4062: ResourcesGenerationTask_v0 could not be loaded` | Wrong TFM pulling unexpected dependencies | Set the windows TFM |
| `CS0012: type 'Grid' defined in unreferenced assembly 'Uno.UI'` | Graphics3DGL built Skia-only | `-p:BuildGraphics3DGLForWindows=true` (already in `build-app.ps1`) |
| `PRI175` / `PRI252: .xbf not found` | Path ≥ 260 chars | Shorten the repo path or `subst` |
| `MSB1008: Only one project` | Bash ate `/r` | Use dash syntax |

### Phase 3: Run the Tests

Encode the filter as base64 (pipe-separated, UTF-8):

```bash
FILTER=$(echo -n "Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Given_Border" | base64 -w 0)
```

```bash
# All tests
pwsh -NoProfile -ExecutionPolicy Bypass -File .claude/skills/winui-runtime-tests/run-tests.ps1 \
    -ResultsFile "$(pwd)/winui-test-results.xml"

# Filtered
pwsh -NoProfile -ExecutionPolicy Bypass -File .claude/skills/winui-runtime-tests/run-tests.ps1 \
    -ResultsFile "$(pwd)/winui-test-results.xml" -Filter "$FILTER"
```

Useful switches: `-DebugOutput` (crash triage), `-TimeoutSeconds` (default 600), `-KeepRegistered` (leave the dev package registered), `-OutputDir` (override output folder detection).

The app's console output — including each test name as it runs — streams live, so a hang is visible immediately rather than after a timeout.

### Phase 4: Parse Results

```bash
pwsh -NoProfile -ExecutionPolicy Bypass -File .claude/skills/winui-runtime-tests/parse-results.ps1 \
    -ResultsFile "$(pwd)/winui-test-results.xml"
```

Prints the `TOTAL / PASSED / FAILED / INCONCLUSIVE / SKIPPED` tally, then every non-passing case with its message (add `-ShowPassed` for the full list).

**Interpreting WinUI failures**: a test failing here reflects *native WinUI* behavior. A test that passes on Uno but fails on WinUI (or vice versa) is a parity gap. Exclude a test from WinUI with:
```csharp
[TestMethod]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
public void When_Test_That_Diverges_On_WinUI() { ... }
```

### Phase 5: Cleanup

1. **Restore `src/crosstargeting_override.props`** to the user's previous value.
2. Optionally remove the registered package:
   ```bash
   pwsh -NoProfile -ExecutionPolicy Bypass -File .claude/skills/winui-runtime-tests/cleanup.ps1
   ```
   (`run-tests.ps1` already unregisters on exit unless `-KeepRegistered` was passed.)

---

## Measured Timings

Windows 11, 32-core, warm NuGet cache, Release x64, `UnoFastDevBuild=true`, 21-test filter:

| Step | `winapp` (default) | MSIX fallback |
|------|--------------------|---------------|
| Build, cold | 5:26 | — |
| Build after a source edit | 4:30 | 5:03 (+33s packaging & signing) |
| Deploy | included in run | 14.9s `Add-AppxPackage` |
| Test run | 35s first run, ~10-12s subsequently | 23s |
| **Per iteration after an edit** | **~4:45** | **~5:41** |

The first `winapp run` stages ~600 MB into `<output>\AppX`; later runs sync only what changed, which is why they drop to ~10s. The MSIX path additionally needs a one-time certificate + admin elevation.

---

## MSIX Fallback Path

Use it only to validate the packaged artifact itself — MSIX packaging, signing, app identity or CI parity. It is slower and needs a certificate.

```bash
SKILL_DIR=".claude/skills/winui-runtime-tests"
# 1. Certificate (first time only; prompts UAC once)
pwsh -NoProfile -ExecutionPolicy Bypass -File "$SKILL_DIR/setup-cert.ps1"
THUMBPRINT=$(cat ~/.uno-dev-cert-thumbprint)

# 2. Build + package + sign (timeout 600000)
pwsh -NoProfile -ExecutionPolicy Bypass -File "$SKILL_DIR/build-app.ps1" -Mode Package -Thumbprint "$THUMBPRINT"

# 3. Install
pwsh -NoProfile -ExecutionPolicy Bypass -File "$SKILL_DIR/install-msix.ps1" -RepoRoot "."

# 4. Run + 5. parse (as in Phases 3-4, but via the alias runner)
pwsh -NoProfile -ExecutionPolicy Bypass -File "$SKILL_DIR/run-tests-msix.ps1" \
    -ResultsFile "$(pwd)/winui-test-results.xml" -Filter "$FILTER"
```

#### Install failure diagnostics

| Error | Cause | Fix |
|-------|-------|-----|
| `0x800B0109: root certificate must be trusted` | Cert not in `LocalMachine\Root` | Run `setup-cert.ps1` |
| `0x80073CFB: same identity already installed` | Old package present | `install-msix.ps1` handles it |
| `0x80073CF3: framework could not be found` | Installed Windows App Runtime older than the MSIX dependency | Install the runtime matching the `Microsoft.WindowsAppSDK` NuGet version |
| `0x80073D2C` / `0x80070057` | MSIX built unsigned | Rebuild with `-Thumbprint` |

Signing notes: use `PackageCertificateThumbprint` (not `PackageCertificateKeyFile`, which fails with `APPX0105` in many environments), and never `AppxPackageSigningEnabled=false` — the resulting MSIX cannot be installed. For certificates use `certreq` / `certutil`: the `Cert:` PSDrive and the PKI cmdlets (`New-SelfSignedCertificate`, `Import-PfxCertificate`) are unavailable in some environments. Never commit a PFX.

---

## Technical Reference

### Command-Line Arguments
| Argument | Description |
|----------|-------------|
| `--runtime-tests=<path>` | Absolute path for the NUnit XML results |
| `--runtime-test-filter=<base64>` | Base64-encoded, pipe-separated fully qualified test names |
| `--runtime-tests-group=<n>` / `--runtime-tests-group-count=<n>` | CI sharding |

The filter can also arrive via the `UITEST_RUNTIME_TESTS_FILTER` environment variable.

### Key Files
- **Project**: `src/SamplesApp/SamplesApp/SamplesApp.csproj` (windows TFM = `$(NetCurrentWinAppSDK)`)
- **App manifest**: `src/SamplesApp/SamplesApp/Package.appxmanifest` (alias `unosamplesapp.exe`)
- **Build output**: `src/SamplesApp/SamplesApp/bin/x64/Release/<tfm>/win-x64`
- **Entry point**: `src/SamplesApp/SamplesApp.Shared/App.Tests.cs`
- **Tests**: `src/Uno.UI.RuntimeTests/Tests/`
- **CI YAML / script**: `build/ci/tests/.azure-devops-tests-winappsdk.yml`, `build/test-scripts/run-winui-runtime-tests.ps1`
- **SDK pin**: `build/ci/net11/_global.json`

### Versions
| Item | Value |
|------|-------|
| WinAppSDK | 2.4.0 — keep the csproj, `src/Uno.Sdk/packages.json`, the CI runtime installer URL and this table in lockstep |
| Windows App Runtime (CI) | `https://aka.ms/windowsappsdk/2.4/2.4.0/windowsappruntimeinstall-x64.exe` — pin the exact stable version; `<minor>/latest` can resolve to an experimental build whose framework fails the MSIX dependency with `0x80073CF3` |
| `winapp` CLI | 0.6.x — bundled in `Microsoft.Windows.SDK.BuildTools.WinApp` (referenced by the head) |

### Inspecting a running app

`winapp ui` drives any running Windows app over UI Automation — useful when a test hangs and you want to see the live tree. Launch with `-KeepRegistered` (or run `winapp run <output> --detach`), then:

```bash
winapp ui list-windows -a SamplesApp.Windows
winapp ui inspect -a SamplesApp.Windows -d 6
winapp ui screenshot -a SamplesApp.Windows -o hang.png
```

---
description: Override a locally-built Uno.UI branch into a consumer app's restored NuGet packages so you can run/debug the app's real Skia-desktop head against your in-progress Uno changes. Use when you need to reproduce or validate a bug in an external Uno Platform app (e.g. under D:\Personal\...) against the current worktree, not just runtime tests.
---

## User Input

```text
$ARGUMENTS
```

Treat the user input as the **consumer app project path** (e.g. `D:\Personal\gc-toolbox\src\GcToolkit\GcToolkit.csproj`) and, optionally, the platform (default **net10.0-desktop** / Skia). You **MUST** consider it before proceeding.

---

## Overview

`UnoNugetOverrideVersion` copies the **locally-built Uno assemblies over the extracted NuGet package** the consumer app restored, so `dotnet run` on that app loads *your* branch's code. This is the only faithful way to reproduce an app-level bug (shell + real `Window` + `NavigationView`/`Frame` + transitions) that the runtime-test harness can't reproduce.

**Scope:** this runbook targets **Skia desktop** (`net10.0-desktop`), because the enhanced-lifecycle/theming code (`UNO_HAS_ENHANCED_LIFECYCLE`) only runs on Skia/WASM. Native Windows (`net10.0-windows…`, WinAppSDK) uses real WinUI, so your Uno changes aren't exercised there.

**The whole loop is whack-a-mole-prone.** Read the **Gotchas** section first — half the steps exist to work around file locks, the lib-vs-runtime split, and `buildTransitive` clobbering.

---

## Phase 0 — Discover what the app restored

The override must target the **exact package version** and **exact global-packages folder** the app uses.

```bash
APP=D:/Personal/gc-toolbox/src/GcToolkit/GcToolkit.csproj   # from $ARGUMENTS
ASSETS=$(dirname "$APP")/obj/project.assets.json
```

1. **Restored Uno.WinUI version** (often differs from the `Uno.Sdk` version in `global.json`!):
   ```bash
   grep -oE '"Uno.WinUI/[^"]+"' "$ASSETS" | sort -u
   ```
   Call this `$VER` (e.g. `6.7.0-dev.263`).

2. **Global packages folder** (may be a custom path, NOT `~/.nuget/packages`):
   ```bash
   python -c "import json;print('\n'.join(json.load(open(r'$ASSETS'))['packageFolders']))"
   ```
   Take the **first writable** entry — call it `$PKG` (e.g. `D:\Packages\NuGet`). All `dotnet` calls below set `NUGET_PACKAGES=$PKG` so restore AND the override-copy target both use it.

3. **Confirm the package is extracted** there:
   ```bash
   ls "$PKG/uno.winui/$VER/uno-runtime/net10.0/skia/Uno.UI.dll"
   ```

---

## Phase 1 — Build the overrides

Each Uno library project has an `AfterBuild` target (gated on `UnoNugetOverrideVersion`) that copies its output into
`$(NuGetPackageRoot)\<packageid>\<version>\uno-runtime\net10.0\skia\` (Skia runtime flavor).

A Skia-desktop app pulls Uno code from **three** packages — override all of them or you'll hit `MissingMethodException` at launch:

| Package (`CommonOverridePackageId`) | Build these projects | Assemblies |
|---|---|---|
| `uno.winui` | `src/Uno.UI/Uno.UI.Skia.csproj` (+ its project refs build Composition), `src/Uno.UI.FluentTheme*/*.Skia.csproj`, `src/Uno.UI.Toolkit/Uno.UI.Toolkit.Skia.csproj` | Uno.UI, Uno.UI.Composition, Uno.UI.FluentTheme(.v1/.v2), Uno.UI.Toolkit |
| `uno.winrt` | `src/Uno.UWP/Uno.Skia.csproj` | Uno.dll (the WinRT/`Uno.UWP` projection — `SystemThemeHelper` etc.) |
| `Uno.Foundation` | `src/Uno.Foundation/Uno.Foundation.Skia.csproj` | Uno.Foundation.dll |

Build them (from the Uno worktree root):

```bash
export NUGET_PACKAGES="$PKG"
COMMON="-c Release -f net10.0 -p:UnoNugetOverrideVersion=$VER -p:UnoTargetFrameworkOverride=net10.0 -p:UnoFastDevBuild=true"
for proj in \
  src/Uno.UI/Uno.UI.Skia.csproj \
  src/Uno.UI.FluentTheme/Uno.UI.FluentTheme.Skia.csproj \
  src/Uno.UI.FluentTheme.v1/Uno.UI.FluentTheme.v1.Skia.csproj \
  src/Uno.UI.FluentTheme.v2/Uno.UI.FluentTheme.v2.Skia.csproj \
  src/Uno.UI.Toolkit/Uno.UI.Toolkit.Skia.csproj \
  src/Uno.UWP/Uno.Skia.csproj \
  src/Uno.Foundation/Uno.Foundation.Skia.csproj ; do
  dotnet build "$proj" $COMMON 2>&1 | grep -iE "error|Time Elapsed";
done
```

> **Tip:** for a *fast inner loop* where you only changed `Uno.UI`, rebuild just `Uno.UI.Skia.csproj`. Override the rest once, up front.

---

## Phase 2 — Sync the compile reference + repair buildTransitive

Two **mandatory** post-build fixups (the override target does neither):

**2a. Compile reference (`lib/net10.0`).** The app *compiles* against `lib/net10.0/*.dll` but *runs* `uno-runtime/net10.0/skia/*.dll`. If your branch changed any **public API**, the app compiles against the stale lib and crashes at runtime (`MissingMethodException`). Copy the runtime DLL over the lib reference for every assembly whose API changed:

```bash
P="$PKG/uno.winui/$VER"
cp "$P/uno-runtime/net10.0/skia/Uno.UI.dll"            "$P/lib/net10.0/Uno.UI.dll"
cp "$P/uno-runtime/net10.0/skia/Uno.UI.Composition.dll" "$P/lib/net10.0/Uno.UI.Composition.dll"
```

**2b. Restore `buildTransitive` from the `.nupkg`.** Building `Uno.UI` rebuilds `Uno.UI.Tasks` and clobbers the package's hash-named build task (`Uno.UI.Tasks.<hash>.dll`) with a dev-named `v0` one, so the **consumer build then fails** with `MSB4062: RuntimeAssetsSelectorTask … could not be loaded`. Re-extract `buildTransitive/` from the original package after every override build:

```powershell
Add-Type -AssemblyName System.IO.Compression.FileSystem
$pkg="$env:PKG\uno.winui\$env:VER\uno.winui.$env:VER.nupkg"; $dest="$env:PKG\uno.winui\$env:VER"
$zip=[System.IO.Compression.ZipFile]::OpenRead($pkg)
foreach($e in $zip.Entries){ if($e.FullName -like 'buildTransitive/*' -and $e.Name){ $t=Join-Path $dest ($e.FullName -replace '/','\'); $d=Split-Path $t -Parent; if(-not(Test-Path $d)){New-Item -ItemType Directory -Force -Path $d|Out-Null}; [System.IO.Compression.ZipFileExtensions]::ExtractToFile($e,$t,$true) } }
$zip.Dispose()
```

---

## Phase 3 — Run the app

```bash
cd "$(dirname "$APP")/../.."   # app repo root
export NUGET_PACKAGES="$PKG"
dotnet run --project "$APP" -f net10.0-desktop -c Debug -p:SingleTargetFramework=net10.0-desktop
```

- Add **diagnostics by editing the Uno source**, rebuilding the override (Phase 1 for the one project), and re-running. `Console.WriteLine("[TAG] …")` lands in stdout; gate it tightly and remove before commit.
- Drive/observe the desktop window via **UI Automation** (PowerShell `System.Windows.Automation`) + screen-capture (`System.Drawing.Graphics.CopyFromScreen`), or run the app's **WASM** head in a browser for MCP-driven clicking + console logs.

---

## The iterate loop (rebuild a running override)

The package DLLs get **locked** by the running app *and* by persistent MSBuild worker nodes / the Roslyn analyzer. **Before every rebuild:**

```powershell
# kill the app
Get-Process | ? { $_.MainWindowTitle -like '*<AppTitle>*' } | Stop-Process -Force -EA SilentlyContinue
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" | ? { $_.CommandLine -like '*<App>.dll*' } | % { Stop-Process -Id $_.ProcessId -Force -EA SilentlyContinue }
# release the package-DLL lock held by reusable build nodes
Get-CimInstance Win32_Process -Filter "Name='dotnet.exe' OR Name='MSBuild.exe' OR Name='VBCSCompiler.exe'" | ? { $_.CommandLine -match 'nodemode:1|VBCSCompiler|RoslynCodeAnalysisService' } | % { Stop-Process -Id $_.ProcessId -Force -EA SilentlyContinue }
dotnet build-server shutdown
```

Then verify it's unlocked before building:
```powershell
$u="$env:PKG\uno.winui\$env:VER\uno-runtime\net10.0\skia\Uno.UI.dll"
try{[System.IO.File]::Open($u,'Open','ReadWrite','None').Close();'UNLOCKED'}catch{'LOCKED'}
```

Each iteration = **unlock → rebuild override (Phase 1) → sync lib + restore buildTransitive (Phase 2) → run (Phase 3)**.

---

## Gotchas (read first)

1. **Version mismatch:** the restored `Uno.WinUI` version ≠ the `Uno.Sdk` version in `global.json`. Always read it from `project.assets.json`.
2. **Custom global-packages folder:** apps frequently set `NUGET_PACKAGES` / a `packageFolders` entry to a non-default path (e.g. `D:\Packages\NuGet`). Override there, not in `~/.nuget`.
3. **Three packages, not one:** `MissingMethodException` for `…App(IUnoPlatformHostBuilder, Func<T>)` / `SystemThemeHelper.get_IsHighContrast` / a FluentTheme `Get_NNN` crash each mean *another* package (uno.winui core, uno.winrt, Uno.Foundation, or a FluentTheme assembly) still has the old DLL. Override the whole set.
4. **lib vs uno-runtime:** the override target only writes `uno-runtime/skia`. Public-API changes also need the `lib/net10.0` reference copied (Phase 2a) or the app compiles against stale API.
5. **buildTransitive clobber:** every `Uno.UI` override build breaks the consumer build until you restore `buildTransitive` (Phase 2b).
6. **File locks:** kill the app + MSBuild nodes + `dotnet build-server shutdown` before each rebuild, or the copy fails with `MSB3021 … being used by another process`.
7. **Skia only:** if the app runs as native Windows (WinAppSDK), your Uno changes aren't loaded — force `net10.0-desktop`.
8. **Clean up:** the override mutates files inside the shared NuGet cache. To revert, delete the version folder and let the app re-restore (or re-extract from the `.nupkg`).

# Single Uno.Sdk SamplesApp Head — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the four SamplesApp heads (`SamplesApp.Skia.Generic`, `SamplesApp.Skia.WebAssembly.Browser`, `SamplesApp.Skia.netcoremobile`, `SamplesApp.Windows`) with one `Uno.Sdk`-based multi-targeted head covering all Skia targets except MacCatalyst, plus the WinUI/WinAppSDK target.

**Architecture:** New head uses `Sdk="Uno.Sdk.Private"` (packed from local `src/Uno.Sdk`) for cross-platform MSBuild plumbing, but consumes the Uno framework from in-repo source via per-TFM `ProjectReference` with `DisableImplicitUnoPackages=true` + `DisableImplicitUnoWinAppSdkPackages=true`. A `SamplesAppUseImplicitPackages` switch (default `false`) flips both flags to exercise NuGet `UnoFeatures` resolution on demand. Skia renders all non-Windows TFMs; the `windows10.0` TFM uses native WinUI. Lands alongside the old heads; CI migrates per-target; old heads deleted last.

**Tech Stack:** MSBuild / `Uno.Sdk`, .NET 10, Skia runtimes (Win32/X11/FrameBuffer/macOS/WebAssembly.Browser/Android/AppleUIKit), WinAppSDK 2.1.3 / WinUI + MSIX, Azure DevOps YAML, PowerShell/bash test scripts.

**Spec:** `specs/051-samplesapp-unosdk-skia/spec.md`

## Global Constraints

- **TFMs:** `net10.0` only. Final head: `net10.0-desktop;net10.0-browserwasm;$(NetCurrentWinAppSDK);net10.0-android;net10.0-ios;net10.0-tvos`, where `$(NetCurrentWinAppSDK)` = `net10.0-windows10.0.19041.0`. Reference `Net*` properties from `Directory.Build.props`; never hardcode. (No MacCatalyst.)
- **Default mode:** `DisableImplicitUnoPackages=true` **and** `DisableImplicitUnoWinAppSdkPackages=true` (source `ProjectReference`s). The `SamplesAppUseImplicitPackages` switch (default `false`) is the only thing that flips them.
- **From-source build:** Uno framework via `ProjectReference` to in-repo projects in default mode; never NuGet for `Uno.*` framework assemblies. Non-framework deps (SkiaSharp native, Uno.Wasm.Bootstrap, AndroidX, Microsoft.WindowsAppSDK + SDK BuildTools, fonts, MSAL, MSTest) stay `PackageReference`, pinned via `PackageReference … Update` (CPM stays disabled — do **not** add `Directory.Packages.props`).
- **Windows renders native WinUI** (`UseWinUI=true`, no `Uno.UI` reference); all other TFMs render Skia.
- **Platform symbols & suffix exclusion** are owned by `src/Uno.CrossTargetting.targets` / `Uno.Sdk` — never set platform `DefineConstants` or `Compile Remove="**/*.skia.cs"` in the head csproj.
- **Code style:** tabs, Allman braces, `#nullable enable` per file, MUX/MIT headers only on ported code (none here). Comments explain *why*, short.
- **Copy:** any user-facing sample copy says "Uno Platform", never just "Uno".
- **Commits:** Conventional Commits. Commit each task's deliverable when it builds clean.
- **Never cancel builds.** Set generous timeouts.
- **Landing strategy:** the new head is **additive** until P6. Do not modify or delete the four existing heads, `SamplesApp.Skia` base lib, or their CI stages before P6 — only add the new head and (per target in P1–P5) repoint CI to it.

---

## File Structure

**New files (the head):**
- `src/SamplesApp/SamplesApp.Head/SamplesApp.Head.csproj` — the consolidated head (`AssemblyName=SamplesApp`).
- `src/SamplesApp/SamplesApp.Head/Platforms/Desktop/Program.cs` — desktop entry point.
- `src/SamplesApp/SamplesApp.Head/Platforms/WebAssembly/Program.cs` + web assets (P2).
- `src/SamplesApp/SamplesApp.Head/Platforms/Windows/` — WinUI `App`/`Main`, `Package.appxmanifest`, MSIX logo `Assets/`, `app.manifest` (P3).
- `src/SamplesApp/SamplesApp.Head/Platforms/Android/` — `Application` + `MainActivity` + `AndroidManifest.xml` + assets (P4).
- `src/SamplesApp/SamplesApp.Head/Platforms/iOS/` + `Platforms/tvOS/` — `Main.cs` + `Info.plist`/entitlements (P5).
- `src/SamplesApp/SamplesApp.Head/Assets/`, `app.manifest`, `Resources/Info.plist` (desktop) — migrated per phase.

**Modified files:**
- `global.json` — pin `Uno.Sdk.Private` sentinel.
- `src/Directory.Build.props` — register head in `_AdjustedOutputProjects`.
- `src/Uno.UI-Skia-only.slnf` — add (P1) then remove old heads (P6).
- `build/ci/tests/*.yml`, `build/ci/publish/*.yml`, `build/test-scripts/*.{ps1,sh}` — repoint per target (P1–P5), delete dead ones (P6). Includes `.azure-devops-tests-winappsdk.yml` (P3).

**Source-of-truth references** (read for exact package/ProjectReference lists when wiring each target):
- Desktop: `SamplesApp.Skia.Generic.csproj` + `SamplesApp.Skia.csproj`
- WASM: `SamplesApp.Skia.WebAssembly.Browser.csproj`
- Mobile: `SamplesApp.Skia.netcoremobile.csproj`
- Windows/WinUI: `SamplesApp.Windows.csproj`

---

## Plan structure note

**P0 and P1 are fully detailed and immediately actionable** — the gating, knowable work (local-SDK bootstrapping + the `Directory.Build` × `Uno.Sdk` layering spike + a working desktop head). **P2–P6 and P-implicit are concrete task sequences** (exact files, refs, verification, commits) that reference P1's established csproj pattern and the source-of-truth csprojs above rather than re-pasting full package lists, because their exact MSBuild conditions are finalized against what P0/P1 empirically prove. Expand P2+ into per-step detail when starting them.

---

## Implementation status snapshot (2026-06-24)

**One `Uno.Sdk` head (`src/SamplesApp/SamplesApp.Head`) targets all of `net10.0-{browserwasm,desktop,android,ios,tvos}` + `$(NetCurrentWinAppSDK)`.**

| Target | Builds from head | Validated |
|--------|------------------|-----------|
| desktop | ✅ `dotnet build` | locally (build+boot+runtime-test slice) |
| browserwasm | ✅ `dotnet build` | locally (build) |
| windows/WinUI | ✅ `MSBuild.exe` | locally (build) |
| android | ✅ `dotnet build` | locally (build) |
| ios + tvos | wired | ⚠️ needs macOS (not buildable on Windows) |

- **Content model:** all Skia TFMs reference the generic-Skia `SamplesApp.Skia` base lib (samples compile once as Skia); WinUI inlines + uses the real WinUI XAML compiler.
- **CI:** all platform stages + scripts repointed to the head (commit `f2f030edba`) — pending Azure DevOps validation.
- **Old heads NOT deleted** — entangled with shared `SamplesApp.Windows` resources (base lib + native `SamplesApp.Wasm`) and RuntimeTests app bin refs; do alongside the native-head removal.
- **Follow-ups:** wasm PWA icons; android Resizetizer splash-raster registration under Uno.Sdk single-project (worked around with a manual `splash_screen.png`); windows `dotnet run`/winapp dev-loop.

---

## Implementation outcomes — P0 + P1 (desktop) ✅ done & committed

**The hybrid approach is validated end-to-end on desktop.** Key empirical results for P2+ implementers:

- **Layering risk RETIRED, zero mitigations:** `Sdk="Uno.Sdk.Private"` (local pack) layered over the repo's `Directory.Build.props/.targets` + `Uno.CrossTargetting.targets` with no conflicts. Uno.Sdk's `CustomAfterDirectoryBuildProps` ordering handles it. No opt-outs were needed.
- **Pack mechanism:** `Uno.Sdk` has `GeneratePackageOnBuild=true` and a CoreCompile-time version-replacement dance — pack via **`dotnet build`** (not `dotnet pack`, which skips the compile → NU5026). `build/pack-local-uno-sdk.ps1` does this at sentinel `255.255.255-dev`.
- **Gotchas found & fixed (apply the same in P2+):**
  1. **`UnoRuntimeIdentifier=Skia` must be set EARLY** (top PropertyGroup, before the `sourcegenerators.local.props` import) for non-windows TFMs. Uno.Sdk sets it in `.targets` (too late), so the XAML source-generator props compute `IncludeXamlNamespaces` with `not_skia` → conditional namespaces like `xmlns:skia="http://uno.ui/skia"` get stripped → `CS0103` on x:Named fields (e.g. `BrowserInputHelper_Tests`). Fixed in the head csproj.
  2. Shared imports: the targets file is `SamplesApp.UnitTests.targets` (NOT `.Shared.targets`); the props is `.Shared.props`.
  3. `Uno.Extensions.Logging.WebAssembly.Console` package is needed on all Skia TFMs (`App.xaml.cs` uses it under `#if __SKIA__`).
- **No manual `Uno.UI.Tasks.targets` / `uno.winui.runtime-replace.targets` imports needed** — Uno.Sdk handles resource generation. (Simplification vs the old heads.)
- **Validation (desktop):** compile (full SamplesApp) ✓; boot (no startup crash) ✓; runtime (`Given_ThicknessConverter` 5/5 pass via `--runtime-tests`) ✓.
- **P1.6 (CI flip) is intentionally not done locally** — it requires editing Azure DevOps YAML + pushing + watching the pipelines, which can't be verified in a local session. Do it where the pipeline run can be observed.

### P2 (browserwasm) ✅ done & committed
- One head builds **both** net10.0-desktop and net10.0-browserwasm from source. Uno.Sdk auto-selects `Microsoft.NET.Sdk.WebAssembly` per TFM.
- Gotchas: `System.Management` → desktop-only (wasm wanted 9.0-rc via `Microsoft.Windows.Compatibility`); `Resizetizer`/`UnoIcon` gated off wasm (else `Uno.Wasm.Bootstrap` PWA-content generation fails — **no wasm PWA icons yet, follow-up**); TargetFrameworks desktop **not first** (UNOB0011); `Platforms/{Desktop,WebAssembly}` folders auto-stripped per TFM.
- The Skia groups are gated on `'$(UnoRuntimeIdentifier)' == 'Skia'` (true for all non-windows TFMs via the early-set).

### P3 (windows / WinAppSDK) ✅ COMPILES (enabled) — three targeted fixes
- The windows TFM (`$(NetCurrentWinAppSDK)`) is **back in `<TargetFrameworks>`** and builds the full sample set. Three fixes:
  1. **`AssemblyName=SamplesApp.Windows` on the windows TFM only** (per-TFM) — not in the Toolkit/Foundation IVT lists, so Toolkit.Windows's self-linked *internal* helpers stay invisible and the public `Uno.Core.Extensions` package APIs are used → no duplicate-type collision. (Skia TFMs keep `SamplesApp`.)
  2. **Self-link `Uno.UI\Extensions\DependencyObjectExtensions.cs`** on windows (no `Uno.UI` there; the packages don't provide it) — mirrors the old head.
  3. **Gate `sourcegenerators.local.props` to Skia only** — on WinUI the real WinUI XAML compiler handles XAML; the Uno XAML/DP generators must not run (they fail looking for Uno-internal types like `IDependencyObjectParse`).
- **Build tool:** builds with **`MSBuild.exe`** (`-r -p:UnoTargetFrameworkOverride=$(NetCurrentWinAppSDK) -p:Platform=x64`) — matches the existing `SamplesApp.Windows` CI. `dotnet build`/`dotnet run` currently **fail** here: the repo's WindowsAppSDK pulls the WinUI markup-compiler + MRT PRI tasks from `microsoft.windowsappsdk/1.0.0/tools`, which don't load under dotnet's out-of-proc task host (`MetadataLoadContext disposed`). This is a repo-WindowsAppSDK-tooling interaction, not the head's wiring. `winapp init` would enable `dotnet run` but sets up a parallel `winapp.yaml` SDK-pinning that conflicts with the repo's managed WindowsAppSDK 2.1.3 — out of scope here. **Decision: windows uses MSBuild.exe (like the current windows CI); `dotnet run`/winapp dev-loop support is a separate repo-infra follow-up.**
- Remaining for P3: flip the WinAppSDK CI stage (`.azure-devops-tests-winappsdk.yml`) to the new head (MSBuild.exe), and run the WinUI runtime tests.

### P4 (android) ✅ BUILDS — via the SamplesApp.Skia base lib
- **Resolved.** The fix was the **content-sourcing model**, not the runtime: the head now references the generic-Skia `SamplesApp.Skia` library on **all Skia TFMs** (desktop/wasm/android/ios/tvos) instead of inlining the sample globs. Inlining compiled the samples with `__ANDROID__`, so they took native code branches needing native-only types (`BindableButtonEx`, `BindableDrawerLayout`, native `FeatureConfiguration` flags, …) absent from `Uno.UI.Skia`. The base lib compiles them once as generic Skia. WinUI still inlines (different `Microsoft.UI.Xaml` type identity) + uses the real WinUI XAML compiler; the head no longer imports the Uno source generators.
- Splash: added a manual `Platforms/Android/Resources/drawable/splash_screen.png` to satisfy Resizetizer's auto-generated splash wrapper (its raster auto-registration doesn't fire under Uno.Sdk single-project; the app's real splash is `@drawable/splash` via `Theme.App.Starting`). **Follow-up:** the proper Resizetizer-android splash registration under Uno.Sdk single-project.
- Deleted the obsolete `Button_UseUWPDefaultStyles` sample.
- Remaining for P4: install/run the APK + flip Android CI (default/coreclr/nativeaot).

#### (historical) original P4 status — wired but TFM disabled
- All android wiring is committed (Platforms/Android entry points/manifest/resources/assets, `netcoremobile` framework refs, `SkiaSharp.NativeAssets.Android` + AndroidX + UniversalImageLoader, MSAL/GooglePlay/Foldable). The framework projects all build for net10.0-android.
- **BLOCKER — the Skia-on-mobile compilation model.** `src/Uno.CrossTargetting.targets` defines `__SKIA__` from `UnoRuntimeIdentifier==Skia` (line 77) and `__ANDROID__` from `IsAndroid` (line 105) **independently**, and includes `.skia.cs` (Skia) and `.Android.cs` (IsAndroid) **independently**. Skia-on-Android is meant to compile the **app code as generic Skia** (`UnoRuntimeIdentifier=Skia`, `IsAndroid` effectively *false*, `__SKIA__`/`.skia.cs`, **no** `__ANDROID__`) with only a thin native android host — the repo achieves this via **`RuntimeAssetsSelectorTask`** swapping the platform TFM to generic `netX` for app assemblies. Evidence: samples like `Button_UseUWPDefaultStyles.xaml.cs` take `#elif __ANDROID__ → BindableButtonEx` (a native type absent from `Uno.UI.Skia`), so they only compile when `__ANDROID__` is *not* defined (i.e. generic-Skia compile). 
  - This head currently composes android as **native** (`IsAndroid=true` → `__ANDROID__` + `.Android.cs`), so it hits CS0234 (`BindableButtonEx`) and CS0109. Setting `UnoRuntimeIdentifier=Skia` early instead gives BOTH `.skia.cs` + `.Android.cs` → CS0111. Neither is the generic-Skia model.
  - **Remaining work:** replicate the repo's Skia-on-mobile model in the Uno.Sdk head so android app code compiles as generic Skia (likely engaging `RuntimeAssetsSelectorTask` / making `IsAndroid` false for the app-code compile while keeping the native host). `$(NetCurrentWinAppSDK)`… er, `net10.0-android` is removed from `<TargetFrameworks>` until then; desktop+wasm+windows stay green.

### P5 (ios + tvos) — not started; blocked-by-design here
- Same Skia-on-mobile compilation model as P4 (resolve android first; ios/tvos mirror it). **Additionally cannot be built/validated on Windows — requires macOS** (the existing iOS CI builds on mac agents). Best done after P4's model is wired, on a Mac.

#### (historical) original P3 blocker — now resolved
- **`targetframework-override.props` import added** — the head now honors `UnoTargetFrameworkOverride` (required for CI per-platform builds; previously a full multi-TFM build ran every time).
- **Toolchain confirmed:** the windows TFM needs **`MSBuild.exe`** (VS), not `dotnet build` — under dotnet the WinUI `CompileXaml` + MRT PRI tasks fail (`MetadataLoadContext disposed`). With MSBuild.exe (`-r -p:UnoTargetFrameworkOverride=$(NetCurrentWinAppSDK) -p:Platform=x64`) the WinUI XAML compiler + WinAppSDK restore + `.Windows` ref gating all work and the entire sample set compiles down to one blocker.
- **BLOCKER — root-caused: an `InternalsVisibleTo` × `AssemblyName` collision.** The colliding helpers (`AsyncLock`, `EnumerableExtensions`, `DependencyObjectExtensions`) are all **`internal`**. `Uno.UI.Toolkit.Windows` **self-links** their source files (it has no `Uno.UI` to reference on WinUI), and the `Uno.Core.Extensions` packages carry their own copies. Both `Uno.UI.Toolkit` and `Uno.Foundation` grant `[InternalsVisibleTo("SamplesApp")]`. This head uses `AssemblyName=SamplesApp` on **every** TFM, so on windows it sees **both** internal copies → CS0433/CS0121. The old `SamplesApp.Windows` head's assembly is named **`SamplesApp.Windows`** (not in any IVT list), so it never saw the duplicates — it self-links the sources instead. That asymmetry is the entire bug.
  - **Removed** the head's redundant linked `DependencyObjectExtensions.cs` (Toolkit provides it via IVT) — one of the three collisions.
  - **Remaining decision (Martin's call — touches assembly identity / a shared project):** pick one — (a) give the windows TFM a **non-`SamplesApp` AssemblyName** (drops the Toolkit/Foundation IVT) and self-link the internal helpers the samples use, mirroring the old head; (b) on windows reference **only** Toolkit.Windows for the overlapping internals and link the small set of extras the samples need (`SelectOrDefault`, `ToUnixTimeMilliseconds`, `ToStringInvariant`, `Grouping`, …), dropping the `Uno.Core.Extensions.*` packages; or (c) stop `Uno.UI.Toolkit.Windows` granting IVT to `SamplesApp` (Toolkit-side fix; also un-breaks the old head). 
  - `$(NetCurrentWinAppSDK)` stays out of `<TargetFrameworks>` (windows dormant) until this is chosen; desktop+wasm remain green. `winapp` is installed and `dotnet run` is the intended runner once it compiles.

---

## Phase 0 — Local Uno.Sdk bootstrapping + layering spike

**Exit criterion:** A desktop-only (`net10.0-desktop`) skeleton head using `Sdk="Uno.Sdk.Private"` builds and launches from source locally.

### Task 0.1: Pack the local Uno.Sdk.Private into the local feed

**Files:**
- Create: `build/pack-local-uno-sdk.ps1`
- Verify against: `src/Uno.Sdk/Uno.Sdk.csproj`, `NuGet.config` (feed `src/PackageCache`)

**Interfaces:**
- Produces: `src/PackageCache/Uno.Sdk.Private.255.255.255-dev.nupkg`; sentinel version `255.255.255-dev` consumed by Task 0.2.

- [ ] **Step 1: Write the pack script**

`build/pack-local-uno-sdk.ps1`:
```powershell
#!/usr/bin/env pwsh
# Packs the in-repo Uno.Sdk as Uno.Sdk.Private at a fixed sentinel version into
# the local PackageCache feed, so the in-repo SamplesApp head can consume it via global.json.
$ErrorActionPreference = 'Stop'
$repoRoot = Resolve-Path "$PSScriptRoot/.."
$version  = '255.255.255-dev'
$feed     = Join-Path $repoRoot 'src/PackageCache'

dotnet pack "$repoRoot/src/Uno.Sdk/Uno.Sdk.csproj" `
  -c Release `
  -p:PackageVersion=$version `
  -p:PackageOutputPath=$feed

Write-Host "Packed Uno.Sdk.Private $version -> $feed"
```

- [ ] **Step 2: Run it**

Run: `pwsh build/pack-local-uno-sdk.ps1`
Expected: build succeeds; `src/PackageCache/Uno.Sdk.Private.255.255.255-dev.nupkg` exists.

- [ ] **Step 3: Verify the package carries the sentinel version**

Run (bash): `unzip -p src/PackageCache/Uno.Sdk.Private.255.255.255-dev.nupkg 'Sdk/Sdk.props' | grep -E 'UnoSdkVersion|UnoVersion'`
Expected: `<UnoVersion>255.255.255-dev</UnoVersion>` and `<UnoSdkVersion>255.255.255-dev-Private</UnoSdkVersion>` (the `DefaultUnoVersion` tokens were replaced). Source mode ignores these versions, but a valid pack is required for SDK resolution.

- [ ] **Step 4: Commit**

```bash
git add build/pack-local-uno-sdk.ps1
git commit -m "build: Add script to pack local Uno.Sdk.Private into PackageCache"
```

### Task 0.2: Pin the sentinel SDK version in global.json

**Files:**
- Modify: `global.json`

**Interfaces:**
- Consumes: sentinel `255.255.255-dev` from Task 0.1.
- Produces: `Sdk="Uno.Sdk.Private"` resolvable for projects that declare it.

- [ ] **Step 1: Add the msbuild-sdks entry**

Edit `global.json` so `msbuild-sdks` reads:
```json
  "msbuild-sdks": {
    "Microsoft.Build.NoTargets": "3.7.56",
    "Uno.Sdk.Private": "255.255.255-dev"
  }
```

- [ ] **Step 2: Verify SolutionTemplate isolation**

Run (bash): `find src/SolutionTemplate -name global.json -exec grep -l Uno.Sdk.Private {} \;`
Expected: each template has its own `global.json` with its own version — confirming the root pin does not affect them (nearest `global.json` wins).

- [ ] **Step 3: Commit**

```bash
git add global.json
git commit -m "build: Pin local Uno.Sdk.Private sentinel in root global.json"
```

### Task 0.3: Create the desktop-only skeleton head (the layering spike)

**Files:**
- Create: `src/SamplesApp/SamplesApp.Head/SamplesApp.Head.csproj`
- Create: `src/SamplesApp/SamplesApp.Head/Platforms/Desktop/Program.cs`

**Interfaces:**
- Produces: a buildable head named `SamplesApp.Head` (`AssemblyName=SamplesApp`) targeting `net10.0-desktop`.

- [ ] **Step 1: Write the minimal skeleton csproj**

`SamplesApp.Head.csproj` (spike form — desktop only, minimal refs to isolate the layering question):
```xml
<Project Sdk="Uno.Sdk.Private">
  <PropertyGroup>
    <TargetFrameworks>net10.0-desktop</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <UnoSingleProject>true</UnoSingleProject>
    <AssemblyName>SamplesApp</AssemblyName>

    <SamplesAppUseImplicitPackages Condition="'$(SamplesAppUseImplicitPackages)' == ''">false</SamplesAppUseImplicitPackages>
    <DisableImplicitUnoPackages Condition="'$(SamplesAppUseImplicitPackages)' != 'true'">true</DisableImplicitUnoPackages>
    <DisableImplicitUnoWinAppSdkPackages Condition="'$(SamplesAppUseImplicitPackages)' != 'true'">true</DisableImplicitUnoWinAppSdkPackages>
  </PropertyGroup>

  <!-- Spike: reference only Uno.UI.Skia + the Win32 runtime to prove the SDK/Directory.Build layering -->
  <ItemGroup Condition="'$(SamplesAppUseImplicitPackages)' != 'true'">
    <ProjectReference Include="..\..\Uno.UI\Uno.UI.Skia.csproj" />
    <ProjectReference Include="..\..\Uno.UI.Composition\Uno.UI.Composition.Skia.csproj" />
    <ProjectReference Include="..\..\Uno.UI.Dispatching\Uno.UI.Dispatching.Skia.csproj" />
    <ProjectReference Include="..\..\Uno.Foundation\Uno.Foundation.Skia.csproj" />
    <ProjectReference Include="..\..\Uno.UWP\Uno.Skia.csproj" />
    <ProjectReference Include="..\..\Uno.UI.Runtime.Skia.Win32\Uno.UI.Runtime.Skia.Win32.csproj" />
    <ProjectReference Include="..\..\Uno.UI.Runtime.Skia.Win32.Support\Uno.UI.Runtime.Skia.Win32.Support.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Write a trivial Program.cs that boots an empty app**

`Platforms/Desktop/Program.cs`:
```csharp
#nullable enable
using Uno.UI.Hosting;

namespace SamplesApp.Head;

internal static class Program
{
	[System.STAThread]
	public static void Main(string[] args)
	{
		var host = UnoPlatformHostBuilder.Create()
			.App(() => new global::Uno.UI.Hosting.UnoApplicationShim())
			.UseWin32()
			.Build();
		host.Run();
	}
}
```
> If `UnoApplicationShim` does not exist, substitute the smallest available `Microsoft.UI.Xaml.Application` subclass; this task's goal is *build + boot*, not running samples. The real `SamplesApp.App` is wired in P1 Task 1.2.

- [ ] **Step 3: Pack the SDK and attempt the build**

Run:
```bash
pwsh build/pack-local-uno-sdk.ps1
dotnet build src/SamplesApp/SamplesApp.Head/SamplesApp.Head.csproj -p:UnoTargetFrameworkOverride=net10.0-desktop -p:UnoFastDevBuild=true /bl:spike.binlog
```
Expected: SUCCESS. **If it fails**, this is the P0 layering spike doing its job — diagnose via `spike.binlog` (use the `dotnet-msbuild:binlog-failure-analysis` skill). Likely culprits/fixes:
- *Double platform-symbol / suffix application* (repo `Uno.CrossTargetting.targets` + SDK `Uno.CrossTargeting.targets`): set the repo-side opt-out the spike identifies, or scope the repo import with `Condition` on `'$(UsingUnoSdk)' != 'true'`.
- *Version pinning collisions*: confirm both disable flags are in effect (they are in default mode).
- *`targetframework-override` suffix detection*: confirm `net10.0-desktop` survives; add `UnoDisableTargetFrameworkPlatformOverride` if it strips the platform.

Record the mitigations as csproj comments and in `spec.md` §8.

- [ ] **Step 4: Run the booted app briefly**

Run: `dotnet run --project src/SamplesApp/SamplesApp.Head/SamplesApp.Head.csproj -p:UnoTargetFrameworkOverride=net10.0-desktop`
Expected: a window opens (empty app), no startup crash; close it.

- [ ] **Step 5: Commit**

```bash
git add src/SamplesApp/SamplesApp.Head/
git commit -m "feat: Add desktop-only Uno.Sdk SamplesApp head skeleton (P0 spike)"
```

### Task 0.4: Register the head for isolated output paths

**Files:**
- Modify: `src/Directory.Build.props` (the `_AdjustedOutputProjects` ItemGroup)

- [ ] **Step 1: Add the head**

In `src/Directory.Build.props`, add to `_AdjustedOutputProjects` (matching existing entries' form):
```xml
<_AdjustedOutputProjects Include="SamplesApp.Head" />
```

- [ ] **Step 2: Rebuild and confirm isolated output**

Run: `dotnet build src/SamplesApp/SamplesApp.Head/SamplesApp.Head.csproj -p:UnoTargetFrameworkOverride=net10.0-desktop -p:UnoFastDevBuild=true`
Expected: SUCCESS; output under `src/SamplesApp/SamplesApp.Head/bin/SamplesApp.Head/...` (no collision warnings).

- [ ] **Step 3: Commit**

```bash
git add src/Directory.Build.props
git commit -m "build: Register SamplesApp.Head in _AdjustedOutputProjects"
```

---

## Phase 1 — Desktop head complete + CI migration

**Exit criterion:** the new head runs the full SamplesApp on Skia desktop, desktop runtime tests pass through `SamplesApp.dll`, and the desktop CI stages (Windows/Linux/macOS) build/test the new head green.

### Task 1.1: Wire the full desktop framework references

**Files:**
- Modify: `src/SamplesApp/SamplesApp.Head/SamplesApp.Head.csproj`
- Read for parity: `SamplesApp.Skia.Generic.csproj` (lines 33–78), `SamplesApp.Skia.csproj` (lines 33–78)

- [ ] **Step 1: Add the complete desktop ProjectReference set**

Extend the gated `ItemGroup` from Task 0.3 with the remaining desktop runtimes + AddIns (mirror `SamplesApp.Skia.Generic.csproj` + base lib): `Uno.UI.Runtime.Skia.X11`, `.Linux.FrameBuffer`, `.MacOS`, AddIns `Uno.WinUI.Graphics3DGL`, `Uno.WinUI.Graphics2DSK.Crossruntime`, `Uno.UI.Lottie.Skia`, `Uno.UI.MSAL.Reference`, `Uno.UI.Svg.Skia`, `Uno.WinUI.SpellChecking`, `Uno.UI.FluentTheme.Skia`, `Uno.UI.RemoteControl.Skia`, `Uno.UI.RuntimeTests.Skia`, `Uno.UI.Toolkit.Skia`, `Uno.UI.Adapter.Microsoft.Extensions.Logging`, `Uno.UI.MediaPlayer.Skia.Win32`, `Uno.UI.MediaPlayer.Skia.X11`, `Uno.UI.WebView.Skia.X11`, `SourceGenerators/System.Xaml/Uno.Xaml`.

- [ ] **Step 2: Add desktop non-source PackageReferences**

Add (from `SamplesApp.Skia.Generic.csproj` + base lib): `SkiaSharp`, `SkiaSharp.NativeAssets.Linux`, `Microsoft.Web.WebView2` (alias `MsWebView2`), `VideoLAN.LibVLC.Windows`, `Uno.Resizetizer`, `Uno.Fonts.Fluent`, `Newtonsoft.Json`, `Microsoft.Identity.Client`, `MSTest.TestFramework`, `MSTest.Analyzers`, `System.CommandLine`, logging packages, and the Release-only `Microsoft.Graph`/`Microsoft.Kiota.Abstractions` refs. Keep version-pin discipline.

- [ ] **Step 3: Set `UnoUIMSBuildTasksPath` + `PreBuildUnoUITasks`** exactly as the source heads do.

- [ ] **Step 4: Build**

Run: `dotnet build src/SamplesApp/SamplesApp.Head/SamplesApp.Head.csproj -p:UnoTargetFrameworkOverride=net10.0-desktop -p:UnoFastDevBuild=true /bl:p1-build.binlog`
Expected: SUCCESS.

- [ ] **Step 5: Commit**

```bash
git add src/SamplesApp/SamplesApp.Head/SamplesApp.Head.csproj
git commit -m "feat: Wire full desktop framework references for the consolidated head"
```

### Task 1.2: Import shared content and wire the real App

**Files:**
- Modify: `src/SamplesApp/SamplesApp.Head/SamplesApp.Head.csproj`
- Read: `SamplesApp.Skia.csproj` lines 80–102

- [ ] **Step 1: Import the shared content globs**

```xml
<Import Project="..\..\SourceGenerators\sourcegenerators.local.props" />
<Import Project="..\SamplesApp.Shared\SamplesApp.Shared.props" />
<Import Project="..\SamplesApp.Samples\SamplesApp.Samples.props" />
<Import Project="..\SamplesApp.UnitTests.Shared\SamplesApp.UnitTests.Shared.props" />
<Import Project="..\Benchmarks.Shared\Benchmarks.Shared.projitems" Label="Shared" />
<Import Project="..\SamplesApp.UnitTests.Shared\SamplesApp.UnitTests.Shared.targets" />
```
Also: `<EmbeddedResource Include="..\SamplesApp.Windows\Package.appxmanifest" LogicalName="Package.appxmanifest" />` (until P3 moves it under `Platforms/Windows`).

- [ ] **Step 2: Port the real desktop entry point**

Port `SamplesApp.Skia.Generic/Program.cs` into `Platforms/Desktop/Program.cs`: `SamplesApp.App` instantiation, `ConfigureLogging()`, the X11/Win32/LinuxFrameBuffer/macOS host chain, the X11 WebView2 `ApiExtensibility.Register` in `AfterInit`, and the `AssemblyLoadContext.Resolving` fallback. Keep one consistent namespace.

- [ ] **Step 3: Check `Benchmarks.Shared.projitems` for stale absolute paths**

Run (bash): `grep -nE 'C:\\\\|/Users/|/home/' src/SamplesApp/Benchmarks.Shared/Benchmarks.Shared.projitems || echo "clean"`
Expected: `clean`. If not, fix to `$(MSBuildThisFileDirectory)`-relative in the same commit.

- [ ] **Step 4: Build**

Run: `dotnet build src/SamplesApp/SamplesApp.Head/SamplesApp.Head.csproj -p:UnoTargetFrameworkOverride=net10.0-desktop -p:UnoFastDevBuild=true`
Expected: SUCCESS (samples + unit-test + benchmark sources compile in).

- [ ] **Step 5: Commit**

```bash
git add src/SamplesApp/SamplesApp.Head/
git commit -m "feat: Compile shared samples/tests and wire SamplesApp.App into desktop head"
```

### Task 1.3: Migrate desktop assets and the macOS app-bundle target

**Files:**
- Create: `src/SamplesApp/SamplesApp.Head/Assets/`, `app.manifest`, `Resources/Info.plist`
- Read: `SamplesApp.Skia.Generic.csproj` lines 29–37, 90–93, 122–153

- [ ] **Step 1: Copy desktop assets + manifest and reference them**

Copy `Assets/`, `app.manifest`, `Resources/Info.plist` from `SamplesApp.Skia.Generic`. Add `ApplicationManifest`/`Manifest`, `UnoSplashScreen`/`UnoIcon`, the `_ValidatePublishedItems` target, and the macOS `_UnoBundleAppMacOS` target (gated on `IsOsPlatform('OSX')` as the original).

- [ ] **Step 2: Build + run**

Run: `dotnet run --project src/SamplesApp/SamplesApp.Head/SamplesApp.Head.csproj -p:UnoTargetFrameworkOverride=net10.0-desktop`
Expected: SamplesApp launches with icon/splash; the sample list is populated (confirms `SamplesListGenerator` ran against the head).

- [ ] **Step 3: Commit**

```bash
git add src/SamplesApp/SamplesApp.Head/
git commit -m "feat: Migrate desktop assets, manifest and macOS bundle target to the head"
```

### Task 1.4: Desktop runtime-test smoke

**Files:** none (validation)

- [ ] **Step 1: Run a runtime-test slice through the new head**

```bash
dotnet build src/SamplesApp/SamplesApp.Head/SamplesApp.Head.csproj -c Release -p:UnoTargetFrameworkOverride=net10.0-desktop
dotnet src/SamplesApp/SamplesApp.Head/bin/SamplesApp.Head/Release/net10.0-desktop/SamplesApp.dll --runtime-tests=$(pwd)/rt-results.xml
```
Expected: `rt-results.xml` produced; pass rate matches the current `SamplesApp.Skia.Generic` baseline. **Runtime validation** — record pass/fail counts.

- [ ] **Step 2: No commit.** If failures don't reproduce on the old head, stop and root-cause (debugging-discipline rule).

### Task 1.5: Add the head to the Skia solution filter

**Files:**
- Modify: `src/Uno.UI-Skia-only.slnf`

- [ ] **Step 1: Add the head's csproj path** (keep the existing heads — additive).
- [ ] **Step 2: Verify the filter builds**

Run: `dotnet build src/Uno.UI-Skia-only.slnf -p:UnoTargetFrameworkOverride=net10.0-desktop -p:UnoFastDevBuild=true`
Expected: SUCCESS including the new head.

- [ ] **Step 3: Commit**

```bash
git add src/Uno.UI-Skia-only.slnf
git commit -m "build: Add consolidated head to Uno.UI-Skia-only solution filter"
```

### Task 1.6: Flip the desktop CI build + test stages

**Files:**
- Modify: `build/ci/tests/.azure-devops-tests-skia-build.yml` (build job → new csproj, `-f net10.0-desktop`, `-p:UnoTargetFrameworkOverride=net10.0-desktop`; artifact `samplesapp-desktop-skia` from the head's `bin/.../net10.0-desktop`)
- Modify: `build/test-scripts/run-windows-skia-runtime-tests.ps1`, `linux-skia-runtime-tests.sh`, `macos-skia-runtime-tests.sh` (`SamplesApp.Skia.Generic.dll` → `SamplesApp.dll`)

- [ ] **Step 1: Update the build job** — repoint project path, set `-f net10.0-desktop` / `-p:UnoTargetFrameworkOverride=net10.0-desktop`, and the `bin/Release/net10.0-desktop` publish path. Add a pre-build step running `build/pack-local-uno-sdk.ps1` so CI agents have the local SDK before restore.
- [ ] **Step 2: Update the three desktop test scripts** to launch `SamplesApp.dll`.
- [ ] **Step 3: Push and verify CI** — `runtime_tests_skia_build`, Windows, Linux, macOS desktop stages green on the new head. Fix before P2.
- [ ] **Step 4: Commit**

```bash
git add build/ci/tests/.azure-devops-tests-skia-build.yml build/test-scripts/run-windows-skia-runtime-tests.ps1 build/test-scripts/linux-skia-runtime-tests.sh build/test-scripts/macos-skia-runtime-tests.sh
git commit -m "ci: Build and runtime-test desktop Skia via the consolidated head"
```

---

## Phase 2 — browserwasm

**Exit criterion:** the head adds `net10.0-browserwasm`, the WASM SamplesApp publishes and runs, and the WASM CI stage builds/tests the new head green.

- [ ] **Task 2.1:** Add `net10.0-browserwasm` to `<TargetFrameworks>`. Desktop TFM still builds.
- [ ] **Task 2.2:** Add browserwasm-gated `ProjectReference`s (`Runtime.Skia.WebAssembly.Browser`, `Uno.Foundation.Runtime.WebAssembly`, `Uno.Foundation.Wasm`, `Uno.Dispatching.Wasm`, `Uno.Wasm`) + `PackageReference`s (`SkiaSharp.NativeAssets.WebAssembly`, `Uno.Wasm.Bootstrap`, `Microsoft.Windows.Compatibility`) — mirror `SamplesApp.Skia.WebAssembly.Browser.csproj` lines 93–111. Add the WASM PropertyGroup (ICU/IDBFS/fingerprint workaround) + `Issue109289_Workaround` target, gated on browserwasm.
- [ ] **Task 2.3:** Create `Platforms/WebAssembly/Program.cs` (`UseWebAssembly().BuildAsync()`); migrate `WasmScripts/AppManifest.js`, `LinkerConfig.xml`, `aot.profile`, `LinkedFiles/WebContent`, gated on browserwasm. Import `Uno.UI.Runtime.Skia.WebAssembly.Browser/build/*.props|targets` conditionally.
- [ ] **Task 2.4:** Verify Uno.Sdk picks `Microsoft.NET.Sdk.WebAssembly` for browserwasm. `dotnet publish ...Head.csproj -c Release -f net10.0-browserwasm -p:UnoTargetFrameworkOverride=net10.0-browserwasm`. Serve `publish/wwwroot`; confirm boot in a browser (runtime validation).
- [ ] **Task 2.5:** Flip `build/ci/tests/.azure-devops-tests-webassembly-skia-build.yml` (path, `-f net10.0-browserwasm`, `wwwroot` path) and the WASM UITest config (`Constants.cs` URL/port). Verify the WASM CI stage green. Commit per task.

---

## Phase 3 — windows (WinAppSDK / WinUI)

**Exit criterion:** the head adds `$(NetCurrentWinAppSDK)`, builds a runnable MSIX WinUI app, and the WinAppSDK CI stage builds/tests the new head green. **Build the windows TFM with `MSBuild.exe` (VS tooling), not plain `dotnet build`.**

- [ ] **Task 3.1:** Add `$(NetCurrentWinAppSDK)` to `<TargetFrameworks>`. Add the windows-gated PropertyGroup mirroring `SamplesApp.Windows.csproj` lines 3–23: `OutputType=WinExe`, `UseWinUI=true`, `EnableMsixTooling=true`, `TargetPlatformMinVersion=10.0.17763.0`, `WINAPPSDK` define, `Platforms=x86;x64;ARM64`, `RuntimeIdentifiers=win-x86;win-x64;win-arm64`, MSIX signing props, `CsWinRTAotOptimizerEnabled=false`. Gate `OutputType=WinExe` on `GetTargetPlatformIdentifier == 'windows'`.
- [ ] **Task 3.2:** Add windows-gated `ProjectReference`s to the **`.Windows` variants** (mirror `SamplesApp.Windows.csproj` lines 88–107): `Uno.UI.Toolkit.Windows`, `Uno.UI.MSAL.Windows`, `Uno.WinUI.Graphics2DSK.Windows`, `Uno.WinUI.Graphics3DGL` (`AdditionalProperties="BuildGraphics3DGLForWindows=true"`), `Uno.UI.RuntimeTests.Windows`, and the linked `Uno.UI/Extensions/DependencyObjectExtensions.cs`. **No `Uno.UI` reference.** Add windows `PackageReference`s: `Microsoft.WindowsAppSDK` 2.1.3, `Microsoft.Windows.SDK.BuildTools`, `Microsoft.Windows.SDK.BuildTools.WinApp` (PrivateAssets=all), `CommunityToolkit.WinUI.Lottie`, plus the Uno.Core/logging/Graph/MSAL packages from the Windows head.
- [ ] **Task 3.3:** Create `Platforms/Windows/` with the WinUI `App`/`Main`, `Package.appxmanifest`, MSIX logo `Assets/`, `app.manifest`, and `Msix` ProjectCapability + `HasPackageAndPublishMenu` (mirror `SamplesApp.Windows.csproj` lines 25–33, 59–75). Move the `Package.appxmanifest` `EmbeddedResource` reference here (from Task 1.2). Ensure `WINAPPSDK`-gated app code in `SamplesApp.Shared/App.xaml.cs` compiles.
- [ ] **Task 3.4:** Build the MSIX: `MSBuild.exe src/SamplesApp/SamplesApp.Head/SamplesApp.Head.csproj /r /p:Configuration=Release /p:Platform=x64 /p:UnoTargetFrameworkOverride=$(NetCurrentWinAppSDK)`. Expected: MSIX produced; launch on Windows (runtime validation via `winui-runtime-tests` skill).
- [ ] **Task 3.5:** Flip `build/ci/tests/.azure-devops-tests-winappsdk.yml` (project path, TFM, MSIX/artifact paths, publish profile) and any windows UITest launch config. Verify the WinAppSDK CI stage green. Commit per task.

---

## Phase 4 — android

**Exit criterion:** the head adds `net10.0-android`, produces a runnable APK (`uno.platform.samplesapp.skia`), and the Android CI stages (default/coreclr/nativeaot) build/test green.

- [ ] **Task 4.1:** Add `net10.0-android`; set `UnoRuntimeIdentifier=Skia`, mobile/SingleProject props, `AndroidPackageFormat=apk`, multidex, AOT/interpreter workarounds — mirror `SamplesApp.Skia.netcoremobile.csproj` Android `<When>` (lines 220–264) + shared mobile props (lines 9–61).
- [ ] **Task 4.2:** Add android-gated `ProjectReference`s (`Runtime.Skia.Android`, `Uno.Foundation.netcoremobile`, `Uno.Dispatching.netcoremobile`, `Uno.netcoremobile`, AddIns `GooglePlay`/`Foldable`/`MSAL.netcoremobile`) + `PackageReference`s (`SkiaSharp.NativeAssets.Android`, `Uno.UniversalImageLoader`, AndroidX trio).
- [ ] **Task 4.3:** Create `Platforms/Android/` with `Application` + `MainActivity` + `AndroidManifest.xml` (minSdk 21 / targetSdk 36) + Android assets, ported from netcoremobile `Android/`. Reconcile `XAMARIN`/`RUNTIME_CORECLR` defines to android-only. Import `Uno.UI.Runtime.Skia.Android/build/*` gated on android.
- [ ] **Task 4.4:** `dotnet publish ...Head.csproj -c Release -f net10.0-android -p:RunAOTCompilation=false -p:UnoTargetFrameworkOverride=net10.0-android`. Install + launch on an emulator (runtime validation).
- [ ] **Task 4.5:** Flip `.azure-devops-tests-android-skia-build.yml`, `-coreclr`, `-nativeaot`, and `android-run-skia-runtime-tests.sh` (APK name + the `am start` activity name — the new `MainActivity` crc namespace changes; update it). Verify all three Android CI stages green. Commit per task.

---

## Phase 5 — ios + tvos

**Exit criterion:** the head adds `net10.0-ios` + `net10.0-tvos`, produces runnable `.app` bundles, and the iOS CI + TestFlight stages build green.

- [ ] **Task 5.1:** Add `net10.0-ios;net10.0-tvos`; port the iOS + tvOS `<When>` blocks from `SamplesApp.Skia.netcoremobile.csproj` (lines 78–124, 173–218). Set `UnoRuntimeIdentifier=Skia`. **No MacCatalyst.**
- [ ] **Task 5.2:** Add ios/tvos-gated `ProjectReference` to `Runtime.Skia.AppleUIKit` (+ netcoremobile foundation/uwp/dispatching/MSAL). Import `Uno.UI.Runtime.Skia.AppleUIKit/build/*` gated on `IsUIKit`.
- [ ] **Task 5.3:** Create `Platforms/iOS/Main.cs` + `Platforms/tvOS/Main.cs` (`UseAppleUIKit().Build().Run()`), `Info.plist`/entitlements, ported from netcoremobile `iOS/`+`tvOS/`.
- [ ] **Task 5.4:** `dotnet build ...Head.csproj -f net10.0-ios -c Release -p:UnoTargetFrameworkOverride=net10.0-ios` → `SamplesApp.app`. Launch in the iOS simulator (runtime validation).
- [ ] **Task 5.5:** Flip `.azure-devops-tests-ios-skia-build.yml`, `skia-ios-uitest-build.sh`, `.azure-devops-publish-ios-testflight.yml` (path, `_BuildFolder`, `-f net10.0-ios`). Verify iOS CI + TestFlight green. Commit per task.

---

## Phase 6 — Remove the old heads and dead infrastructure

**Exit criterion:** the solution, filters, and all CI reference only the consolidated head; the repo builds and all stages pass.

- [ ] **Task 6.1:** Delete `SamplesApp.Skia.Generic/`, `SamplesApp.Skia.WebAssembly.Browser/`, `SamplesApp.Skia.netcoremobile/`, `SamplesApp.Windows/`. Remove their entries from `Uno.UI-Skia-only.slnf` / any `.sln`.
- [ ] **Task 6.2:** Check `SamplesApp.Skia` (base lib) consumers (`grep -rl "SamplesApp.Skia.csproj" --include=*.csproj src/`). Delete if none; else leave + note why.
- [ ] **Task 6.3:** Remove dead CI scripts/stages referencing old head names; remove old heads from `_AdjustedOutputProjects` if present.
- [ ] **Task 6.4:** Full Skia solution build + a desktop runtime-test run + a WinAppSDK build to confirm no regressions. Commit.
- [ ] **Task 6.5 (optional):** Rename `SamplesApp.Head` → final name if desired; update `_AdjustedOutputProjects`, `.slnf`, CI paths in one commit.

---

## Phase P-implicit — Validate the implicit-packages switch

**Exit criterion:** `SamplesAppUseImplicitPackages=true` builds and runs the head (≥ desktop + wasm) via `UnoFeatures`→packages against local override packages.

- [ ] **Task PI.1:** Define the `<UnoFeatures>` gated on `'$(SamplesAppUseImplicitPackages)' == 'true'`. Start from the AddIns the samples use: `SkiaRenderer`, `MediaPlayerElement`, `WebView`, `Lottie`, `Svg`, `Hosting`, `Foldable`, plus any surfaced by missing-type errors. Cross-check `Uno.Sdk/UnoFeature.cs`.
- [ ] **Task PI.2:** Build local override packages (`UnoNugetOverrideVersion`), then build the head with `-p:SamplesAppUseImplicitPackages=true -p:UnoNugetOverrideVersion=<ver>` for `net10.0-desktop`. Iterate the feature list until it builds (ProjectReferences auto-gate off, both resolvers on).
- [ ] **Task PI.3:** Run the desktop app in implicit mode; confirm parity for a sample slice (runtime validation). Repeat for `net10.0-browserwasm`.
- [ ] **Task PI.4:** Document the switch usage (one-liners for both modes) in `spec.md` §4.4 and/or a short README in the head folder. Commit.

---

## Self-Review

**Spec coverage:**
- §3 project shape (incl. windows TFM + WinExe condition) → 0.3, 1.1–1.3, 2.x, 3.x, 4.x, 5.x. ✓
- §4 hybrid wiring + dual-flag switch → 0.3 (both flags), 1.1 (desktop), 3.2 (windows), PI.* (implicit). ✓
- §4.3 Windows specifics → P3 (3.1–3.5). ✓
- §5 local SDK bootstrapping → 0.1, 0.2, 1.6 Step 1. ✓
- §6 shared content / base lib → 1.2, 6.2. ✓
- §7 entry points/assets (Desktop/WebAssembly/Windows/Android/iOS/tvOS) → 1.2–1.3, 2.3, 3.3, 4.3, 5.3. ✓
- §8 build-system layering → 0.3 (spike), 0.4 (output paths). ✓
- §9 testing/UITest (incl. WinUI parity) → 1.4, 2.5, 3.4–3.5, 4.5, 5.5. ✓
- §10 CI migration (incl. winappsdk) → 1.6, 2.5, 3.5, 4.5, 5.5. ✓
- §11 phases → P0–P6 + P-implicit. ✓
- §13 out-of-scope (native renderers, MacCatalyst; WinUI head IN) → respected. ✓
- §14 open items → tvOS (P5), naming (6.5), UnoFeatures (PI.1), WinExe condition (P3). ✓

**Placeholder scan:** P0/P1 carry concrete code/commands. P2–P6/P-implicit are task-level per the "Plan structure note" — each names exact files, refs, and a runnable verification, with source-of-truth csprojs to mirror. The one soft spot (`UnoApplicationShim` in 0.3 Step 2) has an explicit fallback.

**Type/name consistency:** sentinel `255.255.255-dev` (0.1↔0.2); head name `SamplesApp.Head` + `AssemblyName=SamplesApp` consistent throughout (renamed from earlier `.Skia.Head`); switch `SamplesAppUseImplicitPackages` toggling **both** `DisableImplicitUnoPackages` and `DisableImplicitUnoWinAppSdkPackages` identical in spec §3/§4 and plan 0.3/1.1/PI.*; windows TFM via `$(NetCurrentWinAppSDK)` consistent; artifact `samplesapp-desktop-skia` preserved (1.6). ✓

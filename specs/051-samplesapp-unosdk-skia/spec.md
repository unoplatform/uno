## Status: Ready for spec

> Profile: Engineering refactor / build-system feature
> Date: 2026-06-22
> Tracking: Platform 7.0 milestone (internal)

## Requirement Brief

- **Problem:** The SamplesApp ships **separate heads per target** — `SamplesApp.Skia.Generic` (desktop: Win32/X11/FrameBuffer/macOS), `SamplesApp.Skia.WebAssembly.Browser` (WebAssembly), `SamplesApp.Skia.netcoremobile` (Android/iOS/tvOS/MacCatalyst), and `SamplesApp.Windows` (native WinUI / WinAppSDK reference). They duplicate entry points, manifests, and dependency wiring, none of them consume `Uno.Sdk`, and they bear little resemblance to the single-project app an external customer builds.
- **Outcome:** A **single multi-targeted head** built on `Uno.Sdk` (the in-repo `src/Uno.Sdk`) that replaces the three Skia heads **and** the native WinUI head — covering every Skia target except MacCatalyst, **plus the WinUI/WinAppSDK target** — while preserving the repo's from-source build.
- **Primary users:** Uno Platform maintainers/contributors who build, debug, and run the SamplesApp and its runtime tests across all targets.
- **Priority:** P1
- **Renderer:** **Skia + WinUI.** Skia for desktop/wasm/android/ios/tvos; native WinUI for the `windows10.0` target (the parity/reference platform). The other native Uno renderers (native Android Views, native iOS/UIKit, WASM DOM — `SamplesApp.Wasm`, `SamplesApp.netcoremobile`) remain out of scope and are being removed separately.

---

# Spec

## 1. Executive summary

Create one `Uno.Sdk`-based app head — provisionally `src/SamplesApp/SamplesApp.Head/SamplesApp.Head.csproj`, `AssemblyName=SamplesApp` — that multi-targets:

```
net10.0-desktop;net10.0-browserwasm;$(NetCurrentWinAppSDK);net10.0-android;net10.0-ios;net10.0-tvos
```
where `$(NetCurrentWinAppSDK)` = `net10.0-windows10.0.19041.0` (from `Directory.Build.props`).

It uses **`Sdk="Uno.Sdk.Private"`** (packed from the local `src/Uno.Sdk`) for the MSBuild plumbing that makes a single cross-platform project possible — per-TFM base-SDK selection, TFM→platform mapping (`desktop`/`browserwasm`/`windows10`/`android`/`ios`/`tvos`), file-suffix exclusion, single-project layout, Resizetizer/manifests, and WinUI/MSIX tooling for the windows TFM. The Uno framework itself is consumed **from in-repo source via `ProjectReference`** (the repo's established model), with **`DisableImplicitUnoPackages=true`** (Skia/core) and **`DisableImplicitUnoWinAppSdkPackages=true`** (WinAppSDK) turning off the SDK's NuGet feature-resolver.

A build-time **switch** (`SamplesAppUseImplicitPackages`, default `false`) flips the head into "implicit-packages mode" — it un-sets **both** disable flags, defines real `<UnoFeatures>`, and gates the source `ProjectReference`s off — so the team can, on demand, exercise the customer-facing `UnoFeatures`→package resolution path.

The new head lands **alongside** the four current heads. CI build/test stages are migrated target-by-target and verified green before the old heads, their stages, and dead scripts are deleted.

### Decisions (locked)

| # | Decision | Choice |
|---|----------|--------|
| D1 | Framework consumption | **Hybrid** — `Uno.Sdk` plumbing + source `ProjectReference`s (default), toggleable to implicit-packages mode |
| D2 | Target scope | One project: **all Skia minus MacCatalyst** (desktop, browserwasm, android, ios, tvos) **+ WinUI/WinAppSDK** (windows10.0) |
| D3 | Target frameworks | **net10.0 only** |
| D4 | Rollout | **Land alongside → migrate CI per target → delete old heads** |
| D5 | Implicit-packages switch | `SamplesAppUseImplicitPackages` build property, default `false`; toggles both `DisableImplicitUnoPackages` and `DisableImplicitUnoWinAppSdkPackages` |

## 2. Background — current state (verified)

- **`SamplesApp.Skia`** (`SamplesApp.Skia.csproj`) is a **shared base library**, multi-TFM `net9.0;net10.0`, defining `__SKIA__;HAS_UNO;UNO_REFERENCE_API`, referencing the **`.Reference`** variants, and importing the shared content globs: `SamplesApp.Shared.props`, `SamplesApp.Samples.props`, `SamplesApp.UnitTests.Shared.props`, `Benchmarks.Shared.projitems`. The three Skia heads `ProjectReference` it.
- **The three Skia heads** each use `Microsoft.NET.Sdk` (or `Microsoft.NET.Sdk.WebAssembly` for WASM), set `IsUnoHead=true`, and wire **`ProjectReference`s to in-repo Uno source** — never NuGet.
- **`SamplesApp.Windows`** (`SamplesApp.Windows.csproj`) is the **native WinUI / WinAppSDK** head: `Microsoft.NET.Sdk`, `OutputType=WinExe`, `TargetFramework=$(NetCurrentWinAppSDK)`, `UseWinUI=true`, MSIX tooling (`EnableMsixTooling`, `Package.appxmanifest`, MSIX logo assets), `WINAPPSDK` define. It uses the **real WinUI** (`Microsoft.WindowsAppSDK` 2.1.3) for rendering — **not** `Uno.UI` — and `ProjectReference`s only the **`.Windows` variants** of Uno helpers: `Uno.UI.Toolkit.Windows`, `Uno.UI.MSAL.Windows`, `Uno.WinUI.Graphics2DSK.Windows`, `Uno.WinUI.Graphics3DGL` (`BuildGraphics3DGLForWindows=true`), `Uno.UI.RuntimeTests.Windows`. It imports the same shared content globs.
- **`Uno.Sdk`** (`src/Uno.Sdk`) packs as MSBuild SDK `Uno.Sdk.Private`. Verified mechanics:
  - `Sdk.props` selects the base SDK per TFM: `Microsoft.NET.Sdk.WebAssembly` for `browserwasm` (net9+), else `Microsoft.NET.Sdk`. The `windows10` TFM sets `IsWinAppSdk=true` and Uno.Sdk wires WinUI/WinAppSDK/MSIX. **This per-TFM base-SDK + platform selection is what allows one project to span wasm + desktop + windows + mobile.**
  - `Uno.Features.targets` always implies `skiarenderer` and strips `nativerenderer`, so every non-Windows TFM (incl. android/ios/tvos) renders via Skia; the `windows10` TFM uses native WinUI.
  - `DisableImplicitUnoPackages` gates the Skia/core `UnoFeatures`→package resolver; `DisableImplicitUnoWinAppSdkPackages` gates the WinAppSDK package resolution. Both default `false` in `Sdk.props`.
- **No in-repo project consumes `src/Uno.Sdk` today.** The `src/SolutionTemplate/*` apps that use `Sdk="Uno.Sdk.Private"` each pin a **published** version in their own deeper `global.json` and deliberately detach from the repo's `src/Directory.Build.props`. Consuming the **local** SDK against a **source-built** framework is therefore net-new infrastructure (see §5).

## 3. Target project shape

```xml
<Project Sdk="Uno.Sdk.Private">

  <PropertyGroup>
    <TargetFrameworks>net10.0-desktop;net10.0-browserwasm;$(NetCurrentWinAppSDK);net10.0-android;net10.0-ios;net10.0-tvos</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <OutputType Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'windows'">WinExe</OutputType>
    <UnoSingleProject>true</UnoSingleProject>

    <AssemblyName>SamplesApp</AssemblyName>
    <ApplicationId>uno.platform.samplesapp.skia</ApplicationId>

    <!-- D5: strategy switch. Default = source ProjectReference mode (both resolvers off). -->
    <SamplesAppUseImplicitPackages Condition="'$(SamplesAppUseImplicitPackages)' == ''">false</SamplesAppUseImplicitPackages>
    <DisableImplicitUnoPackages Condition="'$(SamplesAppUseImplicitPackages)' != 'true'">true</DisableImplicitUnoPackages>
    <DisableImplicitUnoWinAppSdkPackages Condition="'$(SamplesAppUseImplicitPackages)' != 'true'">true</DisableImplicitUnoWinAppSdkPackages>

    <UseSystemResourceKeys>false</UseSystemResourceKeys>
  </PropertyGroup>

  <!-- Shared content: import the existing globs directly (see §6) -->
  <!-- Source-mode framework references, per-TFM (see §4) -->
  <!-- Implicit-packages-mode UnoFeatures + version overrides (see §4.4) -->

</Project>
```

- **MacCatalyst dropped** (D2 — legacy/unsupported).
- **WinUI/WinAppSDK target included** via `$(NetCurrentWinAppSDK)` (= `net10.0-windows10.0.19041.0`), matching the existing `SamplesApp.Windows` head's TFM. This is the **only** native-rendering TFM; all others are Skia.
- **tvOS retained** to honor "all Skia targets" — not obviously exercised by current Skia CI; a cheap drop later (open item, non-blocking).
- **`OutputType`** is `WinExe` for windows, `Exe` elsewhere (Uno.Sdk may already set this per platform; the explicit condition is a safety net, validated in the Windows phase).
- **Naming:** provisional csproj `SamplesApp.Head` (dropped `.Skia` since the head now spans Skia **and** WinUI); `AssemblyName=SamplesApp`. Final name decided in cleanup.

## 4. Framework wiring (the hybrid core)

### 4.1 Source mode (default — `SamplesAppUseImplicitPackages=false`)

Both disable flags `true`; framework comes from **per-TFM conditional `ProjectReference`s** wrapped in `Condition="'$(SamplesAppUseImplicitPackages)' != 'true'"`, replicating today's matrix:

| Scope | Projects |
|-------|----------|
| **Skia TFMs (all but windows)** | `Uno.UI.Skia`, `Uno.UI.Composition.Skia`; platform variants of `Uno.Foundation` / `Uno.UWP` / `Uno.UI.Dispatching`: `.Skia` (desktop), `.Wasm` (browserwasm), `.netcoremobile` (android/ios/tvos) |
| **desktop** | `Runtime.Skia.Win32`(+`.Win32.Support`), `.X11`, `.Linux.FrameBuffer`, `.MacOS`; AddIns: `MediaPlayer.Skia.Win32`/`.X11`, `WebView.Skia.X11`, `Graphics3DGL`, `Graphics2DSK`, `Lottie.Skia`, `Svg.Skia`, `SpellChecking`, `MSAL.Reference`, `FluentTheme.Skia`, `RemoteControl.Skia`, `RuntimeTests.Skia`, `Toolkit.Skia`, logging adapter |
| **browserwasm** | `Runtime.Skia.WebAssembly.Browser`, `Uno.Foundation.Runtime.WebAssembly` |
| **android** | `Runtime.Skia.Android`; AddIns: `GooglePlay`, `Foldable`, `MSAL.netcoremobile` |
| **ios / tvos** | `Runtime.Skia.AppleUIKit` |
| **windows (WinAppSDK)** | **No `Uno.UI`** (native WinUI renders). `.Windows` variants only: `Uno.UI.Toolkit.Windows`, `Uno.UI.MSAL.Windows`, `Uno.WinUI.Graphics2DSK.Windows`, `Uno.WinUI.Graphics3DGL` (`BuildGraphics3DGLForWindows=true`), `Uno.UI.RuntimeTests.Windows` |

Conditions use `$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)'))` (`desktop`/`browserwasm`/`windows`/`android`/`ios`/`tvos`) plus an `IsUIKit` flag for ios+tvos, mirroring the current heads.

### 4.2 `UnoRuntimeIdentifier=Skia` propagation

- desktop / browserwasm: set automatically by Uno.Sdk's `-desktop`/`-browserwasm` targets.
- android / ios / tvos: the `buildTransitive` prop fires only for NuGet refs, not `ProjectReference`s → the head **sets `UnoRuntimeIdentifier=Skia` explicitly** for the mobile TFMs.
- windows: no Skia runtime identifier — native WinUI (`UseWinUI=true`, `IsWinAppSdk=true`).

### 4.3 Windows (WinAppSDK) specifics

For `$(NetCurrentWinAppSDK)`, mirror `SamplesApp.Windows.csproj`: `OutputType=WinExe`, `UseWinUI=true`, `EnableMsixTooling=true`, `TargetPlatformMinVersion=10.0.17763.0`, `WINAPPSDK` define, `Platforms=x86;x64;ARM64`, `RuntimeIdentifiers=win-x86;win-x64;win-arm64`, MSIX signing properties, `CsWinRTAotOptimizerEnabled=false`. `PackageReference`s: `Microsoft.WindowsAppSDK` 2.1.3, `Microsoft.Windows.SDK.BuildTools`, `Microsoft.Windows.SDK.BuildTools.WinApp`, `CommunityToolkit.WinUI.Lottie`, plus the Uno.Core/logging/Graph/MSAL packages the Windows head carries. The MSIX logo `Assets/` and `Package.appxmanifest` move under `Platforms/Windows`. `Msix` project capability + `HasPackageAndPublishMenu` preserved.

### 4.4 Implicit-packages mode (`SamplesAppUseImplicitPackages=true`)

- Both disable flags left unset → default `false` → Uno.Sdk resolves `<UnoFeatures>` to `PackageReference`s (Skia + WinAppSDK).
- A representative `<UnoFeatures>` list is defined under `Condition="'$(SamplesAppUseImplicitPackages)' == 'true'"` (≥ `SkiaRenderer`, plus features matching the AddIns the samples exercise — finalized in P-implicit).
- The §4.1 `ProjectReference` ItemGroups are gated **off**.
- To test against **local** source rather than published packages, combine with the existing `UnoNugetOverrideVersion` mechanism. Implicit mode is **not** routine CI; it is a manually-invoked validation path.

### 4.5 Non-source dependencies (both modes)

Dependencies that are NuGet even today stay `PackageReference`, conditioned per TFM: `SkiaSharp` + platform `SkiaSharp.NativeAssets.*`, `Uno.Wasm.Bootstrap` (browserwasm), AndroidX (android), `Microsoft.WindowsAppSDK`/SDK BuildTools (windows), `Uno.Fonts.Fluent`, `Uno.Resizetizer`, MSAL, MSTest, etc. Versions pinned via the repo's `PackageReference … Update` convention (CPM stays disabled).

## 5. Local `Uno.Sdk` bootstrapping

1. **Pack** `src/Uno.Sdk` → `Uno.Sdk.Private` at a **fixed sentinel version** (e.g. `255.255.255-dev`) into the local feed `src/PackageCache`.
2. **Pin** that version in the **root `global.json`** `msbuild-sdks`. Isolated: only the new head declares `Sdk="Uno.Sdk.Private"`; `SolutionTemplate` apps keep their own deeper `global.json`.
3. A **pre-restore/pre-build step** packs the SDK before the head builds — CI + a documented local one-liner — analogous to `PreBuildUnoUITasks`.

Because source mode sets both disable flags, the SDK's `UnoVersion`/`UnoSdkVersion` (used only for implicit package resolution) are irrelevant to the default build — making the local-SDK consumption robust against version drift. (Alternative considered & rejected: `MSBuildSDKsPath` env var — less CI-robust.)

## 6. Shared content & the base library

- The new head imports the **existing content globs directly** — `SamplesApp.Shared.props`, `SamplesApp.Samples.props`, `SamplesApp.UnitTests.Shared.props`, `Benchmarks.Shared.projitems` — as `SamplesApp.Skia` and `SamplesApp.Windows` do today, so all samples/tests/benchmarks compile into it per-TFM. This sidesteps the base-lib's `net9.0;net10.0`-only TFMs (unreferenceable from mobile/windows TFMs).
- `SamplesApp.Skia` base lib **untouched during the alongside phase**; removed in cleanup if unreferenced.
- `Benchmarks.Shared.projitems` checked for stale absolute paths during P0.

## 7. Entry points, platform code & assets

Adopt the `Uno.Sdk` single-project `Platforms/` layout, consolidating today's per-head entry points:

| Folder (TFM) | Entry point | Notes |
|--------------|-------------|-------|
| `Platforms/Desktop` (`-desktop`) | `Program.cs` | `UseWin32().UseX11().UseLinuxFrameBuffer().UseMacOS()`; X11 WebView2 `ApiExtensibility.Register`; ALC resolving hook |
| `Platforms/WebAssembly` (`-browserwasm`) | `Program.cs` | `UseWebAssembly()` (async); `WebContent`, `LinkerConfig.xml`, `AppManifest.js`, `aot.profile` |
| `Platforms/Windows` (`-windows10.0`) | WinUI `App` + generated `Main` | `Package.appxmanifest`, MSIX logo assets, `app.manifest`; native WinUI startup (no Uno host builder) |
| `Platforms/Android` (`-android`) | `Application` + `MainActivity` | `AndroidManifest.xml`, Android assets |
| `Platforms/iOS` / `Platforms/tvOS` | `Main.cs` | `UseAppleUIKit()`; `Info.plist`/entitlements |

- App-level conditional compilation (`#if __SKIA__ / __WASM__ / __ANDROID__ / __APPLE_UIKIT__ / WINAPPSDK`) is preserved — each TFM compiles the shared sources with the correct symbols, driven by Uno.Sdk + repo crosstargeting. Legacy `XAMARIN`/`RUNTIME_CORECLR` defines reconciled to mobile-only.
- File-suffix exclusion is owned by the SDK/repo crosstargeting — the head must not hand-roll `Compile Remove`.

## 8. Build-system integration

- **Register** the head in `_AdjustedOutputProjects` (`src/Directory.Build.props`) for isolated multi-TFM output.
- **`targetframework-override` interaction:** the head must respond to `UnoTargetFrameworkOverride` so a single TFM can be built in isolation (CI passes e.g. `net10.0-android`, `net10.0-windows10.0.19041.0`).
- **Top risk — `Directory.Build` × `Uno.Sdk` layering (gating spike P0):** the repo's src-wide `Directory.Build.props/.targets` + `Uno.CrossTargetting.targets` assume `Microsoft.NET.Sdk`. Layering `Uno.Sdk` can double-apply platform symbols, suffix exclusion, version pinning, TFM override. P0 proves a desktop-only slice before widening. Mitigations: opting the head out of specific repo targets, or relying on Uno.Sdk's `CustomAfterDirectoryBuildProps` ordering.
- `PreBuildUnoUITasks` / `Uno.UI.Tasks` shadow-build preserved.

## 9. Testing & UITest coupling

- `SnapShotTestGenerator` keys on assembly `SamplesApp.UITests` and the `SamplesApp.Samples/` path marker; keeping samples in place and not touching the UITests project means **both generators keep working**.
- Desktop runtime-test invocation: `SamplesApp.Skia.Generic.dll` → `SamplesApp.dll` when each desktop stage is flipped.
- Windows (WinUI) runtime tests run via the WinAppSDK pipeline (MSIX install + run) — the `winui-runtime-tests` parity path; the windows TFM produces the MSIX the stage installs.
- `IsUiAutomationMappingEnabled` and `ApplicationId` set per-TFM; Android CI variants (`coreclr`/`nativeaot`) keep their `-p:` overrides.

## 10. CI migration (alongside → flip → delete)

The four heads are consumed by ~16+ Azure DevOps YAML files and shell/PowerShell scripts hardcoding project paths, TFMs, assembly/APK/`.app`/MSIX names, artifact names, and the app id. Migration is per-target, each in its own change: point the build at the new csproj, set the platform TFM, update expected output/artifact names and collection paths, update the runtime-/UI-test launch script, update `Uno.UI-Skia-only.slnf` (and the windows pipeline `.azure-devops-tests-winappsdk.yml`). Verify the stage green before the next.

## 11. Rollout phases (→ implementation plan)

| Phase | Deliverable | Exit criterion |
|-------|-------------|----------------|
| **P0** | Local-`Uno.Sdk` pack+pin bootstrapping; desktop-only slice proving the layering | Desktop head builds & runs from source locally |
| **P1** | Desktop head complete; CI desktop stages flipped | Desktop runtime tests green |
| **P2** | Add `browserwasm`; flip WASM CI | WASM runtime/UI tests green |
| **P3** | Add `windows10.0` (WinAppSDK/WinUI); flip WinAppSDK CI | WinUI runtime tests + MSIX build green |
| **P4** | Add `android`; flip Android CI (default/coreclr/nativeaot) | Android runtime tests green |
| **P5** | Add `ios` + `tvos`; flip iOS CI (+TestFlight) | iOS runtime/UI tests green |
| **P6** | Delete the four old heads, base lib if unused, dead scripts/`.slnf`/pipeline entries | Solution + CI reference only the new head |
| **P-implicit** | Finalize `<UnoFeatures>`; validate `SamplesAppUseImplicitPackages=true` (vs `UnoNugetOverrideVersion` local packages) | Head builds & runs in implicit mode on ≥ desktop + wasm |

## 12. Risks

1. **`Directory.Build` × `Uno.Sdk` layering** — highest; P0 hard gate.
2. **Windows/WinAppSDK in a multi-TFM Uno.Sdk project** — MSIX tooling, signing, `WinExe`, and `.Windows` variant refs must coexist with the Skia TFMs; validated in P3. Building the windows TFM requires `MSBuild.exe`/VS tooling, not plain `dotnet build` in some paths.
3. **Local SDK version drift** — mitigated by fixed sentinel + both resolvers off in source mode.
4. **Multi-TFM build cost** — one build spans all platforms; mitigated by `.slnf` + `UnoTargetFrameworkOverride`.
5. **Per-TFM `ProjectReference` leakage** — wrong-platform refs; mitigated by strict `Condition`s + per-stage CI.
6. **Implicit-mode feature completeness** — `<UnoFeatures>` must cover everything the samples use; validated in P-implicit, off critical path.

## 13. Out of scope

- Removal of the **native** Uno-renderer heads (`SamplesApp.Wasm` WASM-DOM, `SamplesApp.netcoremobile` native) — handled separately. (The native WinUI head `SamplesApp.Windows` is **in scope** — subsumed by the windows TFM.)
- MacCatalyst support.
- Changes to the `UnoIslands*` sample apps.
- Any change to how external customers consume the **published** `Uno.Sdk`.

## 14. Open items (non-blocking)

- Keep tvOS, or drop it if it adds no CI value?
- Final project/folder name (rename in P6 cleanup vs keep `SamplesApp.Head`).
- Exact `<UnoFeatures>` list for implicit mode (finalized in P-implicit).
- Whether the windows TFM's `OutputType=WinExe` needs the explicit condition or Uno.Sdk sets it (confirmed in P3).

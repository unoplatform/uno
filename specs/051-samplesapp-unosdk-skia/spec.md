## Status: Ready for spec

> Profile: Engineering refactor / build-system feature
> Date: 2026-06-22
> Tracking: Platform 7.0 milestone (internal)

## Requirement Brief

- **Problem:** The SamplesApp ships **three separate Skia heads** — `SamplesApp.Skia.Generic` (desktop: Win32/X11/FrameBuffer/macOS), `SamplesApp.Skia.WebAssembly.Browser` (WebAssembly), and `SamplesApp.Skia.netcoremobile` (Android/iOS/tvOS/MacCatalyst). They duplicate entry points, manifests, and dependency wiring, none of them consume `Uno.Sdk`, and they bear little resemblance to the single-project app an external customer builds.
- **Outcome:** A **single multi-targeted Skia head** built on `Uno.Sdk` (the in-repo `src/Uno.Sdk`), covering every Skia target except MacCatalyst, that replaces the three current Skia heads and dogfoods the customer single-project experience while preserving the repo's from-source build.
- **Primary users:** Uno Platform maintainers/contributors who build, debug, and run the SamplesApp and its runtime tests across Skia targets.
- **Priority:** P1
- **Renderer:** Skia only. Native heads (`SamplesApp.Wasm`, `SamplesApp.netcoremobile`, native `SamplesApp.Windows`) are out of scope — they are being removed separately; this work neither depends on nor blocks that removal.

---

# Spec

## 1. Executive summary

Create one `Uno.Sdk`-based app head — provisionally `src/SamplesApp/SamplesApp.Skia.Head/SamplesApp.Skia.Head.csproj`, `AssemblyName=SamplesApp` — that multi-targets:

```
net10.0-desktop;net10.0-browserwasm;net10.0-android;net10.0-ios;net10.0-tvos
```

It uses **`Sdk="Uno.Sdk.Private"`** (packed from the local `src/Uno.Sdk`) for the MSBuild plumbing that makes a single cross-platform project possible — per-TFM base-SDK selection, TFM→platform mapping, file-suffix exclusion, single-project layout, Resizetizer and manifests. The Uno framework itself is consumed **from in-repo source via `ProjectReference`** (the repo's established model), with `DisableImplicitUnoPackages=true` turning off the SDK's NuGet feature-resolver.

A build-time **switch** (`SamplesAppUseImplicitPackages`, default `false`) flips the head into "implicit-packages mode" (`DisableImplicitUnoPackages=false`, real `<UnoFeatures>`, source `ProjectReference`s gated off) so the team can, on demand, exercise the customer-facing `UnoFeatures`→package resolution path.

The new head lands **alongside** the three current heads. CI build/test stages are migrated platform-by-platform and verified green before the old heads, their stages, and dead scripts are deleted.

### Decisions (locked)

| # | Decision | Choice |
|---|----------|--------|
| D1 | Framework consumption | **Hybrid** — `Uno.Sdk` plumbing + source `ProjectReference`s (default), toggleable to implicit-packages mode |
| D2 | Platform scope | One project, **all Skia minus MacCatalyst**: desktop, browserwasm, android, ios, tvos |
| D3 | Target frameworks | **net10.0 only** |
| D4 | Rollout | **Land alongside → migrate CI per stage → delete old heads** |
| D5 | Implicit-packages switch | `SamplesAppUseImplicitPackages` build property, default `false` |

## 2. Background — current state (verified)

- **`SamplesApp.Skia`** (`SamplesApp.Skia.csproj`) is a **shared base library**, multi-TFM `net9.0;net10.0` (no platform suffix), defining `__SKIA__;HAS_UNO;UNO_REFERENCE_API`, referencing the **`.Reference`** variants of `Uno.Foundation`/`Uno.UWP`/`Uno.UI.MSAL`, and importing the shared content globs: `SamplesApp.Shared.props`, `SamplesApp.Samples.props`, `SamplesApp.UnitTests.Shared.props`, `Benchmarks.Shared.projitems`. The three heads `ProjectReference` it and override the `.Reference` variants with platform variants.
- **The three heads** each use `Microsoft.NET.Sdk` (or `Microsoft.NET.Sdk.WebAssembly` for WASM), set `IsUnoHead=true`, and wire **`ProjectReference`s to in-repo Uno source** — never NuGet — for the framework.
- **`Uno.Sdk`** (`src/Uno.Sdk`) packs as MSBuild SDK `Uno.Sdk.Private`. Verified mechanics:
  - `Sdk.props` selects the base SDK per TFM: `Microsoft.NET.Sdk.WebAssembly` for `browserwasm` (net9+), else `Microsoft.NET.Sdk`. **This is what allows one project to span wasm + desktop + mobile** (today's wasm head needs a different SDK, forcing a separate project).
  - `Uno.Features.targets` always implies `skiarenderer` and strips `nativerenderer`, so **every** TFM (including android/ios/tvos) renders via Skia.
  - `DisableImplicitUnoPackages=true` gates off the `UnoFeatures`→PackageReference resolver (`Uno.Common.targets` only imports `Uno.Implicit.Packages.targets` when it is not `true`).
- **No in-repo project consumes `src/Uno.Sdk` today.** The `src/SolutionTemplate/*` apps that use `Sdk="Uno.Sdk.Private"` each pin a **published** version in their own deeper `global.json` and deliberately detach from the repo's `src/Directory.Build.props`. Consuming the **local** SDK against a **source-built** framework is therefore net-new infrastructure (see §5).

## 3. Target project shape

```xml
<Project Sdk="Uno.Sdk.Private">

  <PropertyGroup>
    <TargetFrameworks>net10.0-desktop;net10.0-browserwasm;net10.0-android;net10.0-ios;net10.0-tvos</TargetFrameworks>
    <OutputType>Exe</OutputType>
    <UnoSingleProject>true</UnoSingleProject>

    <AssemblyName>SamplesApp</AssemblyName>
    <ApplicationId>uno.platform.samplesapp.skia</ApplicationId>

    <!-- D5: strategy switch. Default = source ProjectReference mode. -->
    <SamplesAppUseImplicitPackages Condition="'$(SamplesAppUseImplicitPackages)' == ''">false</SamplesAppUseImplicitPackages>
    <DisableImplicitUnoPackages Condition="'$(SamplesAppUseImplicitPackages)' != 'true'">true</DisableImplicitUnoPackages>

    <!-- Keep full exception strings in release, as today's heads do -->
    <UseSystemResourceKeys>false</UseSystemResourceKeys>
  </PropertyGroup>

  <!-- Shared content: import the existing globs directly (see §6) -->
  <!-- Source-mode framework references, per-TFM (see §4) -->
  <!-- Implicit-packages-mode UnoFeatures + version overrides (see §4.3) -->

</Project>
```

- **MacCatalyst dropped** (D2 — legacy/unsupported).
- **tvOS retained** to honor "all Skia targets." Note: tvOS is not obviously exercised by the current Skia CI stages — it is a cheap TFM to drop if we decide it adds no value. Flagged as an open item, not a blocker.
- **Project/assembly naming:** csproj name `SamplesApp.Skia.Head` is provisional and chosen to avoid colliding with the existing `SamplesApp.Skia` base lib during the alongside phase; `AssemblyName=SamplesApp` matches the current mobile head and becomes the single identity. Renaming the folder/csproj to a final name is a cleanup-phase option.

## 4. Framework wiring (the hybrid core)

### 4.1 Source mode (default — `SamplesAppUseImplicitPackages=false`)

`DisableImplicitUnoPackages=true`; framework comes from **per-TFM conditional `ProjectReference`s** wrapped in `Condition="'$(SamplesAppUseImplicitPackages)' != 'true'"`, replicating today's matrix:

| Scope | Projects |
|-------|----------|
| **All TFMs** | `Uno.UI.Skia`, `Uno.UI.Composition.Skia`; platform variants of `Uno.Foundation` / `Uno.UWP` / `Uno.UI.Dispatching`: `.Skia` (desktop), `.Wasm` (browserwasm), `.netcoremobile` (android/ios/tvos) |
| **desktop** | `Uno.UI.Runtime.Skia.Win32` (+`.Win32.Support`), `.X11`, `.Linux.FrameBuffer`, `.MacOS`; AddIns: `MediaPlayer.Skia.Win32`/`.X11`, `WebView.Skia.X11`, `Graphics3DGL`, `Graphics2DSK`, `Lottie.Skia`, `Svg.Skia`, `SpellChecking`, `MSAL.Reference`, logging adapter |
| **browserwasm** | `Uno.UI.Runtime.Skia.WebAssembly.Browser`, `Uno.Foundation.Runtime.WebAssembly` |
| **android** | `Uno.UI.Runtime.Skia.Android`; AddIns: `GooglePlay`, `Foldable`, `MSAL.netcoremobile` |
| **ios / tvos** | `Uno.UI.Runtime.Skia.AppleUIKit` |

Conditions use `$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)'))` (`desktop`/`browserwasm`/`android`/`ios`/`tvos`) and an `IsUIKit` flag for ios+tvos, mirroring the current `netcoremobile` head.

### 4.2 `UnoRuntimeIdentifier=Skia` propagation

- desktop / browserwasm: set automatically by `Uno.Sdk`'s `-desktop`/`-browserwasm` targets.
- android / ios / tvos: the `buildTransitive` prop that sets it only fires for **NuGet** references, not `ProjectReference`s. The head therefore **sets `UnoRuntimeIdentifier=Skia` explicitly** for the mobile TFMs.

### 4.3 Implicit-packages mode (`SamplesAppUseImplicitPackages=true`)

- `DisableImplicitUnoPackages` left unset → defaults `false` → `Uno.Sdk` resolves `<UnoFeatures>` to `PackageReference`s.
- A representative `<UnoFeatures>` list is defined under `Condition="'$(SamplesAppUseImplicitPackages)' == 'true'"` (at minimum: `SkiaRenderer`, plus the features matching the AddIns the samples exercise — e.g. `MediaPlayerElement`, `WebView`, `Lottie`, `Svg`, `Hosting`, `Foldable`, `Maps` where applicable). The exact list is finalized during P-implicit.
- The §4.1 `ProjectReference` ItemGroups are gated **off** (their `!= 'true'` condition).
- To test implicit mode against **local** source rather than published packages, combine with the existing `UnoNugetOverrideVersion` mechanism (which copies locally-built DLLs into the NuGet cache). Implicit mode is **not** part of routine CI; it is a manually-invoked validation path.

### 4.4 Non-source dependencies (both modes)

Dependencies that are NuGet packages even today stay `PackageReference`, conditioned per TFM: `SkiaSharp` + platform `SkiaSharp.NativeAssets.*`, `Uno.Wasm.Bootstrap` (browserwasm), AndroidX packages (android), `Uno.Fonts.Fluent`, `Uno.Resizetizer`, MSAL, MSTest, etc. — the same set the three heads carry today. Versions remain pinned via the repo's `PackageReference … Update` convention (Central Package Management stays disabled).

## 5. Local `Uno.Sdk` bootstrapping

Because `msbuild-sdks` versions must be exact and nothing in-repo consumes the local SDK yet:

1. **Pack** `src/Uno.Sdk` → `Uno.Sdk.Private` at a **fixed sentinel version** (e.g. `255.255.255-dev`) into the existing local feed `src/PackageCache`.
2. **Pin** that version in the **root `global.json`** `msbuild-sdks` block. This is isolated: only the new head declares `Sdk="Uno.Sdk.Private"`; `SolutionTemplate` apps keep their own deeper `global.json` and are unaffected.
3. A **pre-restore/pre-build step** packs the SDK before the head builds — both in CI and as a documented local one-liner — analogous to the existing `PreBuildUnoUITasks` pattern. (Alternative considered: `MSBuildSDKsPath` env var pointing at `src/Uno.Sdk/Sdk` — rejected as less CI-robust and harder to version.)

The fixed sentinel version avoids the per-commit version drift that the published-version `global.json` pins in `SolutionTemplate` would otherwise impose.

## 6. Shared content & the base library

- The new head imports the **existing content globs directly** — `SamplesApp.Shared.props`, `SamplesApp.Samples.props`, `SamplesApp.UnitTests.Shared.props`, `Benchmarks.Shared.projitems` — exactly as `SamplesApp.Skia` does today, so all samples/tests/benchmarks compile into it per-TFM. This sidesteps the base-lib's `net9.0;net10.0`-only TFMs, which cannot be referenced from the mobile TFMs.
- `SamplesApp.Skia` (base lib) is **left untouched during the alongside phase** (the three old heads still reference it) and removed in cleanup if nothing else depends on it.
- `Benchmarks.Shared.projitems` must be checked for stale absolute paths during P0 (one was flagged during discovery).

## 7. Entry points, platform code & assets

Adopt the `Uno.Sdk` single-project layout, consolidating today's per-head entry points into `Platforms/`:

| Folder (TFM) | Entry point | Host builder |
|--------------|-------------|--------------|
| `Platforms/Desktop` (`-desktop`) | `Program.cs` | `UseWin32().UseX11().UseLinuxFrameBuffer().UseMacOS()` (runtime OS detection) |
| `Platforms/WebAssembly` (`-browserwasm`) | `Program.cs` | `UseWebAssembly()` (async) |
| `Platforms/Android` (`-android`) | `Application` + `MainActivity` | host builder via `UseSkia`/native app |
| `Platforms/iOS` (`-ios`) | `Main.cs` | `UseAppleUIKit()` |
| `Platforms/tvOS` (`-tvos`) | `Main.cs` | `UseAppleUIKit()` |

- Per-platform **manifests/assets** move under their `Platforms/*` folder: `app.manifest` (desktop/Windows DPI), macOS `Info.plist` + `.app`-bundle target (conditioned on `IsOsPlatform('OSX')`), `AndroidManifest.xml` + Android assets, iOS/tvOS `Info.plist`/entitlements, WASM `WebContent`/`LinkerConfig.xml`/`AppManifest.js`/`aot.profile`. MacCatalyst assets dropped.
- App-level conditional compilation (`#if __SKIA__ / __WASM__ / __ANDROID__ / __APPLE_UIKIT__`) is preserved — each TFM compiles the shared sources with the correct symbols, driven by `Uno.Sdk` + the repo's crosstargeting. Legacy `XAMARIN` / `RUNTIME_CORECLR` defines are reconciled per-TFM (mobile only) so desktop builds don't see them.
- File-suffix exclusion (`.skia.cs`, `.wasm.cs`, `.Android.cs`, `.iOS.cs`, `.UIKit.cs`, `.crossruntime.cs`, …) is owned by the SDK/repo crosstargeting — the head must not hand-roll `Compile Remove` rules (see §8 risk).

## 8. Build-system integration

- **Register** the new head in `_AdjustedOutputProjects` (`src/Directory.Build.props`) so its multi-TFM output gets an isolated `bin/$(MSBuildProjectName)` and does not collide.
- **`targetframework-override` interaction:** the head must respond correctly to `UnoTargetFrameworkOverride` (CI passes e.g. `net10.0-android`) so a single platform can be built in isolation. Reconcile against `targetframework-override.props` / `-noplatform.props` suffix detection, which keys on csproj filename.
- **Top risk — `Directory.Build` × `Uno.Sdk` layering (gating spike P0):** the repo's src-wide `Directory.Build.props/.targets` and `Uno.CrossTargetting.targets` were authored assuming `Microsoft.NET.Sdk`. Layering `Uno.Sdk` on top can double-apply platform symbols, suffix exclusion, version pinning, and TFM override. P0 proves a desktop-only slice end-to-end before widening. Mitigations available: selectively opting the head out of specific repo targets, or relying on `Uno.Sdk`'s `CustomAfterDirectoryBuildProps` ordering (it already hooks platform detection after `Directory.Build.props`).
- `PreBuildUnoUITasks` / `Uno.UI.Tasks` shadow-build behavior must be preserved for CI and clean local builds.

## 9. Testing & UITest coupling

- `SnapShotTestGenerator` keys on assembly name **`SamplesApp.UITests`** and the **`SamplesApp.Samples/`** path marker; `SamplesListGenerator` ignores the `SamplesApp.UITests` assembly. Keeping samples in place and not touching the UITests project means **both generators keep working**.
- Desktop runtime-test invocation changes from `dotnet SamplesApp.Skia.Generic.dll --runtime-tests=…` to `dotnet SamplesApp.dll --runtime-tests=…` when each desktop stage is flipped.
- `IsUiAutomationMappingEnabled` (mobile-only today) and `ApplicationId` are set per-TFM as today; Android CI variants (`coreclr`, `nativeaot`) keep overriding `ApplicationId`/runtime properties via `-p:` flags.
- `SamplesApp.UITests` `Constants.cs` identifiers (app ids, WASM dev URL) are reviewed per stage; bucketization env vars (`UNO_UITEST_BUCKET_COUNT`) re-tuned only if the generated test-class count shifts materially.

## 10. CI migration (alongside → flip → delete)

The three heads are consumed by ~15+ Azure DevOps YAML files and shell/PowerShell scripts that hardcode project paths, TFMs, assembly/APK/`.app` names, artifact names (`samplesapp-desktop-skia`, `-wasm-skia`, `-android-skia[-coreclr|-nativeaot]`, `-ios-skia`), and the app id. Migration is per-platform:

For each platform stage, in its own change: point the build at the new csproj, set the platform TFM (`net10.0-desktop` / `-browserwasm` / `-android` / `-ios`), update expected output names and artifact-collection paths, update the runtime-/UI-test launch script, and update `Uno.UI-Skia-only.slnf`. Verify the stage green before the next.

## 11. Rollout phases (→ implementation plan)

| Phase | Deliverable | Exit criterion |
|-------|-------------|----------------|
| **P0** | Local-`Uno.Sdk` pack+pin bootstrapping; desktop-only slice proving the `Directory.Build` × `Uno.Sdk` layering | Desktop head builds & runs from source locally |
| **P1** | Desktop head complete; CI desktop stages (Windows/Linux/macOS/X11/FrameBuffer) flipped | Desktop runtime tests green on the new head |
| **P2** | Add `browserwasm`; flip WASM CI | WASM runtime/UI tests green |
| **P3** | Add `android`; flip Android CI (default/coreclr/nativeaot) | Android runtime tests green |
| **P4** | Add `ios` + `tvos`; flip iOS CI (+TestFlight publish) | iOS runtime/UI tests green |
| **P5** | Delete the three old heads, the base lib if unused, dead scripts and `.slnf`/pipeline entries | Solution + CI reference only the new head |
| **P-implicit** | Finalize `<UnoFeatures>` list; validate `SamplesAppUseImplicitPackages=true` (against `UnoNugetOverrideVersion` local packages) | Head builds & runs in implicit-packages mode on at least desktop + wasm |

## 12. Risks

1. **`Directory.Build` × `Uno.Sdk` layering** — highest risk; P0 is a hard gate.
2. **Local SDK version drift** — mitigated by the fixed sentinel version + pack step.
3. **Multi-TFM build cost** — one `dotnet build` now spans all platforms; mitigated by `.slnf` and `UnoTargetFrameworkOverride` for local single-platform iteration.
4. **Per-TFM `ProjectReference` leakage** — a wrong-platform reference compiling into the wrong TFM; mitigated by strict `Condition`s + per-stage CI verification.
5. **Implicit-mode feature completeness** — the `<UnoFeatures>` list must cover everything the samples use; validated in P-implicit, off the critical path.

## 13. Out of scope

- Removal of the native heads (`SamplesApp.Wasm`, `SamplesApp.netcoremobile`, native `SamplesApp.Windows`) — handled separately.
- MacCatalyst support.
- Changes to the `UnoIslands*` sample apps.
- Any change to how external customers consume the **published** `Uno.Sdk`.

## 14. Open items (non-blocking)

- Keep tvOS, or drop it if it adds no CI value?
- Final project/folder name (rename in P5 cleanup vs keep `SamplesApp.Skia.Head`).
- Exact `<UnoFeatures>` list for implicit mode (finalized in P-implicit).

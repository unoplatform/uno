# Packaging the drawing-backend impl projects — execution plan

Core (done): Uno.UI.Composition.Drawing + .Managed added to Uno.WinUI.nuspec.
Skia-imply flip (done): Uno.Features.targets force-implies `skia` (was `skiarenderer`).

## Remaining
1. UnoFeature.cs: drop `SkiaRenderer`; add `WebGpu` ([UnoArea.Core]).
2. Sdk.props.buildschema.json: drop SkiaRenderer entry; add WebGpu.
3. Uno.Features.targets: rewrite `;skiarenderer;`->`;skia;` for back-compat (before the imply).
4. packages.json: version entries for new package IDs Uno.UI.Composition.Skia, Uno.UI.Composition.WebGpu.
5. Nuspecs: build/nuget/Uno.UI.Composition.Skia.nuspec + Uno.UI.Composition.WebGpu.nuspec.
6. build/Uno.UI.Build.csproj: add both to _NuspecFiles + pack invocations.
7. Targets: 
   - ProjectSystem.Uno.targets: `;skia;`-gated include of Uno.UI.Composition.Skia (Uno heads).
   - New `;webgpu;`-gated include of Uno.UI.Composition.WebGpu (additive; coexists).
   - Gate Svg.Skia/SkiaSharp.Skottie/Uno.WinUI.Svg/Lottie on `;skia;`.
8. Docs: SkiaRenderer references (migration + using-the-uno-sdk + using-skia-rendering).

## Validation (END only — slow, maybe another machine)
- Release-build impl projects so nuspec file paths exist.
- CI-style `nuget pack` of the new nuspecs.
- Create a fresh Uno app against the built packages; confirm it runs.

## Validation gap found (fixed, commit 3c314f9f40)
The CI package build (`build/filters/Uno.UI-packages-all.slnf`, built by `BuildCIPackages`) listed only
`Uno.UI.Composition`. `Uno.UI.Composition.Managed` and `Uno.UI.Composition.SkiaBackend` are referenced only by
SamplesApp/tests (NOT by Uno.UI / the Runtime.Skia hosts), so their assemblies were never produced on the
packaging leg — both `Uno.WinUI` (hard-refs Managed) and `Uno.UI.Composition.Skia` nuspec packs would have failed
on missing DLLs. Added both to `Uno.UI-packages-all.slnf` + `Uno.UI-packages-skia.slnf`. `Composition.Drawing` is
already pulled transitively via `Uno.UI.Composition`.

## Validation environment (Windows build host)
- Host `UNO-BM-0011` via `ssh winhost` (user unodev), checkout at `E:\uno`, remotes: origin=ramezgerges.
- net11 preview SDK (`11.0.100-preview.7`) + android/ios/maccatalyst/maui/wasm workloads installed.
- `global.json` pins `allowPrerelease:false` → stable SDK 10.0.302 (can't target net11). Flip to `true` on the host
  (uncommitted) so the preview SDK is selected for the net10+net11 pack build. `git checkout global.json` reverts.
- CI pack path: `BuildCIPackages` (builds packages-all.slnf Release, net10+net11) then `BuildNuGetPackage`
  (`nuget pack` each nuspec, version from NBGV_SemVer2). Pack reads pre-built DLLs from `src/.../bin/.../Release/{net10,net11}`.

## STATUS (this pass)
DONE + locally validated (Uno.Sdk compiles; JSON/XML valid; Skia backend Release build lands at the nuspec path):
- UnoFeature.cs: dropped SkiaRenderer, added WebGpu.
- Sdk.props.buildschema.json: dropped SkiaRenderer, added WebGpu.
- Uno.Features.targets: force-imply `skia` (was skiarenderer); strip/rewrite `skiarenderer`->`skia` (back-compat).
- packages.json: Uno.UI.Composition.Skia + Uno.UI.Composition.WebGpu in Core group (DefaultUnoVersion).
- ProjectSystem.Uno.targets: `;skia;` -> Uno.UI.Composition.Skia + SkiaSharp.Views; `;webgpu;` -> Uno.UI.Composition.WebGpu (additive); SkiaSharp.Skottie/Svg.Skia gated on `;skia;`.
- Uno.UI.Composition.Skia.nuspec (new) + wired into build/Uno.UI.Build.csproj (_NuspecFiles + pack Exec).
- Docs: using-the-uno-sdk.md, using-skia-rendering.md.

## VALIDATION RESULTS (Windows build host, commit 3c314f9f40)
Evidence labels: **Compile** = built; **Package** = packed/inspected; **Runtime** = restored/ran.
- **Compile**: targeted Release build of the Uno.WinUI + Composition.Skia nuspec inputs (Uno.UI, Composition,
  Drawing, Managed, SkiaBackend, FluentTheme.v2, Toolkit.Skia) — all net10.0 + net11.0, 0 errors.
- **Package**: Drawing + Managed DLLs land at the exact `Uno.WinUI.nuspec` `<file src>` paths (net10+net11);
  SkiaBackend lands at the `Uno.UI.Composition.Skia.nuspec` paths. `Uno.UI.Composition.Skia.nuspec` packs into a
  valid nupkg (bundled NuGet.exe): correct `lib/net10.0` + `lib/net11.0` folders, `uno.png`, deps
  `Uno.WinUI` + `SkiaSharp 4.151.1` under both groups.
- **Runtime**: a net10.0 project referencing the packed `Uno.UI.Composition.Skia` (local feed) triggers NuGet to
  resolve its transitive `Uno.WinUI` dependency — proving the dependency group applies to a net10.0 consumer.
- **Note (pre-existing, branch-wide, NOT this change)**: the bundled `build/external/nuget/NuGet.exe` is 5.4.0
  (2019); it normalizes `net10.0`/`net11.0` dependency-group TFMs to `.NETFramework10.0/11.0`. The modern client
  tolerates this (deps still flow, verified above); the existing `Uno.WinUI.nuspec` uses the same net10/net11
  groups, so any fix belongs to the branch's net10/11 bring-up, not the drawing-backend packaging.
## FULL CI-STYLE BUILD + PACK + SDK-RESOLVE (Windows host, second pass)
- **packages-all.slnf** (the CI packaging leg) builds **clean, 0 errors** in Release across net10+net11 with the
  filter fix — and auto-packs all 7 `Uno.WinUI.Runtime.Skia.*` host packages + `Uno.Sdk.Private` at 7.0.0-dev.1.
  Prereqs installed on the host: **tvos** workload + **Android API-37** platform SDK (`-t:InstallAndroidDependencies`).
  A prerelease PackageVersion (7.0.0-dev.1) is required or the host auto-pack trips NU5104.
- **Uno.WinUI pack**: packed a Skia-slice `Uno.WinUI.nuspec` (net10/net11 lib groups; the `net*-windows` WinAppSDK
  sections dropped — see blocker below). Inspected the nupkg: **`Uno.UI.Composition.Drawing` + `.Managed` present in
  BOTH `lib/net10.0` and `lib/net11.0` (4/4)** — the core nuspec change is proven in the produced artifact.
- **SDK feature resolution** (ran the real `Uno.Sdk.Private` `ImplicitPackagesResolver` task via
  `dotnet msbuild -t:UnoImplicitPackages -getItem:PackageReference`, local feed):
  - `skia`  → injects `Uno.UI.Composition.Skia` (+ SkiaSharp.Views, Skottie). ✓
  - `''` (empty) → skia force-implied → same as `skia`. ✓
  - `skiarenderer` → identical to `skia` (legacy feature mapped for back-compat). ✓
  - `webgpu` / `skia;webgpu` → **adds `Uno.UI.Composition.WebGpu`** and keeps Skia (additive). ✓
  This validates the entire UnoFeature wiring change end-to-end through the actual resolver.

## BLOCKER (pre-existing, env-level, NOT this change): WinAppSDK/WinUI build leg
The `net*-windows` (WinAppSDK) library builds — `Uno.UI.Toolkit.Windows`, `Uno.UI.MSAL.Windows`,
`Uno.WinUI.Graphics2DSK.Windows` — cannot build on this net10/net11-preview-only bench: WindowsAppSDK **1.0.0**'s
XAML markup compiler (`Microsoft.UI.Xaml.Markup.Compiler.dll`, 2021) fails to instantiate under the modern MSBuild
task host (`MissingMethodException` / "Type must be a type provided by the runtime" on `CompileXaml..ctor`), under
BOTH the net11 preview SDK and the net10 stable SDK. WindowsAppSDK 1.0.0 is pinned framework-wide
(`src/Uno.UI.Toolkit/Uno.UI.Toolkit.Windows.csproj` + others). This is entirely in the WinUI/WinAppSDK packaging leg
and orthogonal to the drawing-backend abstraction; CI builds this leg on a different (older-MSBuild) toolchain combo.
Consequence: the stock `BuildNuGetPackage` (which packs all 10 nuspecs incl. the Windows assets + the Toolkit.Windows
PRI `<Error>` guard) can't run unmodified here. Validated the Skia slice instead (above), which is exactly what a Skia
desktop/mobile/wasm app consumes; the WinAppSDK head does not consume the drawing-backend assemblies.

Host left with: `global.json` `allowPrerelease:true` (net11 SDK selection — uncommitted, `git checkout` reverts);
tvos + Android-API-37 installed; `E:\uno\_pkgout` holds the produced `Uno.UI.Composition.Skia` + Skia-slice `Uno.WinUI`
nupkgs for reference.

REMAINING (pack-machine / follow-up):
- WebGPU package: nuspec + native wgpu provisioning (buildTransitive targets or pre-fetched runtimes/) + pack Exec.
  Feature wiring is ready; the package artifact is the missing piece. wgpu-native.targets is written for the app
  head and fetches per-platform natives at build — packaging it for distribution needs pack + multi-platform validation.
- Final validation: CI-style `nuget pack`, then a fresh Uno app against the built packages, confirm it runs.
- Open: global skia-imply makes WinAppSDK always pull SkiaSharp.Views.WinUI (decide whether to scope to Uno heads).
- Migration guides (uno-6/uno-7) still mention SkiaRenderer; advice remains valid (kept for now).

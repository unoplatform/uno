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

## STATUS (this pass)
DONE + locally validated (Uno.Sdk compiles; JSON/XML valid; Skia backend Release build lands at the nuspec path):
- UnoFeature.cs: dropped SkiaRenderer, added WebGpu.
- Sdk.props.buildschema.json: dropped SkiaRenderer, added WebGpu.
- Uno.Features.targets: force-imply `skia` (was skiarenderer); strip/rewrite `skiarenderer`->`skia` (back-compat).
- packages.json: Uno.UI.Composition.Skia + Uno.UI.Composition.WebGpu in Core group (DefaultUnoVersion).
- ProjectSystem.Uno.targets: `;skia;` -> Uno.UI.Composition.Skia + SkiaSharp.Views; `;webgpu;` -> Uno.UI.Composition.WebGpu (additive); SkiaSharp.Skottie/Svg.Skia gated on `;skia;`.
- Uno.UI.Composition.Skia.nuspec (new) + wired into build/Uno.UI.Build.csproj (_NuspecFiles + pack Exec).
- Docs: using-the-uno-sdk.md, using-skia-rendering.md.

REMAINING (pack-machine / follow-up):
- WebGPU package: nuspec + native wgpu provisioning (buildTransitive targets or pre-fetched runtimes/) + pack Exec.
  Feature wiring is ready; the package artifact is the missing piece. wgpu-native.targets is written for the app
  head and fetches per-platform natives at build — packaging it for distribution needs pack + multi-platform validation.
- Final validation: CI-style `nuget pack`, then a fresh Uno app against the built packages, confirm it runs.
- Open: global skia-imply makes WinAppSDK always pull SkiaSharp.Views.WinUI (decide whether to scope to Uno heads).
- Migration guides (uno-6/uno-7) still mention SkiaRenderer; advice remains valid (kept for now).

# Merge reconciliation — items to review (feature/breakingchanges merge)

## Dropped upstream additions (incompatible with our neutral-drawing seam)
- **CompositionSpriteShape.skia.cs**: upstream added raw-Skia damage-tracking helpers
  `TryGetRenderBounds(out SKRect)` + `GetRenderPath(SKPathBuilder)` (partial-invalidation
  optimization) that use `SKRect`/`SKPaint`/`IGeometry.Geometry`(SKPath). These don't compile
  against our neutral `IGeometry` (Bounds is a `Rect`, no `SKPath` accessor). Took OUR neutral
  version; upstream's damage-tracking is DROPPED and needs re-porting onto the neutral seam if
  the perf optimization is wanted. Related callers in Visual/Compositor also affected.

## Host renderers — took OUR neutral versions (hosts hand a neutral render target to the backend)
Upstream kept/enhanced Skia rendering *inside* these hosts (e.g. WebGl: GRContext resource cache +
RetainedLayer present; FrameBuffer vsync/blit specifics). Our branch neutralized them, so their Skia-render
enhancements are dropped in favor of the neutral render-target handoff. Files: Win32 OpenGl, MacOSWindowHost,
Android UnoSKCanvasView + ApplicationActivity, AppleUIKit UnoSKMetalView + RootViewController,
FrameBuffer DRM/FrameBuffer/Software renderers, WASM WebGlBrowserRenderer. Hosts are maintenance-only; verify
each still drives the neutral pipeline correctly at runtime.

## Relocation-reconcile — kept OUR neutral versions, dropped upstream's renamed-file deltas
Upstream renamed these .skia.cs -> .cs (cross-platform-ized) with content changes; we kept our neutral
.skia.cs and dropped upstream's .cs. UN-FOLDED upstream deltas to review/re-integrate:
- CompositionTarget.Rendering: upstream had a **253-line** change (frame scheduling / rendering) — NOT folded in.
- SkiaRenderHelper: upstream had a **58-line** change — NOT folded in.
- SKCanvasVisual: upstream had a **6-line** change — NOT folded in (our version lives in Uno.UI.Composition.Skia).
Also: upstream made these cross-platform (.cs); ours are Skia-only (.skia.cs) — verify non-Skia variants still build.

## Rendering core — took OUR neutral versions (Visual/Compositor/etc. driven by IDrawingSession)
Both sides rewrote these; kept our neutral versions. Upstream's parallel changes NOT folded in — review:
Visual.skia.cs (10 hunks), Compositor.skia.cs (3), SpriteVisual.skia.cs (2), Visual.PaintingSession.skia.cs (2),
CompositionEffectBrush.skia.cs (2), ContainerVisual.skia.cs (usings union). If upstream added rendering fixes
here (damage tracking, invalidation, effect changes), they must be re-applied on the neutral seam.

## Add-ins + packaging
- SvgProvider.cs: took OURS (managed-SVG engine + MaxRasterizePixelCount security bound). Upstream's SvgProvider
  changes (if any) dropped — review.
- SKCanvasElement.cs (Graphics2DSK): took OURS (SkiaGLCanvasElement). UNCERTAIN — upstream uses the
  SKCanvasVisualBaseFactory/ApiExtensibility path, which is what our SkiaBackend.Register registers. Verify the
  2DSK addin builds and renders; may need upstream's factory-based version instead.
- Uno.WinUI.nuspec: took UPSTREAM's unified bin paths (ours referenced the now-removed .Skia variant output
  paths). TODO: add package entries for Uno.UI.Composition.SkiaBackend.dll + Uno.UI.Composition.Drawing.dll
  (+ WebGpu) under uno-runtime/*/skia for the drawing-backend feature to ship.

## Uno.UI subsystems — restored to OUR neutral (SkiaSharp-free) versions (44 files) + deleted 6 upstream Skia-only
Our branch neutralized large swaths of Uno.UI (text/image/shape/geometry/brush) to be SkiaSharp-free; the merge
mixed in upstream's Skia versions (SKPath/SkiaGeometrySource2D/SKSurface/SKFontStyle*). Restored our versions for
~44 files (Shapes/*, Geometry/*, Imaging/*, TextBlock/TextBox/Inline/Run, AcrylicBrush, Image, LoadedImageSurface,
Application.skia, UIElement.mux). Upstream's parallel changes to these are NOT folded in — REVIEW for lost fixes.
Deleted (upstream-only, absent from our branch): FontStyleExtensions, FontWeightExtensions, FillRuleExtensions
(Skia SKFontStyle/SKPathFillType mappers), RetainedLayer (GRContext/SKSurface layer cache), TextVisual.

## BUILD CONVERGENCE STATUS (as of this WIP commit)
Merge committed (75724d5). Build driven from ~1000 -> 272 errors. Uno.UI.Composition builds clean.
Remaining 272 are in Uno.UI, all the SAME pattern: upstream DELETED per-control .skia.cs files and consolidated
them into cross-platform .cs (Skia-coupled), while our branch kept neutral .skia.cs. Neither works alone:
  - restore-ours .skia.cs  -> CS0111/CS0102/CS8646 duplicate members (upstream's consolidated .cs has them too)
  - take-upstream .cs       -> CS0246 SkiaSharp types (our Uno.UI is SkiaSharp-free)
Correct fix = per-file 3-way reconcile: fold our neutral (SkiaSharp-free) logic into upstream's consolidated .cs
and delete the duplicate .skia.cs. Hot spots (by error count): TextBlock (184 — our HarfBuzz managed-text rewrite
vs upstream), Application (30), BitmapImage (20), TextBox (8), Shape (2). This is a deep reconcile of the managed-
text/imaging subsystems and needs the author's knowledge of the managed-text rewrite.

## Build-convergence API reconciliations (small fixes to reach green)
- IsLocalResource -> IsMsAppx: applied upstream's Uri-extension rename in BitmapImage.skia.cs.
- FeatureConfiguration.SkipVisualTreePainting: upstream test optimization delegating to Compositor.SkipVisualTreePainting
  (absent in our neutral Compositor) -> made a no-op. Re-add if the test-paint-skip optimization is wanted.
- TextBox.pointers.cs: upstream-new file referencing TouchSelectionConvention/TextVisual (not in our TextBox model)
  -> deleted; our TextBox.skia.cs handles pointers. Verify touch text-selection behavior.
- TextVisual.skia.cs: our neutral text visual (merge had dropped it) -> restored.

## FINAL: build GREEN
SamplesApp.Skia.Generic (Skia + WebGPU backends) builds with 0 errors after the full merge.
Additional host/test reconciliations applied to reach green:
- MacOSWindowHost: PointerPoint/PointerPointProperties -> Microsoft.UI.Input (settable), PointerEventArgs/
  KeyEventArgs aliased to Windows.UI.Core, PointerDeviceType aliased to Windows.Devices.Input,
  WindowActivationState -> Microsoft.UI.Xaml.WindowActivationState. (matches X11's input pattern)
- X11XamlRootHost, Given_CompositionSpriteShape, Given_TextBox, GLCanvasElement: restored our neutral versions.
- Graphics2DSK re-refs Graphics3DGL; Graphics3DGL gets a SkiaSharp ref (addins may use SkiaSharp).
- RuntimeTests.Skia.csproj: GenerateTargetFrameworkAttribute=false (CS0579 duplicate from custom obj path).
- Given_Path_FillRule: Path alias (System.IO.Path vs Shapes.Path ambiguity).
- Given_TextBox: StackPanel collection-init -> .Children.Add.
NOTE: Only the Skia desktop head is build-verified. WASM/Android/iOS heads + runtime behaviour NOT yet verified.

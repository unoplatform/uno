**GitHub Issue:** closes #21176

## PR Type:

✨ Feature

## What changed? 🚀

> **Stacked on [#23889](https://github.com/unoplatform/uno/pull/23889).** This branch is cut from
> `agents/animated-visual-player-port` and contains only the commits on top of it. It must be merged
> **after** #23889; reviewing it against `feature/breakingchanges` directly will show #23889's diff as well.

#23889 ports `AnimatedVisualPlayer` itself and moves `Uno.UI.Lottie` onto `IAnimatedVisualSource3`.
That covers the runtime `.json` path (Skottie), but the *other* half of the WinUI story — LottieGen
**codegen** output, i.e. a `.json` compiled ahead of time into a C# `IAnimatedVisualSource` that builds
raw `Microsoft.UI.Composition` objects — did not run, because the Composition primitives it emits were
missing or wrong. This PR fills those in, so both kinds of Lottie work.

### 1. Composition expression + keyframe engine

LottieGen output is almost entirely `ExpressionAnimation` strings and keyframe animations. What was missing:

- **28 expression-animation function specifications** added to the parser table
  (`src/Uno.UI.Composition/Composition/ExpressionAnimationParser/FunctionSpecifications/`): scalar math
  (`Sin`/`Cos`/`Tan`/`Asin`/`Acos`/`Atan`/`Sqrt`/`Exp`/`Log`/`Log10`/`Mod`/`Floor`/`Ceil`/`Round`),
  the `Matrix3x2` and `Matrix4x4` factories (`CreateTranslation`, `CreateScale`, `CreateRotation`,
  `CreateSkew`, `CreateFromTranslation`, `CreateFromScale`, `CreateFromAxisAngle`) and
  `Quaternion.CreateFromAxisAngle`.
- **Scalar-to-vector broadcast**: assigning a scalar to a whole `VectorN` now fans out to every
  component, matching WinUI. LottieGen relies on this.
- **`ColorKeyFrameAnimation`** and **`PathKeyFrameAnimation`** implemented, with their `Compositor`
  factories; the corresponding `Generated/` stubs reconciled (`// Skipping already declared…`, type-level
  `[Uno.NotImplemented]` moved under `#if false`).
- **Shared-animation lifetime** (`CompositionAnimation`, `ExpressionAnimation`, `KeyFrameAnimation`):
  a single animation instance is legitimately started on many `CompositionObject`s — LottieGen emits one
  shared progress `ExpressionAnimation` driving dozens of controllers. Previously only the *last* target
  was tracked, so teardown unregistered one of N. Targets are now tracked as a list, the parsed
  expression tree is parsed once and reused (re-parsing per `Start` re-registered reference-parameter
  contexts, giving O(N²) re-evaluation and leaking registrations on teardown), and `PlaybackRate` set
  before `Start()` is now applied when the evaluator is created instead of throwing on a null evaluator.

### 2. Geometry, trim and gradient render fixes

- **Ellipse geometry now starts at 12 o'clock and winds clockwise**, like WinUI/After Effects, so a trim
  window measures from the top. The previous 3 o'clock (WPF-style) origin put a `[0, 0.25]` trim in the
  bottom-right quadrant instead of the top-right — which is exactly what a generated determinate progress
  arc depends on.
- **`TrimOffset` is honoured** instead of ignored; a window that wraps past 1.0 draws the union of both halves.
- **`CompositionVisualSurface` clips to the surface height** rather than reusing its width.
- **Animatable-property accessors** added where LottieGen animates them: `CompositionGradientBrush`,
  `CompositionColorGradientStop`, `CompositionLinearGradientBrush`, `CompositionRadialGradientBrush`,
  `CompositionRectangleGeometry`, `CompositionPathGeometry`.
- **`CompositionSpriteShape.StrokeDashArray` is a live, non-null collection**, matching WinUI.
  `Shape.UpdateStrokeDashArray` clears it instead of assigning `null`, and the renderer reads the
  backing field so an empty collection still means "no dashes".

### 3. `ProgressRing` renders through generated Composition visuals

`ProgressRing` now uses LottieGen-generated `ProgressRingDeterminate` / `ProgressRingIntdeterminate`
composition sources (`src/Uno.UI/UI/Xaml/Controls/ProgressRing/AnimatedVisuals/`), the same as WinUI,
instead of loading `.json` through the `Uno.UI.Lottie` add-in. The `ILottieVisualSourceProvider`
extensibility lookup and the `UNOX0001` "additional package required" placeholder are gone from the
control. Theme colours flow through `IAnimatedVisualSource2.SetColorProperty` rather than the Uno-only
`IThemableAnimatedVisualSource.SetColorThemeProperty`. `IndeterminateSource` / `DeterminateSource` still
override the defaults.

This is also the first real consumer proving the engine above: the generated ring uses expression
animations, a colour keyframe animation and a trimmed ellipse.

### 4. Corrections to #23889

- **Play duration** is computed in the `AnimationPlay` constructor as WinUI does
  (`AnimatedVisualPlayer.cpp:22-27`), not at `Start()`. Because Uno's animated-visual sources load
  asynchronously, a `PlayAsync()` issued before the source resolves would carry a zero duration and
  self-complete through the `< 20ms` fast path; the duration is recomputed only in that case, leaving
  every path WinUI exercises unchanged.
- **Hit-test scoping**: #23889 changed `FrameworkElement.IsViewHit()` to `HasCompositionChildVisual`
  for *every* `FrameworkElement`. Reverted to `false`, with the override moved onto `AnimatedVisualPlayer`
  where WinUI's transparent-background trick applies (`AnimatedVisualPlayer.cpp:391-395`).
- **`AnimationController.MinPlaybackRate`/`MaxPlaybackRate`** corrected from `float.MinValue`/`MaxValue`
  to the documented `-16f`/`16f`.
- **Generated stub**: `Microsoft.UI.Xaml.Controls.IAnimatedVisualSource` had a live `[Uno.NotImplemented]`
  under `#if __SKIA__` on an interface that is implemented on Skia. Moved under `#if false`, with an
  empty non-generated `public partial interface IAnimatedVisualSource` so the sync generator sees it as
  defined by Uno.
- **Dead file removed**: `src/AddIns/Uno.UI.Lottie/LottieVisualSource.reference.cs`
  (`__NETSTD_REFERENCE__` no longer exists).
- **Build**: `src/Uno.UI.RuntimeTests/Directory.Build.targets` excludes `HRUnoLib/bin` and `HRUnoLib/obj`
  from the test compile — once that project has been built, its generated assembly-attribute files were
  compiled a second time into the test assembly (CS0579).

### 5. Win2D types made public — sequencing constraint ⚠️

LottieGen emits `-Public` output that references `CanvasGeometry`, `CanvasPathBuilder`,
`CanvasDevice`, `ICanvasResourceCreator` and the `CanvasArcSize` / `CanvasFigureFill` /
`CanvasFigureLoop` / `CanvasFigureSegmentOptions` / `CanvasFilledRegionDetermination` /
`CanvasSweepDirection` enums directly, so those 10 types under
`src/Uno.UI.Composition/Win2D/Microsoft/Graphics/Canvas/` go from `internal` to `public`.

**These are the same types the planned `Uno.WinUI.Graphics.Win2D` extraction moves out of
`Uno.UI.Composition` into their own assembly.** That extraction is a separate PR and explicitly out of
scope here, but the two must **ship in the same release**: if the types become public in release *N* and
move assemblies in *N+1*, anyone who compiled against them in *N* breaks. If the extraction slips, this
PR should be held or the types kept internal with `InternalsVisibleTo`.

### Both kinds of Lottie

| Path | How it works | Covered by |
|---|---|---|
| Runtime `.json` | `LottieVisualSource` parses at runtime and renders through Skottie | `Given_AnimatedVisualPlayer` real-Lottie tests (from #23889): `When_Lottie_SetSourceAsync_Completes_Source_Is_Ready`, `When_Real_Lottie_Source_Swaps_Uri_Player_Reloads`, `When_Real_Lottie_Source_Fails_Player_Shows_Fallback_Content`, `When_Real_Lottie_Source_Retries_Same_Uri_After_Failure_It_Loads` |
| Compiled (LottieGen) | `.json` compiled ahead of time to C# that builds `Microsoft.UI.Composition` objects | `Given_Generated_Lottie.When_Generated_Source_Loads_And_Renders` — new in this PR |

The generated-Lottie test drives **unmodified LottieGen output** (`-Language CSharp -Public
-WinUIVersion 3.0`, checked in verbatim) through `AnimatedVisualPlayer`, asserts
`IsAnimatedVisualLoaded` and a positive `Duration`, scrubs `SetProgress` across the shared controller,
then screenshots and requires non-background pixels. Three sources chosen to hit different engine paths:

- **Watermelon** — colour keyframes, path morphing, two controllers
- **Gradient_shapes** — animated linear and radial gradient brushes
- **LottieLogo2** — 165 trim animations on a shared controller

A manual sample is added at **Lottie → Generated animations playground**
(`src/SamplesApp/SamplesApp.Samples/AnimatedVisualPlayerPlayground/`) with five generated sources
(LottieLogo1, Watermelon, Gradient_shapes, PinJump, HamburgerArrow) plus stretch/progress/playback controls.

## PR Checklist ✅

- [x] 🧪 Added [Runtime tests, UI tests, or a manual test sample](https://github.com/unoplatform/uno/blob/master/doc/articles/uno-development/working-with-the-samples-apps.md) (for bug fixes / features, if applicable)
- [ ] 📚 Docs have been added/updated following the [documentation template](https://github.com/unoplatform/uno/blob/master/doc/.feature-template.md) (for bug fixes / features)
- [ ] 🖼️ Validated PR `Screenshots Compare Test Run` results.
- [ ] ❗ Contains **NO** breaking changes
- [ ] 👀 Reviewed 2 other [open pull requests](https://github.com/unoplatform/uno/pulls) (optional but appreciated!)

### Breaking changes

Targets `feature/breakingchanges`, alongside #23889.

- `ProgressRing` no longer reads `FeatureConfiguration.ProgressRing.ProgressRingAsset` /
  `DeterminateProgressRingAsset`; those properties still exist but no longer affect the control. Apps
  that pointed them at a custom `.json` should set `IndeterminateSource` / `DeterminateSource` instead.
- `ProgressRing` no longer requires the `Uno.UI.Lottie` add-in, and no longer renders the `UNOX0001`
  placeholder when it is absent.
- Theme colours on the ring go through `IAnimatedVisualSource2.SetColorProperty`; a custom source
  implementing only the Uno-only `IThemableAnimatedVisualSource` will no longer be recoloured.
- The 10 Win2D `Microsoft.Graphics.Canvas` types listed in §5 become public — see the sequencing note there.
- `AnimationController.MinPlaybackRate`/`MaxPlaybackRate` change value (unreleased; introduced in #23889).

### Validation

Runtime tests a reviewer should run (Skia Desktop, via the `/runtime-tests` skill or
`dotnet test` with the corresponding filter):

| Test class | Why |
|---|---|
| `Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls.AnimatedVisuals.Given_Generated_Lottie` | LottieGen end-to-end (3 data rows) — new |
| `Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls.Given_AnimatedVisualPlayer` | #23889's AVP + runtime `.json` Lottie suite, must stay green |
| `Uno.UI.RuntimeTests.Tests.Windows_UI_Composition.Given_CompositionSpriteShape` | `When_Ellipse_Trim_Starts_At_Top` — new, guards the 12 o'clock/clockwise orientation. The `When_Stroke_With_*` tests are pre-existing but their thickness measurement was reworked here to a coverage integral, so they must stay green |
| `Uno.UI.RuntimeTests.Tests.Windows_UI_Composition.Given_CompositionGeometry` | `When_Rectangle_Size_Referenced_By_Expression`, `When_StrokeDashArray_Is_Non_Null_And_Mutable` — new |
| `Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls.Given_ProgressRing` and `Uno.UI.RuntimeTests.MUX.Microsoft_UI_Xaml_Controls.ProgressRingTests` | regression guard for the generated-visual switch (pre-existing tests, not modified here) |

Manual: SamplesApp → **Lottie → Generated animations playground** (excluded from snapshot tests, so it
needs a human pass; the generated ring itself is covered by the normal ProgressRing snapshots).

Build: `dotnet build Uno.UI-Skia-only.slnf --no-restore -p:UnoTargetFrameworkOverride=net10.0 -p:UnoFastDevBuild=true`.

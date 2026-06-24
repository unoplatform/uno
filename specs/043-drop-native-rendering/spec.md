# Engineering Spec: Drop Native Rendering — Skia-Only Uno Platform

**Feature**: Remove the native UI rendering backends (native Android Views, native iOS/tvOS/macCatalyst UIKit, native WebAssembly DOM) and keep **Skia** as the single rendering engine across every target.
**Spec number**: 043
**Target release**: Uno Platform 7.0 (major, breaking)
**Feature branch**: `dev/mazi/drop-native`
**Created**: 2026-06-08
**Status**: Draft
**Audience**: Internal engineering plan (Uno Platform maintainers)

> **Relationship to the 7.0 release.** Native rendering removal is the centerpiece of Uno 7.0. A separately-curated set of additional 7.0 breaking changes is tracked outside this document; this spec is the source of truth for the breaking changes **caused by native rendering removal**. Where the two overlap, this spec governs the native-removal mechanics.

---

## 1. Summary

Uno.UI today renders the WinUI visual tree through **four** backends:

| Backend | How a `UIElement` is realized | Status after 7.0 |
|---|---|---|
| Native Android | `UIElement : BindableView : UnoViewGroup : Android.Views.ViewGroup` (one native `View` per element) | **Removed** |
| Native iOS/tvOS/macCatalyst | `UIElement : BindableUIView : UIView` (one native `UIView`/`CALayer` per element) | **Removed** |
| Native WebAssembly | `UIElement` ↔ one DOM element (`<div>`/SVG), driven by `WindowManager.ts` | **Removed** |
| **Skia** | `UIElement` owns a `Composition.Visual`; everything is drawn into one Skia surface (Win32/Metal/GL/SwiftShader) | **Kept (the only backend)** |

Skia already runs on **every** target — desktop (Win32, WPF, X11, GTK, macOS, FrameBuffer), Skia-on-Android, Skia-on-iOS (`Uno.UI.Runtime.Skia.AppleUIKit`), and Skia-on-WASM (`Uno.UI.Runtime.Skia.WebAssembly.Browser`). On mobile, Skia is currently **opt-in** through `<UnoFeatures>skiarenderer</UnoFeatures>`; native rendering is still the default. **7.0 makes Skia the implicit, exclusive UI rendering path** and deletes the three native backends and all of the abstractions that exist solely to bridge managed XAML to native view trees.

This is a **hard removal in a single major version** — no deprecation interim — performed under the assumption that **Skia-on-Android/iOS/WASM are already at feature parity** (parity validation is out of scope for this spec; see §4).

The payoff is large: a single rendering model, the elimination of the dual managed/native tree, ~900+ platform-suffixed UI files deleted, three source generators removed, three CI device-test stages dropped, the Java/JNI toolchain removed from `Uno.UI`, and the TypeScript DOM renderer removed — turning `Uno.UI` into a far more approachable, mostly-single-path C# codebase.

---

## 2. Goals and non-goals

### Goals
1. Delete the three native UI rendering backends and every mechanism that exists only to bridge them.
2. Collapse the dual managed/native identity of `UIElement`/`FrameworkElement`/`Visual` to a single managed (Composition/Skia) identity on all platforms.
3. Reduce the project/TFM/packaging matrix and the conditional-compilation surface to the Skia path.
4. Catalogue every consumer-facing **breaking change** with a migration note (§8).
5. Catalogue **codebase simplifications** that lower the barrier for new contributors (§7).
6. Produce an ordered, dependency-aware execution plan (§9) with validation gates (§10).

### Non-goals
1. **Parity validation.** We assume Skia-on-Android/iOS/WASM already match native behavior. Closing parity gaps is tracked elsewhere and is a precondition, not part of this spec.
2. **Removing platform (non-UI) WinRT APIs.** `Uno.UWP`/`Uno.Foundation` per-platform implementations (sensors, pickers, storage, connectivity, etc.) **stay** — Skia-on-Android/iOS consume them. Only the UI *rendering* layer is removed (see §3.2 for the nuance that a handful of `Uno.UWP` files *are* native-UI and do get removed).
3. **Consolidating Skia hosts.** The many `Uno.UI.Runtime.Skia.*` hosts (Win32/WPF/X11/GTK/macOS/FrameBuffer/Tizen/Android/AppleUIKit/WebAssembly.Browser) are out of scope; they all stay.
4. **New features.** This is pure removal + simplification.

---

## 3. Background: the architecture being removed

### 3.1 The dual-tree / dual-identity model

On native platforms a `UIElement` *is* a native view, which forces a parallel hierarchy that shadows the managed XAML tree:

- **Android:** `UIElement` inherits `BindableView` → `UnoViewGroup` (Java) → `ViewGroup`. Because a class cannot inherit both a native `ViewGroup` and `DependencyObject`, **`DependencyObject` is an interface on mobile**, with the implementation emitted by `DependencyObjectGenerator`. Measure/arrange route through native `View.measure()/layout()`; pointer input arrives via `UnoMotionHelper.java` → `MotionEvent`; transforms go through `getChildStaticTransformation`; children are mirrored in an `IShadowChildrenProvider.ChildrenShadow` list to avoid JNI marshalling.
- **iOS/tvOS/macCatalyst:** `UIElement` inherits `BindableUIView` → `UIView`. Layout routes through `SetNeedsLayout()`/`LayoutSubviews()`/`SizeThatFits()`; rendering uses `CALayer`/`CAShapeLayer`/`CAGradientLayer`; animation uses `CADisplayLink`/`CABasicAnimation` via `UnoCoreAnimation`; input arrives through `TouchesBegan/Moved/Ended`.
- **WASM DOM:** each `UIElement` maps to a DOM element created/measured/arranged/styled via `WindowManagerInterop.wasm.cs` (~929 lines) calling the `WindowManager.ts` singleton (~1,500 lines); layout is read back from `offsetWidth/offsetHeight`; styling is applied through `WasmCSS/Uno.UI.css` (~299 lines).

Skia uses **none** of this: `UIElement` is a managed object owning a `Composition.ContainerVisual`; measure/arrange are pure managed (`MeasureOverride`/`ArrangeOverride`); input flows through the managed `InputManager`/`PointerManager`; rendering is `Uno.UI.Composition` → Skia.

### 3.2 The two-layer boundary (critical — defines what stays)

Uno separates **UI rendering** (Skia-only after 7.0) from **platform capability APIs** (per-platform, kept). The boundary is enforced at build time by `RuntimeAssetsSelectorTask`: a Skia-on-Android app links the **generic Skia** `Uno.UI.dll` for rendering but the **`net*-android`** `Uno.Foundation.dll`/`Uno.dll` for sensors/pickers/etc.

Consequences:
- `Uno.UI` UI-rendering variants (`.netcoremobile`, `.Wasm`) are **removed**; the generic Skia variant becomes the only UI assembly.
- `Uno.UWP`/`Uno.Foundation` **keep** their `netcoremobile` TFMs — but a **small subset of their files are native UI** and are removed anyway (e.g. `Media/Playback/MediaPlayer.Android.cs`, `VideoSurface.Android.cs`, `StatusBar.Android.cs` edge-to-edge inset logic). So "Uno.UWP stays" ≠ "Uno.UWP is untouched."
- On WASM, `Uno.Foundation.Runtime.WebAssembly` (JS interop marshalling) **stays** (Skia-on-WASM needs it); `Uno.UI.Runtime.WebAssembly` (the DOM renderer) is **deleted**.

### 3.3 Magnitude (verified against the tree)

| Surface | Approx. count | Disposition |
|---|---|---|
| `*.Android.cs` (whole `src`) | ~393 | UI-rendering ones removed; platform-API ones kept |
| `*.UIKit.cs` | ~162 | mostly removed |
| `*.iOS.cs` | ~81 | UI-rendering ones removed |
| `*.wasm.cs` | ~253 | DOM-rendering ones removed; interop/bootstrap kept |
| `*.java` | 17 | all removed (`Uno.UI.BindingHelper.Android`) |
| `*.ts` | ~149 total (~34 DOM) | DOM renderer removed; bootstrap/interop kept |
| 5-variant projects | ~43 | `.netcoremobile` + `.Wasm` UI variants dropped (~½) |

---

## 4. Scope

### 4.1 In scope (removed)

- The native Android, iOS/tvOS/macCatalyst, and WASM DOM **UI rendering** backends and all bridging abstractions.
- The Java/JNI layer (`Uno.UI.BindingHelper.Android`), the TypeScript DOM renderer (`WindowManager.ts` + `WasmCSS`), and the `Uno.UI.Runtime.WebAssembly` package.
- Native control wrappers, native element hosting, native media/webview/picker rendering, native composition backing, native source generators, native safe-area/keyboard plumbing.
- The `.netcoremobile`/`.Wasm` **UI-rendering** project variants, their TFMs, AndroidX dependencies, native solution filters, native sample/test heads, and native CI stages.
- Native-only public API and `FeatureConfiguration` flags (§8).

### 4.2 Out of scope (kept)

- **Skia on every target**, including Skia-on-Android/iOS/WASM hosts and all desktop Skia hosts.
- `Uno.UWP`/`Uno.Foundation` **non-UI** platform implementations and their `netcoremobile` TFMs.
- `Uno.Foundation.Runtime.WebAssembly` (JS interop), `Uno.UI.MediaPlayer.WebAssembly` (see note below), DevServer/RemoteControl Host, `Uno.UI.Composition` Skia path.
- Parity validation of Skia-on-native (assumed done).

> **Note on `Uno.UI.MediaPlayer.WebAssembly`:** the deep-dive on media playback concludes this AddIn is **retained** — Skia-on-WASM still composes an HTML `<video>`/`<audio>` overlay for media (a browser can't practically decode video into the Skia canvas). The earlier controls-level pass listed it for removal; this spec adopts the media-specialist conclusion (**retain**) and flags it as **must-confirm** during W9.

### 4.3 Fixed decisions

| Decision | Choice |
|---|---|
| Backends removed | All three native backends |
| Sequencing | Hard removal in one major (7.0); no `[Obsolete]` interim |
| Parity | Assumed already achieved |
| Document type | Internal engineering plan |

---

## 5. Guiding principles and invariants

1. **Preserve the platform-API boundary.** Never delete a `Uno.UWP`/`Uno.Foundation` file that implements a non-UI WinRT API. When in doubt, classify a file as *UI rendering* (remove) vs *platform capability* (keep) before touching it.
2. **`#if __SKIA__` becomes the main line.** Replace `#if __ANDROID__/__APPLE_UIKIT__/__WASM__ … #else __SKIA__` chains with the Skia branch. Where the symbol still matters (Skia-on-Android vs Skia-on-Win32 differences), prefer **runtime checks** (`OperatingSystem.IsAndroid()`) or `ApiExtensibility` over compile-time native symbols, consistent with the repo's Skia-first guidance.
3. **One tree, one measure/arrange, one input pipeline.** After removal there is exactly one managed visual tree, one `Layouter` path, and one `InputManager`/`PointerManager` path. Any code branching "is this child a `UIElement` or a native `View`?" is deleted.
4. **Batch deletions per subsystem** to avoid long-lived broken intermediate states; each workstream should build green on the Skia matrix when complete.
5. **Move, don't lose, platform behavior.** Where a native file mixed rendering with a still-needed platform service (IME, pickers, WebView, safe-area), migrate the service to the relevant `Uno.UI.Runtime.Skia.*` host (via `ApiExtensibility`) *before* deleting the native file.

---

## 6. The removal, by workstream

The work decomposes into twelve workstreams. Each lists the principal artifacts removed/kept; the exhaustive enumerations live in [`inventory.md`](./inventory.md).

### W1 — Build system, TFMs, packaging, dependencies

**Remove**
- The `.netcoremobile` and `.Wasm` **UI-rendering** variants of the 5-variant projects (`Uno.UI`, `Uno.UI.Composition`, `Uno.UI.Dispatching`, `Uno.UI.FluentTheme[.v1/.v2]`, `Uno.UI.Toolkit`, `Uno.UI.XamlHost`, `Uno.UI.RemoteControl`, `Uno.UI.RuntimeTests`, native AddIns). `Uno.UI.Runtime.WebAssembly` deleted outright.
- `NetMobilePreviousAndCurrent` (8 TFMs) and `NetWasmPreviousAndCurrent` from `Directory.Build.props` for UI projects; UI rendering targets only `NetSkiaPreviousAndCurrent` (`net9.0; net10.0`).
- `Uno.UI.BindingHelper.Android` (Java compile + JAR embed + JNI bindings) and its conditional `ProjectReference`.
- `Xamarin.AndroidX.AppCompat/RecyclerView/Activity/Browser/SwipeRefreshLayout` from `Uno.UI.netcoremobile.csproj`, `Uno.netcoremobile.csproj`, `Uno.UI.BindingHelper.Android.*.csproj`, and the version pins in `Directory.Build.targets` (lines ~163–177); the unconditional Android block in `Uno.Sdk/targets/Uno.Implicit.Packages.ProjectSystem.Android.targets` (lines ~9–19).
- Native UI solution filters: `Uno.UI-netcoremobile-only.slnf`, `Uno.UI-Wasm-only.slnf`. (`Uno.UI-Windows-only.slnf` **stays** — it builds the WinAppSDK target used as the WinUI parity oracle, not a native Uno backend.)
- TypeScript compilation (`Microsoft.TypeScript.MSBuild`) and `WasmCSS` embedding from `Uno.UI.Wasm.csproj`.
- Native nuspec TFM folders/dependency groups (`lib/net*-android*`, `lib/net*-ios*`, `lib/net*-tvos*`, `lib/net*-maccatalyst*` for UI rendering) and the `uno-runtime/.../webassembly` DOM runtime; collapse to the Reference (`lib/net9.0`, `lib/net10.0`) + `uno-runtime/.../skia` layout.

**Keep**: `RuntimeAssetsSelectorTask` (still needed for Skia-on-mobile two-layer resolution, but its native-vs-Skia branch dies); the file-suffix exclusion machinery in `Uno.CrossTargetting.targets` (harmless once no native files exist — optional cleanup); `Uno.UWP`/`Uno.Foundation` `netcoremobile` TFMs.

**Simplification**: ~43 multi-variant projects → ~21; three solution filters remain (`Skia-only`, `Windows-only`, `Reference-only`); nuspec shrinks ~500→~100 `<file>` entries; no Java toolchain or TypeScript step for UI; estimated 30–50% CI/build wall-clock reduction. See §7.

### W2 — Native Android rendering layer

**Remove**: `Uno.UI.BindingHelper.Android` (17 `.java`: `UnoViewGroup`, `UnoMotionHelper`, `UnoRecyclerView`, `UnoTwoDScrollView`, `TextPaint*`, `UnoSpannableString`, `UnoStaticLayoutBuilder`, `UIElementNative`); all rendering `*.Android.cs` in `Uno.UI` (~227): `BindableView.Android.cs`, `UIElement.Android.cs`, `UIElement.Pointers.Android.cs`, `FrameworkElement.Android.cs`, `Layouter.Android.cs`/`LayouterHelper.Android.cs`, the `Bindable*` widget wrappers, `TextBlock.Android.cs`/`TextBox.Android.cs`/`TextBoxView.Android.cs`, `Image.Android.cs`/`NativeImageView.Android.cs`, `*Brush*.Android.cs` (incl. `AcrylicBrush.Rendering.Blur.Android.cs`), `Border.Android.cs`/`BorderLayerRenderer.Android.cs`, `NativeRenderTransformAdapter.Android.cs`, `FrameworkElementOutlineProvider.Android.cs`, `UIElement.ThemeShadow.Android.cs`, command-bar renderers. Fragment plumbing (`IFragmentTracker.Android.cs`, `BindableFragment.Android.cs`, `PivotItemFragment.Android.cs`) and native `NativePagedView`/ViewPager.

**Keep**: `ViewHelper.Android.cs` (DPI/MeasureSpec math — shared with Skia-on-Android); Activity/Application lifecycle (`ActivityHelper`, `BaseActivity`, `DelegateActivity`, `NativeApplication`) as platform infra; native picker/WebView/IME **services** relocated to `Uno.UI.Runtime.Skia.Android` via `ApiExtensibility`.

**Outcome**: single managed view tree; `UIElement` inherits `DependencyObject` (now a class on Android too); no JNI marshalling; HarfBuzz/SkiaSharp text replaces `StaticLayout`; managed `InputManager` replaces `MotionEvent` dispatch.

### W3 — Native Apple (iOS/tvOS/macCatalyst, UIKit) rendering layer

**Remove**: `BindableUIView.UIKit.cs`, `UIElement.UIKit.cs`, `UIElement.Pointers.UIKit.cs`, `FrameworkElement.UIKit.cs`/`.Apple.cs`; the entire native `ListViewBase/` UIKit set (`NativeListViewBase.UIKit.cs`, `ListViewBaseSource.UIKit.cs`, `VirtualizingPanelLayout.UIKit.cs`, `BindableUICollectionView.UIKit.cs`, …); native `Picker`/`DatePicker`/`TimePicker` (`*.iOS.cs`); `*.Apple.cs` CoreAnimation rendering (`BorderLayerRenderer`, `Shape`, `GradientBrush`, `RadialGradientBrush`, `AcrylicBrush`), animation drivers (`DisplayLinkValueAnimator.Apple.cs`, `GPUFloatValueAnimator.Apple.cs`); `NativeRenderTransformAdapter.Apple.cs`; native window/host (`Window.UIKit.cs`, `NativeWindow*.UIKit.cs`, `RootViewController.UIKit.cs`, `NativeFramePresenter.UIKit.cs`).

**Keep**: `Uno.UI.Runtime.Skia.AppleUIKit` (the Skia host — single `UnoSKMetalView`/Metal surface; pointer/keyboard/IME input sources; `UIKitNativeElementHostingExtension` for opt-in native embedding); all `Uno.UWP` `*.UIKit.cs`/`*.Apple.cs` **platform APIs**.

**Distinction to preserve**: "native `UIView`-per-element tree" (removed) vs "single host `UIView` hosting Skia" (kept).

### W4 — Native WebAssembly DOM rendering layer

**Remove**: `Uno.UI.Runtime.WebAssembly` project/package (`HtmlElement.cs`/`HtmlElementAttribute`, host builders, `buildTransitive`, linker defs); `ts/WindowManager.ts` (~1,500 lines) and DOM-only `.ts`; `WindowManagerInterop.wasm.cs` (~929 lines, 26 JSImports); `WasmCSS/Uno.UI.css`; `HtmlElementHelper.wasm.cs`; `BrowserHtmlElement.wasm.cs` and `ContentPresenter.wasm.cs` native-element hosting; SVG-shape `.wasm.cs` (`Rectangle/Ellipse/Polygon/Path/Line/Polyline.wasm.cs`); DOM measurement/arrange/style `.wasm.cs` across controls; DOM `HtmlWebViewElement.wasm.cs`; GCHandle/`HtmlId`/`HtmlTag` plumbing in `UIElement.wasm.cs`.

**Keep**: `Uno.Foundation.Runtime.WebAssembly` (JS interop/Mono bridges); bootstrap/interop `.ts` (`Runtime`, `Xaml`, input/focus glue that Skia-on-WASM needs); `Uno.UI.Runtime.Skia.WebAssembly.Browser` (canvas renderer); `Uno.UI.MediaPlayer.WebAssembly` (HTML media overlay — see W9).

**Outcome**: WASM rendering becomes identical to desktop Skia (canvas, immediate-mode); no per-element DOM, no CSS plumbing, no `allActiveElementsById` registry.

### W5 — Core `Uno.UI` shared abstractions

This is the heart of the contributor-facing simplification. **Remove**:
- `UIElement.Android/.UIKit.cs`, `FrameworkElement.Android/.UIKit/.Apple.cs`, `BindableView`/`BindableUIView`.
- `IShadowChildrenProvider` + `ChildrenShadow`; all children become `UIElement` and are enumerated via `GetChildren()`.
- Platform `Layouter` (`Layouter.Android/.UIKit.cs`, `LayouterHelper.Android.cs`) and native layout adapters (`ItemsStackPanelLayout.Android/.UIKit.cs`, `VirtualizingPanelLayout.Android.cs`, legacy UIKit list layouts) → one managed `Layouter`.
- Native pointer injection (`UIElement.Pointers.Android/.UIKit/.Native.cs`) and the platform partials in `InputManager` → only `UIElement.Pointers.Managed.cs` remains.
- `NativeRenderTransformAdapter.Android/.wasm.cs`; all `#if !__SKIA__` transform/opacity/clip/brush branches → Composition path only.
- `FrameworkElementAutomationPeer.Android/.UIKit.cs` native accessibility bridges → managed automation peer only (native a11y migrates to the Skia hosts, e.g. `Uno.UI.Runtime.Skia.Android/Accessibility`).
- `VisualTreeHelper.Android/.Apple.cs` and `#if __ANDROID__ || __APPLE_UIKIT__` tree-navigation branches.
- Native window/`ContentRoot` host adaptation; `IFrameworkElement`/`ILayouterElement` reduce to a managed-only contract (`ILayouterElement.Android/.Apple.cs` removed).
- Native `FeatureConfiguration` toggles (`AndroidUseManagedLoadedUnloaded`, `AlwaysClipNativeChildren`, `IMPLEMENTS_MANAGED_MEASURE_DIRTY_PATH` becomes always-on, etc.).

### W6 — Source-generator mixin infrastructure

**Remove** (`src/SourceGenerators/Uno.UI.SourceGenerators.Internal/Mixins/`):
- `FrameworkElementAndroidMixinGenerator.cs` (~1,000 lines) and `FrameworkElementUIKitMixinGenerator.cs` (~1,300 lines) — they synthesize `IFrameworkElement` onto native-`View`-based controls; unnecessary once controls inherit managed `FrameworkElement`.
- `BaseActivityCallbacksGenerator.cs` (Android Activity lifecycle → managed events).
- `MixinGeneration.targets` and the `UNO_MIXIN_GENERATION` constant; the `FrameworkElement.EffectiveViewport.cs` **template** transformation (preserve the managed EffectiveViewport logic itself).
- **Audit** `DependencyPropertyMixinGenerator.cs`: keep any properties consumed by Skia controls (move to `[GeneratedDependencyProperty]` inline); delete the generator if all emitted properties are native-only. Eliminates the `OnXChangedPartialNative` pattern.

**Outcome**: incremental generators drop from ~5 to ~2 (XAML + DependencyObject); single control inheritance chain.

### W7 — Composition native backing

**Remove** (`Uno.UI.Composition`): `Visual.Android/.UIKit.cs`, `VisualCollection.Android/.UIKit.cs`, `ContainerVisual.UIKit.cs`, `SpriteVisual.UIKit.cs`, `KeyFrameAnimations/ScalarKeyFrameAnimation.UIKit.cs`, `CoreAnimation.Apple.cs` (~337 lines), `Uno/CompositorThread.Android.cs` (~262 lines, RenderNode/HardwareRenderer), `Uno/ICompositionRoot.Android.cs`; the `CompositionConfiguration.UseCompositorThread` flag; the `NativeOwner` property and ~10 partial-method declarations on `Visual`/`VisualCollection`; the `CompositorThread.Start`/`ICompositionRoot` usage in `ApplicationActivity.Android.cs`.

**Keep**: all `.skia.cs` composition (the Skia scene graph is the only backing).

### W8 — Controls with native implementations

**Remove** native paths and consolidate to the single Skia-rendered control for: `TextBox`/`PasswordBox`/`RichEditBox` (native `EditText`/`UITextField`/`<textarea>` + IME), `ListView`/`GridView` (`NativeListViewBase`, `UnoRecyclerView`, `UICollectionView`), `MediaPlayerElement` (native presenters — see W9), `WebView`/`WebView2` (native `WebView`/`WKWebView`/`<iframe>`), `DatePicker`/`TimePicker`/`ComboBox` (native dialogs/`Spinner`/`UIPickerView`), `CommandBar`/`AppBar` (native action bars), `Frame` (`NativeFramePresenter` + native back-stack), `Popup` (`NativePopup`/`PopupWindow`), `ToggleSwitch`/`Slider`/`ProgressRing`/`ProgressBar`, `ScrollViewer` (native scroll). Remove **native element hosting in XAML** via `ContentPresenter` (Android `View`/`UIView`/DOM element as `Content`).

**Remove `FeatureConfiguration` native flags** (full list in §8.3): `Style.UseUWPDefaultStyles`/`ConfigureNativeFrameNavigation()`, `Popup.UseNativePopup`, the `NativeListViewBase` class, `NativeFramePresenter.AndroidUnloadInactivePages`, `TextBox.UseOverlayOnSkia`/`UseLegacyInputScope`, `WebView.ForceSoftwareRendering`, etc.

### W9 — Media playback

**Remove** (native backends): `Uno.UWP/Media/Playback/MediaPlayer.Android.cs` (~554), `MediaPlayer.Apple.cs` (~544), `Internal/VideoSurface.Android/.Apple.cs`, `Internal/AudioPlayerBroadcastReceiver.Android.cs`; `Uno.UI/.../MediaPlayerElement/MediaPlayerPresenter.Android/.Apple.cs`; the `#if __APPLE_UIKIT__ || __ANDROID__` `SetVideoSurface` branch in `MediaPlayerPresenter.cs`.
**Keep / mature**: `MediaPlayerPresenter.Others.cs` (the `ApiExtensibility`-based single path); `Uno.UI.Runtime.Skia.Android/...AndroidSkiaMediaPlayerPresenterExtension.cs` and `Uno.UI.Runtime.Skia.AppleUIKit/...MediaPlayerPresenterExtension.cs` become the **only** mobile media paths; `Uno.UI.MediaPlayer.Skia.Win32/.X11`; **`Uno.UI.MediaPlayer.WebAssembly` retained** (HTML media overlay for Skia-on-WASM — confirm).
> The native Skia media extensions currently delegate to the native `UpdateVideoStretch`/`UpdateVideoGravity`; they must be refactored to stand alone once the native `MediaPlayer.*` files are gone.

### W10 — Window, safe-area, insets, keyboard plumbing

**Remove** native feedback paths: `LayoutProvider.Android.cs` (dual `PopupWindow`/`ViewTreeObserver` keyboard+inset monitor), `InputPane.Android/.Apple.cs`, the `GetVisualBounds`/`GetVisibleBounds` `WindowInsets`/`DisplayCutout`/safe-area logic in `NativeWindowWrapper.Android/.UIKit.cs`, `StatusBar.Android.cs` edge-to-edge inset logic (API 35+), `RootViewController.ViewSafeAreaInsetsDidChange`, `UIKeyboard.Notifications` observers, `NativePage`/`NativeFramePresenter` `AutomaticallyAdjustsScrollViewInsets`, `FeatureConfiguration.AndroidSettings.IsEdgeToEdgeEnabled`.
**Keep**: the Skia-host equivalents (`Uno.UI.Runtime.Skia.Android/LayoutProvider.cs` + `InputPaneExtension`, `Uno.UI.Runtime.Skia.AppleUIKit/AppleUIKitWindowWrapper.cs` + `InputPaneExtension`) — these must own keyboard/inset/safe-area handling. `InsetsExtensions.Android.cs` (Insets→Thickness util) stays.
> **Highest-risk workstream.** Notch/Dynamic-Island safe areas, soft-keyboard resize, and pre-API-30 keyboard-height detection must be confirmed in the Skia hosts before the native files are deleted.

### W11 — Tests, samples, templates, tooling, CI

**Remove**: `Uno.UI.RuntimeTests.netcoremobile.csproj`, `Uno.UI.RuntimeTests.Wasm.csproj`; `SamplesApp.netcoremobile`, `SamplesApp.Wasm` (DOM) heads; `SamplesApp.Wasm.UITests` (Node/TypeScript snapshot runner); native heads in `src/SolutionTemplate/*` (`*.Mobile`, native `Main.Android/.iOS/.maccatalyst.cs`) and the `uno56droidioswasmskia` native heads; `Uno.UI.RemoteControl.netcoremobile.csproj`; the three native CI stages `wasm_tests` (~130 min), `android_tests`, `ios_tests` and `build/ci/scripts/determine-test-scope.ps1`; xharness/Xamarin.UITest device runners; native artifacts (`samplesapp-android-native`, `-ios-native`, `-wasm-native`).
**Keep**: `Uno.UI.RuntimeTests.Skia.csproj` and all `runtime_tests_skia_*`/`skia_*` stages; `SamplesApp.Skia*` heads (generic, `Skia.netcoremobile`, `Skia.WebAssembly.Browser`); `Uno.UI.RemoteControl.Skia/.Wasm`; DevServer/Host (platform-agnostic).

### W12 — Documentation

Remove/rewrite docs that describe native rendering as a supported path: `doc/articles/features/using-native-rendering.md` (xref `uno.features.renderer.native`), `doc/articles/native-views.md` (xref `Uno.Development.NativeViews`), and native sections in `how-uno-works.md`, `Uno-UI-Performance.md`, `intro.md`, `using-skia-hosting-native-controls.md`, `guides/xf-migration/control-mappings.md` & `native-controls.md`, and `toc.yml`. Update `AGENTS.md`, `.claude/rules/platform-targeting.md`, `.claude/rules/code-style.md`, and `building-uno-ui.md` to drop native-rendering targeting guidance and mark `net*-android/ios/wasm` as Skia-only. Author the consumer **migration guide** referenced by §8.

---

## 7. Codebase simplifications for new contributors

These are the durable wins that make Uno meaningfully easier to learn and contribute to. Each maps to one or more workstreams.

1. **One `UIElement`, one tree.** `UIElement` becomes a plain managed object owning a `Composition.Visual` on *every* platform. No `BindableView`/`BindableUIView`, no `UnoViewGroup`, no DOM element, no `IShadowChildrenProvider`, no "is this child a `UIElement` or a native `View`?" branching. A newcomer learns *one* mental model. (W2, W3, W4, W5)
2. **`DependencyObject` is always a class.** The mobile-only "`DependencyObject` is an interface implemented by a source generator" special case disappears, removing a long-standing source of confusion in the generators and in reflection-facing code. (W5, W6)
3. **One measure/arrange.** `MeasureOverride`/`ArrangeOverride` compute layout directly in C# — no Android `MeasureSpec` packing, no `View.measure()/layout()` reflection, no iOS `SizeThatFits`, no DOM `offsetWidth` read-back, no native measure-cache invalidation. Layout is debuggable with plain breakpoints. (W5)
4. **One input pipeline.** Pointer/keyboard always flow through the managed `InputManager`/`PointerManager`. No `MotionEvent`/`UnoMotionHelper`, no `TouchesBegan`, no DOM event extractors, no Android "implicit capture" divergence. (W5)
5. **One control implementation per control.** `TextBox`, `ListView`, `ScrollViewer`, etc. have a single Skia code path instead of `.Android.cs`/`.UIKit.cs`/`.wasm.cs`/`.skia.cs` quadruplets — far less to read, far fewer "did I update all four?" mistakes. (W8)
6. **Pure-C# `Uno.UI` (no Java, no TypeScript).** The Android Java/JNI toolchain and the WASM TypeScript DOM renderer leave the UI layer entirely. Contributors no longer need a Java SDK or a TS build to work on `Uno.UI`. (W1, W2, W4)
7. **Two source generators instead of five.** Removing the three native mixin generators (and the `EffectiveViewport` template transform and `OnXChangedPartialNative` pattern) makes the generator pipeline simpler, faster, and far less surprising. (W6)
8. **Composition is a single Skia scene graph.** No `CALayer` synchronization, no Android `CompositorThread`/`RenderNode`, no `CoreAnimation` marshalling — one tree to inspect, no managed/native desync class of bugs. (W7)
9. **A smaller, single-shape build.** ~43 multi-variant projects → ~21; five solution filters → three (drop `netcoremobile-only`/`Wasm-only`, keep `Skia-only`/`Windows-only`/`Reference-only`); the TFM matrix for UI collapses to `net9.0;net10.0`; nuspec and dependency graph shrink (no `Xamarin.AndroidX.*`). New contributors load `Uno.UI-Skia-only.slnf` and go. (W1)
10. **Drastically simpler CI story.** Three device-test stages, xharness, emulator/simulator provisioning, the Node WASM snapshot runner, and `determine-test-scope.ps1` all disappear; every PR runs the same Skia suite. Faster feedback, cheaper CI, nothing to learn about device farms. (W11)
11. **Fewer runtime knobs.** ~40 native-only `FeatureConfiguration` members removed — less surface to document, fewer "why is this Android-only?" questions, behavior consistent with desktop Skia. (W5, W8)
12. **The `#if` forest thins out.** Thousands of `#if __ANDROID__/__IOS__/__WASM__` blocks collapse to the Skia line; remaining platform differences are expressed as runtime checks or `ApiExtensibility`, which read like ordinary code. (all)

---

## 8. Breaking changes

All breaking changes ship in **7.0** with **no deprecation interim**. The exhaustive per-symbol list (with line references) is in [`inventory.md`](./inventory.md); this section is the categorized catalogue with migration guidance.

### 8.1 Distribution & packaging

| Change | Impact | Migration |
|---|---|---|
| **`Uno.WinUI.Runtime.WebAssembly` package deleted** | WASM apps using DOM rendering can't restore it. | Use `Uno.WinUI.Runtime.Skia.WebAssembly.Browser`. UX renders to canvas; no DOM tree. |
| **`Uno.UI.BindingHelper.Android` assembly removed** | Code referencing `UnoViewGroup`/`UnoRecyclerView`/JNI bindings breaks. | Remove the reference; Skia-on-Android needs no JNI binding. |
| **`Uno.UI.Maps` AddIn removed** | Native Google Maps control gone (no managed/Skia equivalent in core). | Use a third-party/Skia map or custom rendering. |
| **`Uno.WinUI` UI assemblies for `net*-android/ios/tvos/maccatalyst` are now Skia** | Same TFM string, **binary-incompatible** with previously-native-built consumers. | Recompile all libraries against 7.0; remove native bootstrap. |
| **`Xamarin.AndroidX.*` transitive deps removed** (AppCompat, RecyclerView, Activity, Browser, SwipeRefreshLayout) | Apps relying on these *via Uno* lose them transitively. | Add explicit `PackageReference`s if your own code uses AndroidX. |
| **`Uno.Implicit.Packages.ProjectSystem.Android.targets` gutted/deleted** | The implicit AndroidX injection for Android heads is gone. | None for normal use; custom toolchains importing it must update. Verify the Google Play/AndroidTV/Auto/Wear feature blocks in that file are preserved/relocated. |

### 8.2 Public API removed

- **Native base classes & identity**: `BindableView` (+ `BindableButton/CheckBox/ProgressBar/RadioButton/SwitchCompat/ToggleButton/SeekBar/DrawerLayout/ListView/GridView/Fragment`), `BindableUIView` (+ `BindableUI*`); `UIElement` no longer inherits a native `View`/`UIView`/DOM element. *Migration*: remove casts to `Android.Views.View`/`UIKit.UIView`; use `UIElement.Visual` (Composition) and platform APIs via `Uno.UI.Runtime.Skia.*`/`Uno.Foundation`.
- **Native element hosting**: `Uno.UI.NativeElementHosting.BrowserHtmlElement` (all methods), `Uno.UI.Runtime.WebAssembly.HtmlElementAttribute`, `ContentPresenter` hosting of native `View`/`UIView`/DOM `Content`. *Migration*: use `WebView2` for HTML; redesign with Uno controls; on iOS, opt-in native embedding remains via `UIKitNativeElementHostingExtension` (overlay-composited, reduced perf).
- **Native control/host types**: `NativeListViewBase`, `NativePagedView`, `NativeScrollContentPresenter`, `NativeRefreshControl`, `NativeFramePresenter`, `NativePopup`/`NativePopupBase`, `RootViewController` (UIKit), `Window : UIWindow` identity, `NativeRenderTransformAdapter`, `IShadowChildrenProvider`, `CompositorThread`, `Uno.UI.Composition.ICompositionRoot`, native `Brush.*`/`GradientBrush.*`. *Migration*: use the WinUI control (`ListView`/`Frame`/`Popup`/…) and Composition brushes; all render via Skia.
- **Extension/interop surface**: platform-conditional members of `UIElement`/`UIElementExtensions` (e.g. native `GetVisualTreeParent`), `Uno.UI.Toolkit.UIElementExtensions` native overloads, `IFrameworkElement.Measure/Arrange` native-`View` overloads, `ILayouterElement` native partials, `OnXChangedPartialNative`. *Migration*: use cross-platform `VisualParent`/`VisualTreeHelper` and standard measure/arrange.
- **Source generators** (internal): `FrameworkElementAndroidMixinGenerator`, `FrameworkElementUIKitMixinGenerator`, `BaseActivityCallbacksGenerator`, possibly `DependencyPropertyMixinGenerator`. *Migration*: none for app authors (internal).

### 8.3 `FeatureConfiguration` flags removed

Android-only: `ComboBox.AllowPopupUnderTranslucentStatusBar`, `FrameworkElement.AndroidUseManagedLoadedUnloaded`, `FrameworkElement.InvalidateNativeCacheOnRemeasure`, `Popup.UseNativePopup`, `NativeListViewBase` (class: `RemoveItemAnimator`, `ForceRecycleOnDrop`, `UseNativeSnapHelper`), `NativeFramePresenter.AndroidUnloadInactivePages`, `TextBox.UseLegacyInputScope`, `UIElement.AlwaysClipNativeChildren`, `UIElement.UseLegacyClipping`, `ScrollViewer.AndroidScrollbarFadeDelay`, `WebView.ForceSoftwareRendering`, `PointerRoutedEventArgs.AllowRelativeTimeStamp`, `TextBlock.IsJavaStringCachedEnabled`/`JavaStringCachedCapacity`, `AndroidSettings.IsEdgeToEdgeEnabled`.
iOS-only: `Image.LegacyIosAlignment`, `FrameworkElement.IOsAllowSuperviewNeedsLayoutWhileInLayoutSubViews`.
WASM-only: `Interop.ForceJavascriptInterop`, `UIElement.AssignDOMXamlName`, `UIElement.AssignDOMXamlProperties`, `TextBlock.IsMeasureCacheEnabled`.
Native (Android+iOS): `ListViewBase.AnimateScrollIntoView`.
Skia overlay: `TextBox.UseOverlayOnSkia`; `Style.UseUWPDefaultStyles` + `Style.ConfigureNativeFrameNavigation()`.
*Migration*: delete the calls; behavior is the unified Skia/WinUI behavior. Guard with `#if` only if a single codebase must support both pre-7.0 and 7.0.

### 8.4 Behavioral changes (same API, different result)

- **Rendering parity**: gradients, corner radii, shadows/elevation, anti-aliasing now render via Skia rather than `Canvas`/`CALayer`/CSS — subtle pixel differences possible; re-baseline visual tests.
- **Text**: measurement/line-breaking/bidi/kerning/`MaxLines` via HarfBuzz+SkiaSharp rather than `StaticLayout`/`UITextKit`/DOM — possible layout shifts in text-dense UIs.
- **Lists/scroll**: managed virtualization & scrolling replace `RecyclerView`/`UICollectionView`/`UIScrollView`/CSS overflow — fling curves, edge glows, snap points may differ.
- **IME/keyboard**: canvas-rendered text input with a platform IME bridge replaces native `EditText`/`UITextField`; CJK/emoji composition and selection visuals should be re-tested.
- **Pickers / command bars / frame**: Skia/WinUI-styled rather than native-styled; appearance changes on Android/iOS.
- **Composition animation timing**: Skia interpolation replaces `CABasicAnimation`; standard easing should match, exotic timing may not.

### 8.5 Tooling, templates, project heads

- **`<UnoFeatures>skiarenderer</UnoFeatures>` becomes implicit/mandatory** for `android`/`ios`/`tvos`/`maccatalyst`. Projects that relied on native rendering by *omitting* `skiarenderer` must be regenerated/updated.
- **Project templates** drop native `*.Mobile`/`*.Wasm` (DOM) heads; new apps get Skia heads only (`Skia.netcoremobile`, `Skia.WebAssembly.Browser`, desktop Skia). Existing apps must update `.csproj` conditionals and bootstrap.
- **CI**: native test stages and `determine-test-scope.ps1` removed; the platform-suffix scope filters no longer gate native stages.

---

## 9. Execution plan & sequencing

The removal is large but highly parallelizable *within* a workstream and ordered *across* workstreams by hard dependencies. Recommended phases (each ends green on the Skia build matrix):

**Phase 0 — Preconditions (gate).** Confirm Skia-on-Android/iOS/WASM parity (assumed) and that the Skia hosts own IME, pickers, WebView, media, safe-area/keyboard, and accessibility (W9/W10 services exist before their native counterparts are deleted). Snapshot current Skia visual baselines.

**Phase 1 — Leaf subsystems (parallel).** W7 (Composition native backing), W9 (media native backends), W6 (mixin generators *after* the native control files they target are scheduled for deletion). These have contained blast radius.

**Phase 2 — Core identity (sequential within, big-bang per platform).**
1. Consolidate `DependencyObject` to class-based inheritance (generator change) — **prereq** for deleting `UIElement.Android.cs`.
2. Delete `BindableView`/`BindableUIView` and re-root `UIElement` on `DependencyObject`/Composition (W5).
3. Remove `IShadowChildrenProvider`; unify `GetChildren()`.
4. Collapse `Layouter` to the managed path; delete native layout adapters.
5. Unify pointer/input (`InputManager`), transforms/opacity/clip/brush (Composition), accessibility, visual-tree navigation.
   *Delete the ~350 core `*.Android.cs/.UIKit.cs/.Apple.cs` partials in a tight batch to avoid broken intermediate builds.*

**Phase 3 — Controls (W8) & native services relocation (W2/W3/W10).** Delete native control wrappers; relocate still-needed platform services (IME, pickers, WebView, safe-area, keyboard, native a11y) into the Skia hosts; remove native `FeatureConfiguration` flags (do this *before/with* the control deletions to avoid dangling references).

**Phase 4 — WASM DOM (W4).** Remove `WindowManager.ts`/`WindowManagerInterop.wasm.cs`/`WasmCSS`/`BrowserHtmlElement`/`HtmlElementAttribute`/SVG-shape `.wasm.cs`; delete `Uno.UI.Runtime.WebAssembly`; keep `Uno.Foundation.Runtime.WebAssembly` + bootstrap TS.

**Phase 5 — Build, packaging, deps (W1).** Drop `.netcoremobile`/`.Wasm` UI variants, native TFMs, AndroidX deps, solution filters, nuspec native groups, TypeScript build. (Late, after all referenced files are gone.)

**Phase 6 — Tests/samples/templates/CI (W11) & docs (W12).** Remove native test/sample/template heads and CI stages; update templates to Skia-only; rewrite docs; publish the migration guide. (Coordinate CI changes *with* project removal to avoid red builds.)

**Cross-cutting ordering rules**
- A native file that also hosts a kept platform service is deleted **only after** the service lands in the Skia host.
- nuspec/solution-filter/CI edits come **after** the projects/files they reference are gone.
- `MixinGeneration`/generators are removed **after** the native control files they generate for are deleted.
- The `RuntimeAssetsSelectorTask` native branch is simplified **last** (it still serves Skia-on-mobile two-layer resolution).

---

## 10. Validation strategy

| Gate | Evidence required |
|---|---|
| **Compile** | `Uno.UI-Skia-only.slnf` and `Uno.UI-Reference-Only.slnf` build clean for `net9.0` and `net10.0`; `Uno.UWP`/`Uno.Foundation` still build all kept `netcoremobile` TFMs. |
| **Unit** | `dotnet test Uno.UI/Uno.UI.Tests.csproj` (note: targets `net9.0`). |
| **Runtime (Skia)** | `Uno.UI.RuntimeTests.Skia` green on Win32, Linux/X11, **Skia-on-Android**, **Skia-on-iOS**, **Skia-on-WASM** — explicitly exercising the device targets that previously used native rendering. |
| **Visual regression** | Re-baseline Skia screenshots; diff against Phase-0 snapshots; manual sweep on notched Android/iPhone + Dynamic Island for W10. |
| **Packaging** | Restore the produced `Uno.WinUI` into a throwaway `net10.0-android`/`-ios`/`-browserwasm` app; assert no `Xamarin.AndroidX.*`/DOM-runtime packages flow and the app renders via Skia. |
| **Templates** | `packages_tests`-equivalent: every shipped template generates, restores, and builds with Skia-only heads. |

Report every result with an explicit **Compile / Unit / Runtime** label per the repo's validation-evidence rule; never present compile-only as runtime-validated.

---

## 11. Risks & mitigations (consolidated)

| Risk | Severity | Mitigation |
|---|---|---|
| Hidden parity gaps surface as regressions (text metrics, fling, snap points, IME, a11y, safe-area) | High | Phase-0 gate; device runtime tests; W10 manual device sweep; treat any gap as a release blocker. |
| Native file deleted before its kept platform service is relocated | High | Enforce the "service-before-deletion" ordering (W2/W3/W9/W10); checklist in `inventory.md`. |
| Broken intermediate builds from partial core deletion | Medium | Batch the ~350 core partials per platform into tight PRs; keep Skia matrix green between PRs. |
| Consumer binary incompatibility (same TFM, Skia binaries) | High (by design) | Clear 7.0 migration guide; release notes; recompile-everything guidance. |
| `DependencyPropertyMixinGenerator` removal drops a Skia-used property | Medium | Audit every emitted property; migrate Skia-relevant ones to `[GeneratedDependencyProperty]` before deleting. |
| `Uno.UI.MediaPlayer.WebAssembly` wrongly removed | Medium | Confirm Skia-on-WASM media overlay dependency in W9 before any deletion. |
| AndroidX implicit-packages file also carries Play/TV/Auto/Wear features | Medium | Preserve/relocate those feature blocks when gutting `Uno.Implicit.Packages.ProjectSystem.Android.targets`. |
| nuspec generator hardcodes native TFM groups | Medium | Update the generation script alongside nuspec edits; verify the produced `.nupkg`. |
| Third-party libs subclass `BindableView`/`UIView` or use native-only APIs | Medium | Document in migration guide; no shim (hard removal). |
| Docs/rules drift, confusing contributors | Low | W12 updates `AGENTS.md`, `.claude/rules/*`, and `building-uno-ui.md` in the same release. |

---

## 12. Open questions / assumptions to confirm

1. **`Uno.UI.MediaPlayer.WebAssembly`**: retained as the Skia-on-WASM media overlay? (Spec assumes **yes**.)
2. **tvOS/macCatalyst**: fully covered by `Uno.UI.Runtime.Skia.AppleUIKit` as Skia hosts? (Assumed yes.)
3. **Native element hosting on iOS** (`UIKitNativeElementHostingExtension`): kept as an opt-in overlay path, or also removed? (Spec assumes **kept** — it's a Skia-host capability, not native rendering.)
4. **`DependencyObject`-as-interface removal**: scope of generator + reflection changes — confirm no public contract depends on the interface shape.
5. **`Uno.UI.XamlHost` / WPF islands**: any native-rendering coupling beyond Skia hosting to untangle?
6. **Pre-API-30 Android keyboard-height** detection in the Skia host (W10) — confirmed or formally dropped?

---

## 13. Appendices

- [`inventory.md`](./inventory.md) — exhaustive removable-project list, per-area file inventory, the full `FeatureConfiguration` removal table with line references, the CI stage/artifact list, and the documentation-update checklist.

*This spec is grounded in a 15-agent codebase analysis of the native rendering surface (`Uno.UI`, `Uno.UWP`, `Uno.Foundation`, `Uno.UI.Composition`, source generators, build system, tests/CI). File paths and counts were verified against the working tree on `dev/mazi/drop-native` and should be re-checked at implementation time as the tree evolves.*

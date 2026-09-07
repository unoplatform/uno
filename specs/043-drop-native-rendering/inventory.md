# Appendix: Removal Inventory — Drop Native Rendering (Spec 043)

Companion to [`spec.md`](./spec.md). Exhaustive enumerations kept out of the main document for readability. **All paths/line numbers are point-in-time** against `dev/mazi/drop-native` and must be re-verified at implementation; treat them as a map, not a guarantee.

---

## A. Removable projects

### Fully deleted
```
src/Uno.UI.BindingHelper.Android/Uno.UI.BindingHelper.Android.netcoremobile.csproj   # Java/JNI binding layer
src/Uno.UI.Runtime.WebAssembly/Uno.UI.Runtime.WebAssembly.csproj                     # native WASM DOM runtime (Uno.WinUI.Runtime.WebAssembly)
src/AddIns/Uno.UI.Maps/*                                                              # native Google Maps (no core Skia equivalent)
```

### `.netcoremobile` + `.Wasm` UI-rendering variants dropped (generic Skia variant kept)
```
src/Uno.UI/Uno.UI.netcoremobile.csproj                 src/Uno.UI/Uno.UI.Wasm.csproj
src/Uno.UI.Composition/*.netcoremobile.csproj          src/Uno.UI.Composition/*.Wasm.csproj
src/Uno.UI.Dispatching/*.netcoremobile.csproj          src/Uno.UI.Dispatching/*.Wasm.csproj
src/Uno.UI.FluentTheme/*.netcoremobile.csproj          src/Uno.UI.FluentTheme/*.Wasm.csproj
src/Uno.UI.FluentTheme.v1/*.netcoremobile.csproj       src/Uno.UI.FluentTheme.v1/*.Wasm.csproj
src/Uno.UI.FluentTheme.v2/*.netcoremobile.csproj       src/Uno.UI.FluentTheme.v2/*.Wasm.csproj
src/Uno.UI.Toolkit/*.netcoremobile.csproj              src/Uno.UI.Toolkit/*.Wasm.csproj
src/Uno.UI.XamlHost/*.netcoremobile.csproj             src/Uno.UI.XamlHost/*.Wasm.csproj
src/Uno.UI.RemoteControl/*.netcoremobile.csproj        src/Uno.UI.RemoteControl/*.Wasm.csproj
src/Uno.UI.RuntimeTests/Uno.UI.RuntimeTests.netcoremobile.csproj
src/Uno.UI.RuntimeTests/Uno.UI.RuntimeTests.Wasm.csproj
src/AddIns/Uno.UI.Lottie/*.netcoremobile.csproj  + .Wasm.csproj
src/AddIns/Uno.UI.MSAL/*.netcoremobile.csproj    + .Wasm.csproj
src/AddIns/Uno.UI.Svg/*.netcoremobile.csproj
src/AddIns/Uno.UI.GooglePlay/*.netcoremobile.csproj
src/AddIns/Uno.UI.Foldable/*.netcoremobile.csproj
src/SamplesApp/SamplesApp.netcoremobile/*           src/SamplesApp/SamplesApp.Wasm/*
```

### Kept (do **not** delete)
```
src/Uno.UWP/Uno.netcoremobile.csproj                    # platform WinRT APIs (sensors, pickers, …)
src/Uno.Foundation/Uno.Foundation.netcoremobile.csproj  # foundation APIs
src/Uno.Foundation.Runtime.WebAssembly/*                # JS interop / Mono bridges (Skia-on-WASM needs it)
src/Uno.UI.Runtime.Skia.* (Android, AppleUIKit, WebAssembly.Browser, Win32, Wpf, X11, MacOS, Linux.FrameBuffer, Tizen, Gtk)
src/AddIns/Uno.UI.MediaPlayer.WebAssembly/*             # HTML media overlay (CONFIRM in W9)
src/AddIns/Uno.UI.MediaPlayer.Skia.Win32, .Skia.X11; src/AddIns/Uno.UI.WebView.Skia.X11
src/Uno.UI.RuntimeTests/Uno.UI.RuntimeTests.Skia.csproj
```

### Solution filters deleted
```
src/Uno.UI-netcoremobile-only.slnf
src/Uno.UI-Wasm-only.slnf
```
Kept: `Uno.UI-Skia-only.slnf` (primary), `Uno.UI-Reference-Only.slnf`, and `Uno.UI-Windows-only.slnf` (WinAppSDK parity oracle for `/winui-runtime-tests` — **not** a native Uno backend).

---

## B. Per-area file inventory (representative; see §3.3 magnitudes)

### B1. Android native (`Uno.UI`, ~227 `*.Android.cs` + 17 `*.java`)
- **Java (all):** `UnoViewGroup.java`, `UnoMotionHelper.java`, `UnoRecyclerView.java`, `UnoTwoDScrollView.java`, `UIElementNative.java`, `TextPaintPoolNative.java`, `TextPaintSpan.java`, `UnoSpannableString.java`, `UnoStaticLayoutBuilder.java`, `UnoWebViewHandlerJavascriptInterface`, …
- **Identity/tree/layout:** `Controls/BindableView.Android.cs`, `UI/Xaml/UIElement.Android.cs`, `UI/Xaml/FrameworkElement.Android.cs`, `UI/Xaml/Controls/Layouter/Layouter.Android.cs`, `LayouterHelper.Android.cs`, `ILayouterElement.Android.cs`, `Panel.Android.cs`, `Canvas.Android.cs`.
- **Bindable widgets:** `BindableButton/CheckBox/ProgressBar/RadioButton/SwitchCompat/ToggleButton/SeekBar/DrawerLayout/ListView/GridView/Fragment.Android.cs`.
- **Text/input:** `TextBlock.Android.cs`, `TextBox.Android.cs`, `TextBoxView.Android.cs`, `InputScopeHelper.Android.cs`, `InputTypesExtensions.Android.cs`, `UIElement.Pointers.Android.cs`.
- **Drawing/transform/shadow:** `Image.Android.cs`, `NativeImageView.Android.cs`, `Border.Android.cs`, `BorderLayerRenderer.Android.cs`, `*Brush*.Android.cs` (`Brush`, `SolidColorBrush`, `LinearGradientBrush`, `RadialGradientBrush`, `ImageBrush`, `GradientBrush`, `AcrylicBrush`, `AcrylicBrush.Rendering.Android.cs`, `AcrylicBrush.Rendering.Blur.Android.cs`, `RevealBrush`, `XamlCompositionBrushBase`), `NativeRenderTransformAdapter.Android.cs`, `FrameworkElementOutlineProvider.Android.cs`, `UIElement.ThemeShadow.Android.cs`.
- **Virtualization/fragments:** `NativeListViewBase.Android.cs`, `ListViewBase.Android.cs`, `IFragmentTracker.Android.cs`, `Pivot/PivotItemFragment.Android.cs`, `NativePagedView` (FlipView ViewPager), `SpinnerEx.Android.cs`, `SpinnerSecondayViewPool.Android.cs`.
- **Nav/command/popup:** `NativeFramePresenter.Android.cs`, `NativeCommandBarPresenter.Android.cs`, `CommandBarRenderer.Android.cs`, `AppBarButtonRenderer.Android.cs`, `NativePopup.Android.cs`, `ToggleSwitch.Android.cs`, `ScrollViewer.Android.cs`.
- **A11y/visual tree:** `FrameworkElementAutomationPeer.Android.cs`, `AutomationPeer.Android.cs`, `VisualTreeHelper.Android.cs`.
- **KEEP:** `Extensions/ViewHelper.Android.cs`, `ActivityHelper.Android.cs`, `BaseActivity.cs` core, `DelegateActivity.Android.cs`, `NativeApplication.cs`, `Extensions/InsetsExtensions.Android.cs`.

### B2. Apple UIKit native (`Uno.UI`, ~162 `*.UIKit.cs` + ~81 `*.iOS.cs` + `*.Apple.cs`)
- **Identity/tree/layout:** `Controls/BindableUIView.UIKit.cs`, `UI/Xaml/UIElement.UIKit.cs`, `UIElement.Pointers.UIKit.cs`, `FrameworkElement.UIKit.cs`, `FrameworkElement.Apple.cs`, `Layouter.UIKit.cs`, `ILayouterElement.Apple.cs`.
- **Lists/pickers (60+):** entire native `UI/Xaml/Controls/ListViewBase/*.UIKit.cs` set; `Picker.iOS.cs`, `PickerModel.iOS.cs`, `DatePickerSelector.iOS.cs`, `NativeDatePickerFlyout.iOS.cs`, `TimePicker/*.iOS.cs`.
- **Rendering/animation (`*.Apple.cs`):** `BorderLayerRenderer`, `Shape`, `GradientBrush`, `RadialGradientBrush`, `AcrylicBrush`, `FillRuleExtensions`, `Animators/DisplayLinkValueAnimator`, `GPUFloatValueAnimator`, `FloatValueAnimator`, `NativeRenderTransformAdapter`, `Matrix3x2Extensions`, `NSBezierPathExtensions`.
- **Window/host/nav:** `Window.UIKit.cs`, `NativeWindow.UIKit.cs`, `NativeWindowFactory.UIKit.cs`, `NativeWindowWrapper.UIKit.cs`, `RootViewController.UIKit.cs`, `NativeFramePresenter.UIKit.cs`, `NativeFramePresenterUIGestureRecognizerDelegate.cs`, `NativePage.UIKit.cs`, `NativeFlipView.UIKit.cs`, `NativeScrollContentPresenter.UIKit.cs`, `CommandBar/*UIKit.cs`.
- **TextBox (partial):** `TextBox/*.Apple.cs`, `SinglelineTextBoxView.UIKit.cs`, `MultilineTextBoxView.UIKit.cs` — native `UITextInput` removed; Skia text + IME overlay (in `Skia.AppleUIKit`) kept.
- **KEEP:** `Uno.UI.Runtime.Skia.AppleUIKit/*`; all `Uno.UWP/*.UIKit.cs`/`*.Apple.cs` platform APIs.

### B3. WASM DOM native (`Uno.UI`, ~253 `*.wasm.cs` + ~34 DOM `*.ts`)
- **Runtime/interop:** `Uno.UI.Runtime.WebAssembly/*` (incl. `HtmlElement.cs`, host builders, `buildTransitive/*`, `LinkerDefinition.xml`); `UI/Xaml/Window/WindowManagerInterop.wasm.cs` (~929 lines); `ts/WindowManager.ts` (~1,500), DOM-only `ts/*`; `WasmCSS/Uno.UI.css` (~299); `UI/Xaml/HtmlElementHelper.wasm.cs`; `LinkerDefinition.Wasm.xml` DOM rules.
- **Element hosting:** `NativeElementHosting/BrowserHtmlElement.cs` + `.wasm.cs` + `.skia.cs` (keep `.reference.cs` stub only if API shape retained), `Controls/ContentPresenter/ContentPresenter.wasm.cs`, `WebView/Native/Wasm/HtmlWebViewElement.wasm.cs`.
- **Shapes/brushes/controls:** `Shapes/{Rectangle,Ellipse,Polygon,Path,Line,Polyline}.wasm.cs`; `Media/{ImageBrush,GeometryData,GeometryGroup}.wasm.cs`; `Image.wasm.cs`, `TextBlock.wasm.cs`, `TextBox.wasm.cs`, `TextBoxView.wasm.cs`, `Control.wasm.cs`, `ScrollContentPresenter.wasm.cs`, `ScrollViewer.wasm.cs`, `Border/BorderLayerRenderer.wasm.cs`, `NativeRenderTransformAdapter.wasm.cs`.
- **`UIElement.wasm.cs`:** strip GCHandle/`HtmlId`/`HtmlTag`/`HtmlTagIsSvg` and `CreateContent` constructor logic.
- **KEEP:** `Uno.Foundation.Runtime.WebAssembly/*`; bootstrap/interop `ts/Runtime*`, `ts/Xaml*`, focus/input glue used by Skia-on-WASM; `Uno.UI.Runtime.Skia.WebAssembly.Browser/*`.

### B4. Source generators (`src/SourceGenerators/Uno.UI.SourceGenerators.Internal/Mixins/`)
```
FrameworkElementAndroidMixinGenerator.cs   (~1000 lines)   DELETE
FrameworkElementUIKitMixinGenerator.cs     (~1300 lines)   DELETE
BaseActivityCallbacksGenerator.cs          (~300 lines)    DELETE
DependencyPropertyMixinGenerator.cs        (~150 lines)    AUDIT → delete or migrate Skia-used props
src/Uno.UI/MixinGeneration.targets                          DELETE (+ remove <Import> from Uno.UI.netcoremobile.csproj)
src/Uno.UI/UI/Xaml/FrameworkElement.EffectiveViewport.cs    DELETE template; KEEP managed EffectiveViewport logic
tests: Uno.UI.SourceGenerators.Tests/MixinGeneratorTests/Given_MixinGenerators.cs   prune native cases
```

### B5. Composition native backing (`src/Uno.UI.Composition/Composition/`)
```
Visual.Android.cs (14)          Visual.UIKit.cs (93)
VisualCollection.Android.cs (32) VisualCollection.UIKit.cs (42)
ContainerVisual.UIKit.cs (8)    SpriteVisual.UIKit.cs (86)
KeyFrameAnimations/ScalarKeyFrameAnimation.UIKit.cs (38)
CoreAnimation.Apple.cs (337)
Uno/CompositorThread.Android.cs (262)   Uno/ICompositionRoot.Android.cs (13)
Visual.cs            → remove NativeOwner + ~10 partial decls
VisualCollection.cs  → remove 6 insert/remove partial decls
Uno/CompositionConfiguration.cs → remove UseCompositorThread flag/option
src/Uno.UI/UI/Xaml/ApplicationActivity.Android.cs → drop ICompositionRoot impl + CompositorThread.Start
```

### B6. Media (`Uno.UWP/Media/Playback/`, `Uno.UI/.../MediaPlayerElement/`)
```
DELETE: MediaPlayer.Android.cs (554), MediaPlayer.Apple.cs (544),
        Internal/VideoSurface.Android.cs, Internal/VideoSurface.Apple.cs,
        Internal/AudioPlayerBroadcastReceiver.Android.cs,
        MediaPlayerPresenter.Android.cs (55), MediaPlayerPresenter.Apple.cs (57)
EDIT:   MediaPlayerPresenter.cs → drop #if __APPLE_UIKIT__||__ANDROID__ SetVideoSurface branch
KEEP:   MediaPlayerPresenter.Others.cs (ApiExtensibility path), MediaPlayer.cs skeleton,
        Skia.Android AndroidSkiaMediaPlayerPresenterExtension.cs (mature → standalone),
        Skia.AppleUIKit MediaPlayerPresenterExtension.cs (mature → standalone),
        AddIns/Uno.UI.MediaPlayer.WebAssembly (CONFIRM)
```

### B7. Safe-area / insets / keyboard (W10)
```
DELETE: Uno.UI/LayoutProvider.Android.cs, UI/ViewManagement/InputPane/InputPane.Android.cs + .Apple.cs
EDIT:   NativeWindowWrapper.Android.cs (drop GetVisualBounds WindowInsets/DisplayCutout ~116–195),
        NativeWindowWrapper.UIKit.cs (drop GetVisibleBounds ~114–143 + keyboard ~195–231),
        Uno.UWP/UI/ViewManagement/StatusBar/StatusBar.Android.cs (drop edge-to-edge ~141–176),
        RootViewController.UIKit.cs (drop ViewSafeAreaInsetsDidChange ~81–85),
        NativePage.UIKit.cs / NativeFramePresenter.UIKit.cs (drop AutomaticallyAdjustsScrollViewInsets),
        FeatureConfiguration.AndroidSettings.IsEdgeToEdgeEnabled + EdgeToEdge.Enable() in ApplicationActivity.Android.cs
KEEP:   Skia.Android/UI/Xaml/LayoutProvider.cs + InputPaneExtension.cs,
        Skia.AppleUIKit/AppleUIKitWindowWrapper.cs + InputPaneExtension.cs,
        Uno.UI/Extensions/InsetsExtensions.Android.cs
```

---

## C. `FeatureConfiguration` removal table (`src/Uno.UI/FeatureConfiguration.cs`)

| Member | Platform | ~Lines |
|---|---|---|
| `ComboBox.AllowPopupUnderTranslucentStatusBar` | Android | 96–102 |
| `FrameworkElement.AndroidUseManagedLoadedUnloaded` | Android | 255–266 |
| `FrameworkElement.InvalidateNativeCacheOnRemeasure` | Android | 268–273 |
| `FrameworkElement.IOsAllowSuperviewNeedsLayoutWhileInLayoutSubViews` | iOS | 288–295 |
| `Image.LegacyIosAlignment` | iOS | 319–322 |
| `Interop.ForceJavascriptInterop` | WASM | 337–345 |
| `Popup.UseNativePopup` | Android | 366–371 |
| `ListViewBase.AnimateScrollIntoView` | Android+iOS | 413–423 |
| `NativeListViewBase` (class: `RemoveItemAnimator`, `ForceRecycleOnDrop`, `UseNativeSnapHelper`) | Android | 427–449 |
| `PointerRoutedEventArgs.AllowRelativeTimeStamp` | Android | 473–485 |
| `TextBlock.IsMeasureCacheEnabled` | WASM | 566–568 |
| `TextBlock.IsJavaStringCachedEnabled` / `JavaStringCachedCapacity` | Android | 571–580 |
| `TextBox.UseLegacyInputScope` | Android | 626–636 |
| `ScrollViewer.AndroidScrollbarFadeDelay` | Android | 662–669 |
| `NativeFramePresenter.AndroidUnloadInactivePages` | Android | 699–705 |
| `UIElement.UseLegacyClipping` | Android | 722–728 |
| `UIElement.AssignDOMXamlName` | WASM | 739–743 |
| `UIElement.AssignDOMXamlProperties` | WASM | 750–760 |
| `UIElement.AlwaysClipNativeChildren` | Android | 762–771 |
| `WebView.ForceSoftwareRendering` | Android | 797–806 |
| `Style.UseUWPDefaultStyles` / `Style.ConfigureNativeFrameNavigation()` | native | — |
| `TextBox.UseOverlayOnSkia` | Skia overlay | — |
| `AndroidSettings.IsEdgeToEdgeEnabled` | Android | — |

> Remove the call sites too; many gate native control behavior that no longer exists. Line numbers are approximate — grep the member name.

---

## D. Dependencies / packaging

```
Xamarin.AndroidX.AppCompat, .RecyclerView, .Activity, .Browser, .SwipeRefreshLayout
  - src/Uno.UI/Uno.UI.netcoremobile.csproj                      (~62–67)
  - src/Uno.UWP/Uno.netcoremobile.csproj                        (~37–40)
  - src/Uno.UI.BindingHelper.Android/*.netcoremobile.csproj     (~31–34)
  - src/Directory.Build.targets version pins                    (~163–177)
src/Uno.Sdk/targets/Uno.Implicit.Packages.ProjectSystem.Android.targets
  - unconditional AndroidX block (~9–19): DELETE
  - PRESERVE/RELOCATE GooglePlay/AndroidTV/AndroidAuto/AndroidWear feature blocks (~21–36)
Uno.WinUI.nuspec / Uno.Foundation.nuspec
  - drop UI lib/net*-android*, net*-ios*, net*-tvos*, net*-maccatalyst* groups + AndroidX deps
  - drop uno-runtime/.../webassembly (DOM); keep uno-runtime/.../skia
  - keep non-UI groups: Uno.Foundation.Logging, Uno.Diagnostics.Eventing, Uno.Fonts.Fluent, Uno.WinRT
Microsoft.TypeScript.MSBuild + WasmCSS embedding → remove from Uno.UI.Wasm build
```
**UnoFeatures**: `skiarenderer` becomes implicit/mandatory for android/ios/tvos/maccatalyst.

---

## E. CI / tooling (`build/ci/`)

```
DELETE stages:  wasm_tests (~130 min: Wasm_UITests_Build/Snap/Automated),
                android_tests (Android_Build_NetCoreMobile_For_Tests + 5 UI buckets + 5 runtime groups),
                ios_tests (iOS_Build + UI/runtime groups)
DELETE:         build/ci/scripts/determine-test-scope.ps1 (RequireNativeAndroid/Ios/Wasm),
                xharness/Xamarin.UITest device runners, iOS TestFlight publish,
                artifacts samplesapp-{android,ios,wasm}-native,
                SamplesApp.Wasm.UITests (Node/TypeScript snapshot runner)
KEEP:           runtime_tests_skia_* (Windows, Linux, Android, iOS, Browser), skia_* stages,
                packages_tests (now Skia-only heads, faster), DevServer/RemoteControl Host
```

---

## F. Documentation update checklist (W12)

```
doc/articles/features/using-native-rendering.md        → remove (xref uno.features.renderer.native)
doc/articles/native-views.md                           → remove (xref Uno.Development.NativeViews)
doc/articles/how-uno-works.md                          → drop native-rendering sections
doc/articles/Uno-UI-Performance.md                     → drop native-tree perf notes
doc/articles/intro.md                                  → drop native renderer mention
doc/articles/features/using-skia-hosting-native-controls.md → revise (iOS opt-in embedding only)
doc/articles/guides/xf-migration/control-mappings.md   → revise
doc/articles/guides/xf-migration/native-controls.md    → revise/remove
doc/articles/toc.yml                                   → remove dead entries/xrefs
AGENTS.md, .claude/rules/platform-targeting.md, .claude/rules/code-style.md → drop native targeting guidance
doc/articles/uno-development/building-uno-ui.md        → mark net*-android/ios/wasm as Skia-only
NEW: 7.0 consumer migration guide (referenced by spec §8)
```

---

## G. Solution / project reference hygiene

Search-and-update after deletions:
- `*.sln` / `*.slnx` / `*.slnf` references to removed projects.
- `ProjectReference` to `Uno.UI.BindingHelper.Android` and `Uno.UI.Runtime.WebAssembly` across `src/AddIns/**`, `src/SamplesApp/**`, `src/SolutionTemplate/**`.
- `Directory.Build.props` `_AdjustedOutputProjects` registry entries for removed variants.
- Generated `Generated/` stubs that referenced native `[Uno.NotImplemented]` shapes (regenerate).

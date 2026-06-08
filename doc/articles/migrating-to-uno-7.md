---
uid: Uno.Development.MigratingToUno7
---

# Migrating to Uno Platform 7.0 — Skia-only rendering

Uno Platform 7.0 removes the **native UI rendering backends** (native Android Views,
native iOS/tvOS/macCatalyst UIKit, and the native WebAssembly DOM renderer) and makes
**Skia the single, implicit rendering engine on every target**.

Skia already runs on every platform — desktop (Win32, WPF, X11, GTK, macOS, FrameBuffer),
Skia-on-Android, Skia-on-iOS, and Skia-on-WebAssembly. In 7.0 it becomes the *only* UI
rendering path: a `UIElement` is a plain managed object backed by a `Composition.Visual`
on all platforms, drawn into a single Skia surface.

> [!IMPORTANT]
> This is a hard removal in a single major version — there is no `[Obsolete]` interim.
> Plan to recompile every Uno library against 7.0 and update your application heads.

## Who is affected

- Apps that **opted out** of the Skia renderer on mobile by omitting
  `<UnoFeatures>skiarenderer</UnoFeatures>` and relying on native rendering.
- Apps that used the **native WebAssembly DOM** renderer
  (`Uno.WinUI.Runtime.WebAssembly`).
- Code that referenced native rendering types, native element hosting, or native-only
  `FeatureConfiguration` flags (see below).

Apps already running on Skia on every target need only recompile against 7.0 and remove
native bootstrap/heads.

## What changed

### Rendering is Skia everywhere

`skiarenderer` is now implicit and mandatory for `android`/`ios`/`tvos`/`maccatalyst`.
You no longer need (and can remove) `<UnoFeatures>skiarenderer</UnoFeatures>`.

WebAssembly renders to a canvas through Skia; there is no per-element DOM tree, no
`Uno.UI.css` styling layer, and no `WindowManager.ts`.

### Packages

| Removed / changed | Migration |
|---|---|
| `Uno.WinUI.Runtime.WebAssembly` package removed | Use `Uno.WinUI.Runtime.Skia.WebAssembly.Browser`. The UI renders to a canvas; there is no DOM tree. |
| `Uno.UI.BindingHelper.Android` assembly removed | Remove the reference; Skia-on-Android needs no Java/JNI binding. |
| `Uno.UI.Maps` AddIn removed | The native Google Maps control has no core Skia equivalent — use a third-party/Skia map or custom rendering. |
| `Uno.WinUI` UI assemblies for `net*-android/ios/tvos/maccatalyst` are now the Skia binaries | Same TFM string, but binary-incompatible with previously native-built consumers. Recompile all libraries against 7.0 and remove native bootstrap. |
| `Xamarin.AndroidX.*` transitive deps removed (AppCompat, RecyclerView, Activity, Browser, SwipeRefreshLayout) | If *your own* code uses AndroidX, add explicit `PackageReference`s. |

### Public API removed

- **Native base classes / identity:** `BindableView` (and `Bindable*` widget wrappers),
  `BindableUIView` (and `BindableUI*`). `UIElement` no longer inherits a native
  `View`/`UIView`/DOM element. Remove casts to `Android.Views.View` / `UIKit.UIView`;
  use `UIElement.Visual` (Composition) and reach platform APIs via the
  `Uno.UI.Runtime.Skia.*` hosts and `Uno.Foundation`.
- **Native element hosting:** `Uno.UI.NativeElementHosting.BrowserHtmlElement`,
  `Uno.UI.Runtime.WebAssembly.HtmlElementAttribute`, and `ContentPresenter` hosting of a
  native `View`/`UIView`/DOM element as `Content`. Use `WebView2` for HTML content, or
  redesign with Uno controls. On iOS, opt-in native embedding remains via
  `UIKitNativeElementHostingExtension` (overlay-composited, reduced performance).
- **Native control / host types:** `NativeListViewBase`, `NativePagedView`,
  `NativeScrollContentPresenter`, `NativeFramePresenter`, `NativePopup`,
  `RootViewController`, `Window : UIWindow` identity, `NativeRenderTransformAdapter`,
  `IShadowChildrenProvider`, `CompositorThread`,
  `Uno.UI.Composition.ICompositionRoot`. Use the WinUI control
  (`ListView`/`Frame`/`Popup`/…) — everything renders via Skia.
- **Composition:** `Uno.CompositionConfiguration.Options.UseCompositorThread` (the Android
  RenderNode compositor thread). Remove the flag; Skia composition needs no dedicated
  native render thread.

### `FeatureConfiguration` flags removed

The native-only flags below no longer exist; delete the calls — behavior is the unified
Skia/WinUI behavior:

- **Android:** `ComboBox.AllowPopupUnderTranslucentStatusBar`,
  `FrameworkElement.AndroidUseManagedLoadedUnloaded`,
  `FrameworkElement.InvalidateNativeCacheOnRemeasure`, `Popup.UseNativePopup`,
  `NativeListViewBase.*`, `NativeFramePresenter.AndroidUnloadInactivePages`,
  `TextBox.UseLegacyInputScope`, `UIElement.UseLegacyClipping`,
  `UIElement.AlwaysClipNativeChildren`, `ScrollViewer.AndroidScrollbarFadeDelay`,
  `WebView.ForceSoftwareRendering`, `PointerRoutedEventArgs.AllowRelativeTimeStamp`,
  `TextBlock.IsJavaStringCachedEnabled` / `JavaStringCachedCapacity`,
  `AndroidSettings.IsEdgeToEdgeEnabled`.
- **iOS:** `Image.LegacyIosAlignment`,
  `FrameworkElement.IOsAllowSuperviewNeedsLayoutWhileInLayoutSubViews`.
- **WebAssembly:** `Interop.ForceJavascriptInterop`, `UIElement.AssignDOMXamlName`,
  `UIElement.AssignDOMXamlProperties`, `TextBlock.IsMeasureCacheEnabled`.
- **Native (Android + iOS):** `ListViewBase.AnimateScrollIntoView`.
- **Native styling:** `Style.UseUWPDefaultStyles`, `Style.ConfigureNativeFrameNavigation()`.
- **Skia overlay:** `TextBox.UseOverlayOnSkia`.

If a single codebase must target both pre-7.0 and 7.0, guard the calls with `#if`.

### Behavioral changes (same API, different result)

Because rendering moves from `Canvas`/`CALayer`/CSS to Skia, expect subtle differences and
re-baseline visual tests:

- **Rendering:** gradients, corner radii, shadows/elevation, and anti-aliasing render via
  Skia — minor pixel differences are possible.
- **Text:** measurement, line-breaking, bidi, kerning, and `MaxLines` go through
  HarfBuzz + SkiaSharp instead of `StaticLayout`/UIKit text/DOM — possible layout shifts
  in text-dense UI.
- **Lists / scrolling:** managed virtualization and scrolling replace
  `RecyclerView`/`UICollectionView`/CSS overflow — fling curves, edge glows, and snap
  points may differ.
- **IME / keyboard:** canvas-rendered text input with a platform IME bridge replaces the
  native `EditText`/`UITextField` — re-test CJK/emoji composition and selection visuals.
- **Pickers / command bars / Frame:** Skia/WinUI-styled rather than native-styled —
  appearance changes on Android/iOS.
- **Animation timing:** Skia interpolation replaces `CABasicAnimation` — standard easing
  matches; exotic timing may not.

### Templates and project heads

New apps get Skia heads only. Existing apps should drop native `*.Mobile` / native
`*.Wasm` (DOM) heads in favor of the Skia heads (`Skia.netcoremobile`,
`Skia.WebAssembly.Browser`, and the desktop Skia head) and remove native bootstrap code.

## Migration checklist

1. Remove `<UnoFeatures>skiarenderer</UnoFeatures>` (now implicit) — and any native-only
   feature switches.
2. Recompile **every** Uno-dependent library against 7.0.
3. Remove references to the deleted assemblies/types and to native element hosting.
4. Delete native-only `FeatureConfiguration` calls.
5. Replace the WASM DOM head with the Skia WebAssembly Browser head; remove any DOM/CSS
   customization and `HtmlElement` usage.
6. Re-baseline visual/snapshot tests and re-test text, lists/scroll, IME, pickers, and
   safe-area/notch handling on devices.

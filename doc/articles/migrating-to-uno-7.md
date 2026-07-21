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

This does **not** drop platform support: Android, iOS, macOS, Windows, Linux, and
WebAssembly all remain supported — they now all render with Skia.

> [!IMPORTANT]
> This is a hard removal in a single major version — there is no `[Obsolete]` interim.
> Plan to recompile every Uno library against 7.0 and update your application heads.

## Who is affected

- Apps that **opted out** of the Skia renderer on mobile by omitting
  `<UnoFeatures>skiarenderer</UnoFeatures>` and relying on native rendering.
- Apps that used the **native WebAssembly DOM** renderer (`Uno.WinUI.WebAssembly`).
- Code that referenced native rendering types, native element hosting, or native-only
  `FeatureConfiguration` flags (see below).

Apps already running on Skia on every target need only recompile against 7.0 and remove
native bootstrap/heads.

## What changed

### Rendering is Skia everywhere

The `NativeRenderer` Uno Feature and the renderer-selection logic are gone — Skia is
always used. `skiarenderer` is now implicit and mandatory for
`android`/`ios`/`tvos`/`maccatalyst`; it is kept as a no-op for back-compat, so you can
leave `<UnoFeatures>skiarenderer</UnoFeatures>` in place or remove it — either way Skia
renders.

WebAssembly renders to a canvas through Skia; there is no per-element DOM tree, no
`Uno.UI.css` styling layer, and no `WindowManager.ts`.

If your project was created before Uno Platform 6.0 and still selects a renderer, follow
the [Uno 6.0 migration guide](xref:Uno.Development.MigratingToUno6) first to move to the
Uno.SDK single-project model.

### Packages

| Removed / changed | Migration |
|---|---|
| `Uno.WinUI.WebAssembly` package removed (and the older `Uno.WinUI.Runtime.WebAssembly`) | Use `Uno.WinUI.Runtime.Skia.WebAssembly.Browser`. The UI renders to a canvas; there is no DOM tree. With the `Uno.SDK`, the Skia browser head is referenced implicitly — there is nothing to add. |
| `Uno.WinUI.Skia.X11`, `Uno.WinUI.Skia.MacOS`, and `Uno.WinUI.Skia.Linux.FrameBuffer` bootstrapper packages removed | These were empty meta-packages that only redirected to the real head. With the `Uno.SDK`, remove the reference — the matching `Uno.WinUI.Runtime.Skia.*` head is referenced implicitly for executable heads. For a hand-rolled (non-`Uno.SDK`) head, replace it with the corresponding `Uno.WinUI.Runtime.Skia.<variant>` package. |
| `Uno.UI.BindingHelper.Android` assembly removed | Remove the reference; Skia-on-Android needs no Java/JNI binding. |
| `Uno.UniversalImageLoader` no longer injected (Android) | Skia handles image loading internally. If you initialized it manually, remove the `ConfigureUniversalImageLoader();` call. |
| `Uno.UI.Maps` AddIn removed | The native Google Maps control has no core Skia equivalent — use a third-party/Skia map or custom rendering. |
| `Uno.WinUI` UI assemblies for `net*-android/ios/tvos/maccatalyst` are now the Skia binaries | Same TFM string, but binary-incompatible with previously native-built consumers. Recompile all libraries against 7.0 and remove native bootstrap. |
| `Xamarin.AndroidX.*` transitive deps removed (AppCompat, RecyclerView, Activity, Browser, SwipeRefreshLayout) | If *your own* code uses AndroidX, add explicit `PackageReference`s. |

> [!NOTE]
> Referencing `Uno.WinUI.WebAssembly` (or the older `Uno.WinUI.Runtime.WebAssembly`)
> alongside the Skia browser head raises the `UNOB0017` build diagnostic. Removing the
> explicit reference resolves it.

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
- **Legacy WebAssembly JavaScript interop:** `Uno.Foundation.Interop.IJSObject`,
  `IJSObjectMetadata`, `JSObjectHandle`, `JSObject`, and
  `WebAssemblyRuntime.InvokeJSWithInterop(FormattableString)` — the Uno-only
  managed-to-JavaScript object-marshalling mechanism (no WinUI counterpart). Migrate to the
  standard .NET WebAssembly interop, [JSImport/JSExport](xref:Uno.Wasm.Bootstrap.JSInterop)
  from `System.Runtime.InteropServices.JavaScript` — the recommended, source-generated path
  (thread-safe, CSP-compliant, no `eval`). The string-based `WebAssemblyRuntime.InvokeJS(string)`
  is *not* removed, but it is a legacy eval-based API and is not recommended for new code.

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
  `AppBarButton.EnableBitmapIconTint`, `TimePickerFlyout.UseLegacyTimeSetting`,
  `NavigationView.EnableUno19516Workaround`, `AndroidSettings.IsEdgeToEdgeEnabled`.
- **iOS:** `Image.LegacyIosAlignment`,
  `FrameworkElement.IOsAllowSuperviewNeedsLayoutWhileInLayoutSubViews`,
  `CommandBar.AllowNativePresenterContent`, `DatePicker.UseLegacyStyle`,
  `TimePicker.UseLegacyStyle`.
- **WebAssembly:** `Interop.ForceJavascriptInterop`, `UIElement.AssignDOMXamlName`,
  `UIElement.AssignDOMXamlProperties`, `TextBlock.IsMeasureCacheEnabled`,
  `Cursors.UseHandForInteraction` (the "hand" cursor for interactive controls is
  now never used).
- **Native (Android + iOS):** `ListViewBase.AnimateScrollIntoView`.
- **Native styling:** `Style.UseUWPDefaultStyles`, `Style.ConfigureNativeFrameNavigation()`.
- **Skia overlay:** `TextBox.UseOverlayOnSkia`.

`WebView2.IsInspectable` is also removed; it was an obsolete alias, so switch to
`WebView2.EnableDevTools` instead.

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

### Application settings on iOS, tvOS, and Mac Catalyst

Values stored through `ApplicationData.Current.LocalSettings` / `.RoamingSettings` used to be
written directly into the shared `NSUserDefaults.StandardUserDefaults` domain. In 7.0 they
are stored in a dedicated `NSUserDefaults` suite named `UnoApplicationData`, persisted as
`Library/Preferences/UnoApplicationData.plist` inside the app sandbox.

This isolates Uno-managed settings from the keys the OS, Apple frameworks, and native
libraries keep in the standard domain: enumerating (`Values.Keys`, `Values.Count`) or
clearing (`Values.Clear()`) application settings no longer sees — or deletes — unrelated
native keys.

**Existing values migrate automatically.** On the first settings access after updating to
7.0, values written by an earlier Uno Platform version (recognized by Uno's serialized
`TypeName:value` format) are moved from the standard defaults into the new container. Apps
that only access settings through the `ApplicationData` API need no changes.

Update your code only if native/interop code reads these values directly from the standard
defaults:

```csharp
// Before 7.0 the values were in NSUserDefaults.StandardUserDefaults
var unoDefaults = new NSUserDefaults("UnoApplicationData", NSUserDefaultsType.SuiteName);
```

```swift
// Swift companion code
let unoDefaults = UserDefaults(suiteName: "UnoApplicationData")
```

Values your app writes to the standard defaults itself through native APIs are not
affected — they stay where they are and remain invisible to `ApplicationData`, as before.

> [!IMPORTANT]
> The migration is one-way. Once a 7.0 build has run, the migrated values are removed from
> `NSUserDefaults.StandardUserDefaults`, so downgrading to a pre-7.0 build of your app will
> not find them there anymore.

See [Application Data and Settings](xref:Uno.Features.ApplicationData) for details on where
each platform stores its data.

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
6. Remove manual `ConfigureUniversalImageLoader();` (Android) and other native bootstrap.
7. Re-baseline visual/snapshot tests and re-test text, lists/scroll, IME, pickers, and
   safe-area/notch handling on devices.
8. On iOS/tvOS/Mac Catalyst, application settings move to the `UnoApplicationData`
   container automatically on first access — update any native/interop code that read them
   from `NSUserDefaults.StandardUserDefaults`.

See the [Uno 6.0 migration guide](xref:Uno.Development.MigratingToUno6#optional-use-of-skia-rendering-for-ios-android-and-webassembly)
for the full Android/iOS/WebAssembly Skia bootstrapping steps.

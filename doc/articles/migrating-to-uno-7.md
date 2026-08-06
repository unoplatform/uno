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
- **Deprecated UIKit disposal helper:** `Uno.Foundation.NSObjectExtensions.ValidateDispose`,
  deprecated since Uno 5.x. Remove the call from your `NSObject`/`UIView` `Dispose`
  overrides — Skia does not host native views, so there is nothing to validate.
- **`Uno.Extensions.UriExtensions.IsLocalResource` is renamed to `IsMsAppx`.** Same behavior —
  the predicate has always tested for the `ms-appx` scheme, which the old name did not say.
  Rename the call; there is no forwarding shim.

  ```diff
  - if (uri.IsLocalResource())
  + if (uri.IsMsAppx())
  ```

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
  `TimePicker.UseLegacyStyle`, `UIElement.FailOnNSObjectExtensionsValidateDispose`
  (see `NSObjectExtensions.ValidateDispose` above).
- **WebAssembly:** `Interop.ForceJavascriptInterop`, `UIElement.AssignDOMXamlName`,
  `UIElement.AssignDOMXamlProperties`, `UIElement.RenderToStringWithId`,
  `TextBlock.IsMeasureCacheEnabled`, `Shape.WasmDelayUpdateUntilFirstArrange`,
  `Shape.WasmCacheBBoxCalculationResult`, `Shape.WasmBBoxCacheSize`,
  `Cursors.UseHandForInteraction` (the "hand" cursor for interactive controls is
  now never used).
- **Native (Android + iOS):** `ListViewBase.AnimateScrollIntoView`.
- **Native styling:** `Style.UseUWPDefaultStyles`, `Style.ConfigureNativeFrameNavigation()`.
- **Skia overlay:** `TextBox.UseOverlayOnSkia`.

`WebView2.IsInspectable` is also removed; it was an obsolete alias, so switch to
`WebView2.EnableDevTools` instead.

The cross-platform `Control.UseLegacyContentAlignment` flag is also removed. It opted into the
legacy Top/Left default for `HorizontalContentAlignment`/`VerticalContentAlignment`; the default is
now always the WinUI-correct **Center/Center**. Apps that set it to `true` should instead set
`HorizontalContentAlignment`/`VerticalContentAlignment` explicitly (via a `Style` or per control).

The cross-platform `WinRTFeatureConfiguration.ApplicationLanguages.UseLegacyPrimaryLanguageOverride`
flag is also removed. Unlike the flags above it defaulted to `true`, so this changes behavior for
apps that never touched it: setting `Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride`
no longer swaps `CultureInfo.CurrentCulture`/`CurrentUICulture` from inside the setter. The override
is persisted and applied to the culture on the next app start, matching WinUI. Resource lookup
(`x:Uid`, `ResourceLoader`) still follows the new value immediately, so localized strings keep
updating once the affected pages reload.

Apps that relied on the immediate culture swap — number, date, and currency formatting, or .NET
resource lookup through `CurrentUICulture` — should set the culture themselves alongside the
override:

```csharp
ApplicationLanguages.PrimaryLanguageOverride = language;

var culture = new CultureInfo(language);
CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = culture;
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = culture;
```

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

### Type-hierarchy changes (WinUI parity)

7.0 realigns several types to their WinUI base classes. Most code is unaffected — the
change only breaks code that used the Uno-only members leaked by the wrong base.

- **`MediaPlayerPresenter`** now derives directly from **`FrameworkElement`** (matching
  WinUI) instead of `Border`, removing the extra `Border` level. The `Border`-only surface
  it used to expose — `Child`, `BorderBrush`, `BorderThickness`, `CornerRadius`, `Padding`,
  `BackgroundSizing`, `ChildTransitions` — is gone, and `is Border` is no longer `true` for
  a presenter. The video surface is hosted internally, so playback and `Stretch` are
  unchanged. `MediaPlayerElement` sets its own `Background` (black) on the template root, so
  letterbox bars stay black.
- **`ImageBrush`** now derives from a real **`TileBrush`** (`Brush → TileBrush →
  ImageBrush`, matching WinUI) instead of directly from `Brush`. `AlignmentX`,
  `AlignmentY`, and `Stretch` behave the same for callers but are now **declared on
  `TileBrush`** — code that referenced the static DPs by their declaring type
  (`ImageBrush.StretchProperty`, `AlignmentXProperty`, `AlignmentYProperty`) still resolves
  via inheritance, and `is TileBrush` is now `true` for an `ImageBrush`. Instance usage
  (`imageBrush.Stretch`, XAML `Stretch="…"`) is unaffected.

### XAML changes

- **The WPF-style `clr-namespace:` xmlns form is no longer accepted.** WinUI only supports
  `using:`; Uno used to also accept `clr-namespace:MyApp.Controls;assembly=MyLib` and silently
  strip the prefix and the `;assembly=` token. Replace each declaration with the `using:` form —
  the assembly is inferred, so the `;assembly=` part is simply dropped:

  ```diff
  - xmlns:local="clr-namespace:MyApp.Controls;assembly=MyLib"
  + xmlns:local="using:MyApp.Controls"
  ```

  This is enforced at **build time** (the `UXAML0006` diagnostic) and at **run time** by
  `XamlReader.Load` and Hot Reload, which throw a `XamlParseException`. The declaration itself is
  rejected whether or not the prefix is used, so unused `clr-namespace:` declarations must also be
  removed — the only exemption is a prefix listed in `mc:Ignorable` on the root element.

- **A relative URI on a `Uri`-typed property now compiles to `ms-resource:///Files/…`**, the MRT
  local-resource form WinUI produces. Previously Uno emitted the relative string verbatim. This
  affects custom `Uri` properties, `HyperlinkButton.NavigateUri`, `Hyperlink.NavigateUri`,
  `BitmapIcon.UriSource`, `BitmapIconSource.UriSource`, `WebView2.Source`,
  `MediaPlayerElement.Source`, `LottieVisualSource.UriSource`, and the `Uri` passed to
  `RandomAccessStreamReference.CreateFromUri`:

  ```diff
    <BitmapIcon UriSource="Assets/icon.png" />
  - // compiled value: Assets/icon.png
  + // compiled value: ms-resource:///Files/Assets/icon.png
  ```

  The rewrite is a prefix concat that drops a leading `/` and ignores the folder of the XAML file
  containing it, exactly as WinUI does. **Images declared in application XAML keep loading the same
  asset** — Uno resolves `ms-resource:///Files/X` as `ms-appx:///X`. To keep a value verbatim, write
  it as an explicit `ms-appx:///…` URI in XAML.

  Only the four image sinks Uno already resolved as assets carry that mapping. The properties above
  that never resolved a relative URI stay inert with the new value, exactly as they are on WinUI —
  a hand-written `ms-resource:///Files/…` is treated the same as the compiler-generated form, since
  that *is* MRT's local-file namespace and Uno ships no MRT to resolve it any other way.

  Two changes go beyond the value you read back:

  - **A relative value is now absolute at the point of use.** `WebView2.Source` with a relative value
    used to throw `ArgumentException` while the page initialized; it now navigates a `ms-resource:` URI
    no web view can service, so the failure is silent instead of loud. A `NavigateUri` written without
    a scheme (`www.example.com`) likewise becomes `ms-resource:///Files/www.example.com` and reaches
    the launcher as an unregistered scheme, and a relative `Control.DefaultStyleResourceUri` no longer
    throws while resolving. Give these properties absolute URIs — `https://…`, `ms-appx:///…`.
  - **In a library, relative URIs resolve differently per property type.** On an `ImageSource`-typed
    property the value now carries the library's assembly prefix (`ms-appx:///MyLib/Assets/x.png`), so
    a library's own assets resolve — but a library asset that previously resolved from the *consuming
    app's* root no longer does. On a `Uri`-typed property the `ms-resource` form cannot express that
    prefix, so it resolves against the app root; a library shipping assets for `BitmapIcon` must use
    an explicit `ms-appx:///MyLib/…` URI.

  Properties typed `ImageSource` are unaffected in shape but now resolve consistently: `Image.Source`,
  `ImageBrush.ImageSource`, and custom `ImageSource` properties all resolve a relative URI against
  the base URI. `ImageBrush.ImageSource` and custom `ImageSource` properties previously kept the
  relative string. `ResourceDictionary.Source` is unchanged.

  Two divergences from WinUI remain here, both deliberate:

  - **The base URI is the assembly root, not the XAML file's folder.** `Assets/logo.png` written in
    `Views/MainPage.xaml` resolves as `ms-appx:///Assets/logo.png` on Uno and
    `ms-appx:///Views/Assets/logo.png` on WinUI. This predates the rewrite and is unchanged by it.
  - **Svg assets keep the `ms-appx:///` form.** Measured on WinAppSDK 1.7, WinUI compiles every
    relative svg URI to `ms-resource:///Files/…` — including one written on `Image.Source` rather
    than on `SvgImageSource.UriSource`. Uno keeps `ms-appx:///`, because that form is resolved as an
    asset path and so carries the assembly prefix a library's own svg assets need; the `ms-resource`
    form resolves only against the application root.

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
7. Convert every `xmlns:…="clr-namespace:…"` declaration in your XAML to the `using:` form.
8. Re-baseline visual/snapshot tests and re-test text, lists/scroll, IME, pickers, and
   safe-area/notch handling on devices.

See the [Uno 6.0 migration guide](xref:Uno.Development.MigratingToUno6#optional-use-of-skia-rendering-for-ios-android-and-webassembly)
for the full Android/iOS/WebAssembly Skia bootstrapping steps.

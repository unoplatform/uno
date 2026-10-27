---
uid: Uno.Development.MigratingToUno7.Behavior
---

# Behavior changes to re-test

Part of [Migrating to Uno Platform 7.0](xref:Uno.Development.MigratingToUno7).

**Read this if** your app builds and you are planning what to re-test, or something compiles but acts or looks different on 7.0: same API, different result.

**Skip it if** you have no re-testing to plan. If your app already ran on Skia on every target, you can skip the first section and start at [Rendering backend and drawing libraries](#rendering-backend-and-drawing-libraries).

## Rendering differences

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

See also: [Rendering is Skia everywhere](xref:Uno.Development.MigratingToUno7.ProjectSetup#rendering-is-skia-everywhere).

## Rendering backend and drawing libraries

### Vulkan is the default rendering backend

Vulkan is the default rendering backend on Android, Linux (X11) and Windows (Win32).
Before 7.0 Vulkan was opt-in. Devices without a usable Vulkan driver fall back to other
available backends, so no configuration is needed - but the GPU path most apps actually
run on has changed, so re-test rendering on your target devices, especially custom
`SKCanvas` drawing and native-element interop. Vulkan can also use more memory than
OpenGL, by an amount that depends on the GPU driver; see
[Startup Time and Memory](xref:Uno.Skia.Vulkan#startup-time-and-memory).
To disable Vulkan on desktop while keeping
fallback, use `.UseX11(b => b.DisableRenderingBackends(X11RenderingBackend.Vulkan))` or
`.UseWin32(b => b.DisableRenderingBackends(Win32RenderingBackend.Vulkan))`. To select only
OpenGL, use `ForceRenderingBackend` with the matching `OpenGL` enum value; a forced backend
does not fall back if it cannot be created. The desktop `UseVulkanOnX11`,
`UseVulkanOnWin32`, `UseOpenGLOnX11`, `UseOpenGLOnWin32` and `PreferGLESOverGLOnX11` feature
flags are no longer available. On Android, use
`.UseAndroid(b => b.UseVulkan(false))` or set
`FeatureConfiguration.Rendering.UseVulkanOnSkiaAndroid = false` before building the host.
See [Vulkan Rendering Backend](xref:Uno.Skia.Vulkan).

See also: [Android head uses the host builder](xref:Uno.Development.MigratingToUno7.AppStartup#android-head-uses-the-host-builder).

### SkiaSharp 4 is the default

The Uno.Sdk now resolves the whole SkiaSharp package group —
`SkiaSharp`, `SkiaSharp.Skottie`, the `SkiaSharp.Views.*` packages and every
`SkiaSharp.NativeAssets.*` — at a `4.x` version; on 6.x it was `3.x` with 4 available opt-in
through `SkiaSharpVersion`. Apps that never touch SkiaSharp directly need no change. Apps
with their own `SKCanvas`/`SKPaint` drawing code may not compile: SkiaSharp 4 turns several
long-deprecated APIs into errors, most visibly the text APIs that moved from `SKPaint` to
`SKFont`. If you pin SkiaSharp packages yourself, move **all** of them — managed and
native-asset alike — to the same 4.x version, since a managed/native mismatch fails at
runtime rather than at build time.

See also: [`SkiaSharp.Views.Uno.WinUI`](xref:Uno.Development.MigratingToUno7.Packages#skiasharpviewsunowinui-no-longer-referenced-implicitly), [SkiaSharp and HarfBuzz native assets](xref:Uno.Development.MigratingToUno7.Packages#runtime-hosts-no-longer-carry-skiasharp-or-harfbuzz-native-assets).

### `GLCanvasElement` subclasses compile against Silk.NET.OpenGL 2.23

[`GLCanvasElement`](xref:Uno.Controls.GLCanvasElement) subclasses compile against Silk.NET.OpenGL 2.23 (6.x used 2.16). The OpenGL calls are unchanged, but some enum types passed to them are renamed,
so code using those types needs updating — for example, `gl.CullFace` takes a `TriangleFace` instead of a
`CullFaceMode`:

```diff
- gl.CullFace(CullFaceMode.Back);
+ gl.CullFace(TriangleFace.Back);
```

## List item chrome

Independently of rendering, list items now draw their chrome the way WinUI does.

### `ListViewItem` and `GridViewItem` render through the item presenters

`ListViewItem` and `GridViewItem` render through `ListViewItemPresenter` and
`GridViewItemPresenter`. The default Fluent styles no longer use the expanded templates, so
hover, selection and multi-select visuals change, and named template parts such as
`MultiSelectSquare` no longer exist. `ListViewItemExpanded` still ships and can be set as
`ItemContainerStyle`. The Fluent `GridViewItemExpanded` is removed, as WinUI has none, so
`{StaticResource GridViewItemExpanded}` now resolves to an older, non-Fluent style. Copy the
template into your app if you depend on it.

### `GridViewItem` defaults to `AllowDrop="True"`

As in WinUI, while a drag is over the middle of an item, the item shows its drag-over state and
marks `DragOver` handled, so `DragOver` handlers on the `GridView` or its ancestors are not
called there. Register them with
`AddHandler(UIElement.DragOverEvent, handler, handledEventsToo: true)`, or set `AllowDrop` to
`False` in your `ItemContainerStyle`.

## Manipulation and gestures

Independently of rendering, manipulation recognition was realigned with WinUI.

See also: [Cross-platform compatibility flags](xref:Uno.Development.MigratingToUno7.FeatureFlags#cross-platform-compatibility-flags).

### No `ManipulationDelta` immediately after `ManipulationStarted`

The distance traveled
before a manipulation is recognized is now reported through `ManipulationStarted`'s
`Cumulative` and is never replayed as a delta, so a pan no longer visibly jumps by the
recognition threshold the moment it starts. This matches WinUI, where a manipulation can
complete without raising a single `ManipulationDelta`. Handlers that positioned content by
accumulating `ManipulationDelta.Delta` alone (the classic `GridSplitter` pattern) will now
under-track by the recognition distance — read `Cumulative` instead, which still measures
from the press point and is the value WinUI's own samples use.

### Manipulation start thresholds changed

Touch and pen go from 15px to 10px, so panning
starts sooner. Mouse goes from 1px to 4px, matching the Win32 system drag threshold
(`SM_CXDRAG`), so a small wiggle during a click is no longer reported as a drag. Code that
relied on a near-zero mouse threshold to start a drag should re-test.

### A touch or pen hold is no longer a tap

As on WinUI, a hold raises `Holding` and then
`RightTapped`, but no `Tapped` or `DoubleTapped`, and releasing a hold on a `ListView` or `GridView`
item raises no `ItemClick` and doesn't change the selection. Code that used a long press as a slow
tap should handle `Holding` or `RightTapped` instead. To get the earlier behavior back, set
`Uno.WinRTFeatureConfiguration.GestureRecognizer.SuppressTapOnHold` to `false` at startup (default
`true`). That does not bring back the browser's own long-press text selection and callout on the
WebAssembly host page, which 7.0 turns off. See [Feature flags](xref:Uno.Development.FeatureFlags#touch-and-pen-holds).

See also: [`ListViewBase.ItemClickCommand`](xref:Uno.Development.MigratingToUno7.RemovedApis#listviewbaseitemclickcommand-and-listviewbasecommand).

## Controls and lists

### `ContentControl` subclasses without a `ControlTemplate`

Uno used to bypass the
`ContentPresenter` and parent `Content` directly when a control had no template at all,
so a custom `ContentControl` that set its own `DefaultStyleKey` but shipped no
`Template` setter still rendered its content. That leniency is gone; as on WinUI, such a
control now renders nothing. Give it a default style with a `Template`, or leave
`DefaultStyleKey` alone so it inherits the `ContentControl` default template.

List selection scrolling was also realigned with WinUI.

See also: [`UserControl` derives from `Control`](xref:Uno.Development.MigratingToUno7.WinUIParity#usercontrol-derives-from-control-not-contentcontrol).

### A new selection is brought into view only while the list has focus

`ListView` and
`GridView` used to scroll to every newly selected item. Now, as on WinUI, setting
`SelectedItem` or `SelectedIndex` from code on a list that doesn't have focus leaves the
scroll position alone, and a focused list scrolls only as far as needed to show the item.
Clicking or keyboard navigation still brings the item into view, since they focus the list.
In `Multiple` mode only the first selection is brought into view. Call
`ScrollIntoView(item)` after selecting if you need the old behavior.

See also: [`ListViewBase.ItemClickCommand`](xref:Uno.Development.MigratingToUno7.RemovedApis#listviewbaseitemclickcommand-and-listviewbasecommand).

## `SystemBackdrop` no longer rewrites your content's backgrounds

Setting `Window.SystemBackdrop` used to walk the whole visual tree and replace the
`Background` of every `Panel`, `Border`, `ContentPresenter`, and `Control` that had an opaque
`SolidColorBrush` with a transparent one, re-walking whenever new content was loaded. That is
not what WinUI does, and it was observable from app code — a `Grid` you had explicitly painted
red read back as `Transparent` afterwards.

7.0 matches WinUI: applying a backdrop drops the **window's own root background** only, and your
content is left exactly as you set it.

- **If your app relied on the old behavior**, the material will now be hidden behind your own
  opaque backgrounds. Make the surfaces you want the material to show through transparent
  yourself — typically the root `Page` or panel:

  ```xml
  <Page Background="Transparent">
      <!-- your content -->
  </Page>
  ```

  This is what WinUI has always required, so the same markup works on Windows.
- **Backgrounds bound with `{ThemeResource}` are no longer clobbered.** The old walk wrote its
  transparent brush with local precedence, which silently stopped those backgrounds from
  re-resolving on a theme change. They now update correctly.
- Reading `Background` back after setting a backdrop returns your own brush again.

See also: [`Background` is no longer on `FrameworkElement`](xref:Uno.Development.MigratingToUno7.WinUIParity#background-is-no-longer-on-frameworkelement).

## Dependency property defaults

Dependency properties registered without a default value now follow WinUI.

### Custom value types default to `null`

`DependencyProperty.Register` with no metadata, or
with a `null` `DefaultValue`, used to synthesize the zero value of any value type. It now does
what WinUI does: `int`, `long`, `float`, `double`, `bool`, `TimeSpan`, `DateTimeOffset`,
`Point`, `Size`, `Rect`, `Color`, `Thickness`, `CornerRadius` and the enums of WinUI's XAML
metadata (`Visibility`, `Orientation`, `FontStyle`, `VirtualKey`, ...) get their zero value,
`GridLength` gets `1*`, and every other value type (your own structs and enums, `DateTime`,
`Guid`, `decimal`, `Duration`, `Matrix`, `Vector2`, ...) gets `null`, so a
`(MyEnum)GetValue(...)` wrapper on such a property throws until a value is set. Pass the
default explicitly, for example `new PropertyMetadata(MyEnum.None)`. Code that already runs
on Windows is unaffected, since it needed the explicit default there. The WinUI documentation
says a value type gets "the default of the type", but the WinAppSDK runtime does that only for
the types above; the table was measured on it. `SetValue(null)` on such a property is now
stored rather than rejected with an `InvalidOperationException`, and `ClearValue`, or a
binding that produces `null`, leaves it `null` too. With Debug logging enabled for
`Microsoft.UI.Xaml.DependencyProperty`, every registration that gets a `null` default is
listed at startup.

See also: [`DependencyObject` is a class](xref:Uno.Development.MigratingToUno7.WinUIParity#dependencyobject-is-a-class-not-an-interface), [`DependencyPropertyValuePrecedences`](xref:Uno.Development.MigratingToUno7.RemovedApis#dependencypropertyvalueprecedences-members-changed).

## Authentication on WebAssembly

Independently of rendering, `Uno.WinUI.MSAL`'s `WithUnoHelpers()` changed on WebAssembly.

### `WithUnoHelpers()` signs in through `WebAuthenticationBroker` on WebAssembly

The WASM-only
flavor of `Uno.UI.MSAL` was removed, and its helpers now ship in the main assembly.
`PublicClientApplicationBuilder.WithUnoHelpers()` still sets a browser-compatible
`HttpClient` factory, and `AcquireTokenInteractiveParameterBuilder.WithUnoHelpers()` sets a
web UI that uses the [`WebAuthenticationBroker`](xref:Uno.Features.WAB) popup, configured
through `WinRTFeatureConfiguration.WebAuthenticationBroker`. Both replace a
`WithHttpClientFactory(...)` or `WithCustomWebUi(...)` called before them, so call your own
after `WithUnoHelpers()` — see [MSAL: WebAssembly](xref:Uno.Interop.MSAL#webassembly).

On WebAssembly, `WebAuthenticationBroker.AuthenticateAsync()` changed how it ends.

### A timeout returns `ErrorHttp` instead of `UserCancel`

When
`WinRTFeatureConfiguration.WebAuthenticationBroker.AuthenticationTimeout` elapses, the result is
`WebAuthenticationStatus.ErrorHttp` with `ResponseErrorDetail` 408, so it can be told apart
from the user closing the popup. Canceling the operation now also closes the popup or
`<iframe>`, and the `<iframe>` mode now honors the timeout.

## Android drawables

On Android, retargeted drawables now keep their file extension in their resource name.

### Retargeted drawables are renamed

Images under `Assets` that Uno packages as Android
drawables now include the original extension, so `Assets/logo.png` and `Assets/logo.jpg` no
longer collide and a same-named `logo.svg` asset is no longer shadowed by the PNG. Code that
uses the generated ids changes accordingly: `Resource.Drawable.Assets_logo` becomes
`Resource.Drawable.Assets_logo_png`. Callers of `DrawableHelper.FindResourceId` must now pass
the filename with its extension (for example, `FindResourceId("logo.png")`); `FindResourceId("logo")`
no longer resolves. XAML and code that load images through `ms-appx:///` URIs or relative paths are unaffected.

See also: [`bundle://` image scheme removed](xref:Uno.Development.MigratingToUno7.Packages#bundle-image-scheme-removed-iostvos).

### Drawables you add under `Platforms/Android/Resources/drawable` no longer resolve through `ms-appx:///`

A lookup for `ms-appx:///foo.png` now searches for the drawable `foo_png`, not
`foo`. Move images you load by URI into `Assets` so Uno packages them, or reference the native
drawable directly (`Resource.Drawable.foo`).

See also: [Relative URIs in XAML](xref:Uno.Development.MigratingToUno7.Xaml#relative-uris).

## Related pages

- [Removed feature flags](xref:Uno.Development.MigratingToUno7.FeatureFlags) if your app set a flag that kept the old behavior.
- [WinUI API alignment](xref:Uno.Development.MigratingToUno7.WinUIParity) for changes the compiler does report.
- [App startup, activation and platform state](xref:Uno.Development.MigratingToUno7.AppStartup) for iOS settings and activation.
- [Errors, symptoms and code search](xref:Uno.Development.MigratingToUno7.Lookup) to go from an error or symptom to its fix.
- [Upgrade steps](xref:Uno.Development.MigratingToUno7) to return to the main path.

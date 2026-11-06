---
uid: Uno.Development.MigratingToUno7.RemovedApis
---

# Removed public APIs

Part of [Migrating to Uno Platform 7.0](xref:Uno.Development.MigratingToUno7).

**Read this if** the compiler reports a missing type or member after the upgrade, particularly native views, HTML interop, `ListViewBase.ItemClickCommand`, `GetTemplateChild`, or C# shortcuts such as `Brush brush = Colors.Red`.

**Skip it if** your code builds cleanly. `ItemClickCommand` set with `{Binding}` is the exception: it [still compiles but does nothing](xref:Uno.Development.MigratingToUno7.Lookup#changes-the-compiler-wont-catch).

## Native rendering types and helpers

### Native base classes / identity

`BindableView` (and `Bindable*` widget wrappers),
`BindableUIView` (and `BindableUI*`). `UIElement` no longer inherits a native
`View`/`UIView`/DOM element. Remove casts to `Android.Views.View` / `UIKit.UIView`;
use `UIElement.Visual` (Composition) and reach platform APIs via the
`Uno.UI.Runtime.*` hosts and `Uno.Foundation`.

### Native element hosting

`Uno.UI.Runtime.WebAssembly.HtmlElementAttribute`,
`Uno.Extensions.HtmlCustomEventArgs`, and `VisualTreeHelper.AdaptNative` /
`TryAdaptNative`. The DOM-interop surface that went with the WebAssembly DOM renderer —
`UIElement.HtmlId`, `RegisterHtmlEventHandler`, `SetHtmlAttribute` and friends — is gone
with it.

Native views are still supported: set the native element as the `Content` of a
`ContentControl` (or `ContentPresenter`) and the Skia host overlays it on the rendered UI,
following its layout, clipping, z-order and opacity. No wrapper is needed, so drop the
`AdaptNative` call:

```diff
- Content = VisualTreeHelper.AdaptNative(nativeView);
+ Content = nativeView;
```

The native element is an `Android.Views.View` on Android, a `UIKit.UIView` on iOS/tvOS, and
a `Uno.UI.NativeElementHosting.BrowserHtmlElement` on WebAssembly — the supported replacement
for the removed HTML interop. On desktop it is a `Win32NativeWindow` or `X11NativeWindow`. A
native view cannot be a `Panel` child, only content. See
[Embedding Native Elements in Skia Apps](xref:Uno.Skia.Embedding.Native). Use `WebView2`
for full HTML documents.

### Native control / host types

`NativeListViewBase`, `NativePagedView`,
`NativeScrollContentPresenter`, `NativeFramePresenter`, `NativePopup`,
`RootViewController`, `Window : UIWindow` identity, `NativeRenderTransformAdapter`,
`IShadowChildrenProvider`, `CompositorThread`,
`Uno.UI.Composition.ICompositionRoot`. Use the WinUI control
(`ListView`/`Frame`/`Popup`/…) — everything renders via Skia.

### Layout-constraint optimization

`FrameworkElement.AreDimensionsConstrained`,
`FrameworkElement.UseConstraintOptimizations`, the `ILayoutConstraints` interface, and the
protected `FrameworkElement.IsSimpleLayout`, `IsWidthConstrainedInner` and
`IsHeightConstrainedInner` members. They let the native Android layouter skip re-measuring
a parent; Skia layout never read them, so setting them had no effect. Remove the
assignments and any overrides in your own controls. XAML that still sets the property now
fails the build with `Property 'AreDimensionsConstrained' does not exist on '<Type>'`:
`not_win:AreDimensionsConstrained="True"` fails on every Uno Platform target, while a
platform-prefixed form such as `android:AreDimensionsConstrained` fails only when building
that target.

### Native flyout opt-in

`FlyoutBase.UseNativePopup`, including its conditional-XAML forms
(`android:UseNativePopup` / `ios:UseNativePopup`). Remove the assignment — flyouts always use
the WinUI presentation. The two `Uno.UI.Toolkit` attached properties that only ever
customized that native iOS presentation go with it:
`MenuFlyoutItemExtensions.IsDestructive` (red "destructive" item text) and
`MenuFlyoutExtensions.CancelTextIosOverride` (custom cancel-button caption). Remove the
attributes; style the `MenuFlyoutItem` directly for a destructive look.
`UICommand.IsDestructive` is **not** removed — it still drives the native iOS
`MessageDialog` — but its Uno Platform wrapper is ([see below](#unouitoolkit-members-that-only-ever-ran-on-native-targets)); set the property directly.

### Native default styles

the whole `Generic.Native.xaml` dictionary is gone, so the
`NativeDefaultButton`, `NativeDefaultCheckBox`, `NativeDefaultCommandBar`,
`NativeDefaultAppBarButton`, `NativeDefaultFrame`, `NativeDefaultPivot`,
`NativeDefaultProgressBar`, `NativeDefaultSlider`, `NativeDefaultTextBox`,
`NativeDefaultToggleSwitch`, `NativeDefaultSplitViewOpenPaneLength`, `AndroidButtonStyle`,
`AndroidCheckBoxStyle`, `AndroidRadioButtonStyle`, `iOSButtonStyle`,
`IosPickerFlyoutTextButtonStyle`, `LeftDrawerSplitViewStyle`, and
`RightDrawerSplitViewStyle` resource keys no longer resolve. These styles templated native
views, so there is no Skia equivalent — drop the `Style="{StaticResource NativeDefault…}"`
and the control falls back to the WinUI default style it already used when no native style
was registered. `Uno.UI.Converters.UnoNativeDefaultProgressBarReverseBoolConverter` goes
with them.

### Native-style declaration

the `not_win:IsNativeStyle="True"` attribute on a `Style` is no
longer recognized. A third-party dictionary that still carries it now **fails the XAML
build** instead of silently registering a second, native default style — remove the
attribute. The Uno-only
`Style.RegisterDefaultStyleForType(Type, IXamlResourceDictionaryProvider, bool)` also loses its
`isNative` parameter; it is `[EditorBrowsable(Never)]` and normally only called from
XAML-generated code, so rebuilding regenerates the correct call.

### `Uno.UI.Controls.Legacy.ProgressRing`

the ellipse-based ring, its `DefaultWuxProgressRingStyle`
style and `Uno.UI.Controls.Legacy.Primitives.ProgressRingTemplateSettings` are removed. It only existed to
contrast with the native ring on Android and iOS, which are gone. Use `Microsoft.UI.Xaml.Controls.ProgressRing`
(drop the `legacy:` prefix); it needs no extra package. Its default size is 32x32 instead of 20x20, so set
`Width` and `Height` explicitly if your layout relied on the smaller ring.

### `Uno.UI.Toolkit` members that only ever ran on native targets

`Uno.UI.ViewHelper.Architecture`, which always returned `null` (the rest of
`Uno.UI.ViewHelper` stays), and
`UICommandExtensions.SetDestructive`. To flag a destructive `UICommand`, set
`UICommand.IsDestructive` directly from an iOS-targeted head.

```diff
- uic.SetDestructive(true);
+ uic.IsDestructive = true;
```

### Composition

`Uno.CompositionConfiguration.Options.UseCompositorThread` (the Android
RenderNode compositor thread). Remove the flag; Skia composition needs no dedicated
native render thread.

### Deprecated UIKit disposal helper

`Uno.Foundation.NSObjectExtensions.ValidateDispose`,
deprecated since Uno 5.x. Remove the call from your `NSObject`/`UIView` `Dispose`
overrides — Skia does not host native views, so there is nothing to validate.

## WebAssembly interop

### Legacy WebAssembly JavaScript interop

`Uno.Foundation.Interop.IJSObject`,
`IJSObjectMetadata`, `JSObjectHandle`, `JSObject`, and
`WebAssemblyRuntime.InvokeJSWithInterop(FormattableString)` — the Uno-only
managed-to-JavaScript object-marshalling mechanism (no WinUI counterpart). Migrate to the
standard .NET WebAssembly interop, [JSImport/JSExport](xref:Uno.Wasm.Bootstrap.JSInterop)
from `System.Runtime.InteropServices.JavaScript` — the recommended, source-generated path
(thread-safe, CSP-compliant, no `eval`). The string-based `WebAssemblyRuntime.InvokeJS(string)`
is *not* removed, but it is a legacy eval-based API and is not recommended for new code.

### TypeScript interop marker

`Uno.Foundation.Interop.TSInteropMessageAttribute` and its
`CodeGeneration` enum. They drove a TypeScript bindings generator whose only input was set by the
WebAssembly DOM heads removed in 7.0, so the attribute had no effect. Remove it from your structs.

## Fluent resources

### Fluent Design resource-version types

`Microsoft.UI.Xaml.Controls.XamlControlsResourcesV1`,
`Microsoft.UI.Xaml.Controls.XamlControlsResourcesV2`, the `ControlsResourcesVersion` enum, and
the `ControlsResourcesVersion` member on **both** `XamlControlsResources` (a dependency property)
and `XamlControlsResourcesV2` (an inert `object` property that was never read). None of this
existed in Windows App SDK — `XamlControlsResources` is the only Fluent resources type there, and
it now matches. Use it everywhere:

```diff
- <XamlControlsResourcesV2 />
+ <XamlControlsResources />
```

The V1 styles were deleted several releases ago and `XamlControlsResources` has loaded V2
regardless of the property ever since, so none of this changes visual behavior. Drop any
`ControlsResourcesVersion` assignment, in code or in XAML — on either type, a leftover attribute
now fails the XAML build:

```diff
- <XamlControlsResources ControlsResourcesVersion="Version2" />
+ <XamlControlsResources />
```

The `Uno.UI.FluentTheme.v1` assembly is removed along with these types, and
`Uno.UI.FluentTheme.v2` is merged into `Uno.UI.FluentTheme` — see [Packages and Uno Features](xref:Uno.Development.MigratingToUno7.Packages#unouifluentthemev1-assembly-removed).

## `Window` events

### `Window` event handler delegates

`Microsoft.UI.Xaml.WindowActivatedEventHandler`,
`WindowSizeChangedEventHandler`, `WindowVisibilityChangedEventHandler`, and
`WindowClosedEventHandler`. None of them exists in WinUI, which declares every `Window` event
as `TypedEventHandler<object, TArgs>` — `Activated`, `SizeChanged`, and `VisibilityChanged`
now do the same, and `Closed` already did, which left `WindowClosedEventHandler`
unreferenced. The handler shape is unchanged for `Activated` and `SizeChanged`, so lambdas,
method groups, and `+=`/`-=` keep compiling; only code that named the delegate type
explicitly breaks:

```diff
- window.SizeChanged += new WindowSizeChangedEventHandler(OnSizeChanged);
+ window.SizeChanged += OnSizeChanged;
```

### `Window.VisibilityChanged` now carries `Microsoft.UI.Xaml.WindowVisibilityChangedEventArgs`

`Window.VisibilityChanged` now carries `Microsoft.UI.Xaml.WindowVisibilityChangedEventArgs`
instead of `Windows.UI.Core.VisibilityChangedEventArgs`, matching WinUI. A handler declared
with the old argument type no longer binds, so update its signature. Both members carry the
same meaning on the new type — `Visible` is get-only and `Handled` is get/set:

```diff
- private void OnVisibilityChanged(object sender, VisibilityChangedEventArgs e)
+ private void OnVisibilityChanged(object sender, WindowVisibilityChangedEventArgs e)
  {
      VisibilityLabel.Text = e.Visible ? "Visible" : "Hidden";
  }
```

`Windows.UI.Core.VisibilityChangedEventArgs` itself is **not** removed — it is still the
argument type of the legacy `CoreWindow.VisibilityChanged` event.

## Other Uno-only or non-public members

### `ListViewBase.ItemClickCommand` and `ListViewBase.Command`

Neither exists in WinUI: the
first was a plain property (not a dependency property, so it could not be set from a `Style`)
that ran just before `ItemClick`, and the second was a hidden alias of it. A `ListView` or
`GridView` subclass may now declare its own `ItemClickCommand` without a `new` modifier.
Code that sets either property, and XAML that sets it to a literal value, no longer compiles.
XAML that sets it with `{Binding}` **still compiles but no longer does anything**, so search your
XAML for `ItemClickCommand=` and `Command=` on lists.
Handle `ItemClick` instead, or attach the command with the
[Uno Toolkit](xref:Toolkit.GettingStarted) `CommandExtensions.Command`, which passes the
clicked item as the parameter unless `CommandExtensions.CommandParameter` is set:

```diff
- <ListView IsItemClickEnabled="True" ItemClickCommand="{Binding OpenItem}" />
+ <ListView IsItemClickEnabled="True" utu:CommandExtensions.Command="{Binding OpenItem}" />
```

With [XAML Behaviors](https://github.com/microsoft/XamlBehaviors), use an
`EventTriggerBehavior` on `ItemClick` with an `InvokeCommandAction`. Without a
`CommandParameter`, the action passes the `ItemClickEventArgs` to the command, so read
`ClickedItem` from it or convert it with an `InputConverter`.

### `VisualTreeHelper.GetChildren` is no longer public

It does not exist in WinUI.
`VisualTreeHelper.GetChildren(DependencyObject)` and `GetChildren<T>(DependencyObject)` are
now internal; enumerate children with the WinUI API instead:

```csharp
var count = VisualTreeHelper.GetChildrenCount(parent);
for (var i = 0; i < count; i++)
{
    var child = VisualTreeHelper.GetChild(parent, i);
}
```

### `Control.GetTemplateChild` is no longer public

Uno declared it `public`; WinUI declares
it `protected internal`, and 7.0 now matches. Calling it on a control from outside a `Control`
subclass, for example `autoSuggestBox.GetTemplateChild("SuggestionsContainer")` from a page or
a helper, no longer compiles. Move the call into a subclass of the control, or find the
template part from outside by walking the control's visual tree with `VisualTreeHelper` (as
above) and matching `FrameworkElement.Name`. If an extension method named `GetTemplateChild`
is in scope, the call now binds to that method instead, so the build fails with an error about
its parameters (such as `CS7036`) rather than about accessibility.

### `DependencyPropertyValuePrecedences` members changed

This Uno-only enum, reachable
through the `DependencyObjectExtensions.GetValue`/`SetValue` overloads that take a
precedence, was cleaned up to mirror WinUI's `BaseValueSource`:

| Removed member | Replacement |
| --- | --- |
| `ExplicitStyle` | `Style` — 6.x already stored both explicit and implicit style values here, and 7.0 keeps one level for both, as in WinUI |
| `ImplicitStyle` | `BuiltInStyle` — despite its name, 6.x stored the values of the control's default style here |
| `DefaultStyle` | `BuiltInStyle` |
| `TemplatedParent` | none — it was never written and every read saw the default |

`Coercion`, `Animations`, `Local`, `Inheritance` and `DefaultValue` are unchanged.

### `Uno.Extensions.UriExtensions.IsLocalResource` is renamed to `IsMsAppx`

Same behavior —
the predicate has always tested for the `ms-appx` scheme, which the old name did not say.
Rename the call; there is no forwarding shim.

```diff
- if (uri.IsLocalResource())
+ if (uri.IsMsAppx())
```

### `INativeWebViewProvider` moved to `Uno.Web.WebView2.Core`

It is Uno-only, so it no longer
sits in the WinUI `Microsoft.Web.WebView2.Core` namespace. Code that refers to it by name, such
as `typeof(INativeWebViewProvider)` in an `ApiExtensibility` registration, needs
`using Uno.Web.WebView2.Core;`.

### `PrettyPrint` / `ViewExtensions.TreeGraph` are no longer public on WinAppSDK

`Uno.UI.Extensions.PrettyPrint` and `ViewExtensions` (`TreeGraph`, `FindFirstAncestor`,
`FindFirstDescendant`, …) were accidentally public in the WinAppSDK build of `Uno.WinUI` —
a guard that was meant to keep them internal there always evaluated to `false`. They are
now internal on WinAppSDK, matching the intended Skia-only public surface (they remain
public in the Skia build). There is no known WinAppSDK consumer; if you called these from
a WinAppSDK head, copy the extension methods into your own project.

### `Windows.Media.Playback.IMediaPlaybackList`

an empty interface that is not part of WinUI and
was only exposed on some targets. `MediaPlaybackList` still implements `IMediaPlaybackSource`;
use that type or `MediaPlaybackList` directly.

## Uno-only implicit conversions and static fields removed

Uno exposed a few C# shortcuts that WinUI doesn't have. Code using them compiled on Uno heads but
not on a WinAppSDK head, and the string conversions only failed at runtime. They're removed in
7.0; construct the value explicitly instead:

| Removed | Replacement |
| --- | --- |
| `Brush brush = Colors.Red;` | `new SolidColorBrush(Colors.Red)` |
| `Brush brush = "#FF0000";` | `new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(0xFF, 0xFF, 0x00, 0x00))`. To build a color from a hex string at runtime, use your own parser: WinUI has no string-to-color API. |
| `GridLength width = 42;` / `= "Auto";` | `new GridLength(42)` / `GridLength.Auto` / `new GridLength(1, GridUnitType.Star)` |
| `RowDefinition row = "Auto";` (same for `ColumnDefinition`) | `new RowDefinition { Height = GridLength.Auto }` |
| `IconElement icon = "Add";` | `new SymbolIcon(Symbol.Add)` |
| `CornerRadius radius = 4;` | `new CornerRadius(4)` |
| `CornerRadius.None` | `default(CornerRadius)` or `new CornerRadius(0)` |
| `Thickness.Empty` | `default(Thickness)` or `new Thickness(0)` |
| `Binding binding = "Path";` | `new Binding { Path = new PropertyPath("Path") }` |
| `BindingBase binding = "Path";` / `element.SetBinding(property, "Path")` | `BindingBase binding = new Binding { Path = new PropertyPath("Path") };` / `element.SetBinding(property, new Binding { Path = new PropertyPath("Path") })` |
| `string path = binding.Path;` | `binding.Path?.Path ?? ""` |

XAML is unaffected: `Background="Red"`, `Width="Auto"`, `Icon="Add"` and `{Binding}` markup
keep working. Implicit conversions that WinUI's own C# projection also has (`Duration` and
`KeyTime` from `TimeSpan`, `GridLength.Auto`, `Duration.Forever`, …) are kept.

## Collection initializers on panels, `Border` and `ScrollViewer` removed

Uno's `FrameworkElement` and `ContentControl` implemented `IEnumerable`, and `Border`, `Panel` and
`ScrollViewer` had public `Add` helpers. WinUI has neither. Together they let C# collection-initializer
syntax compile, so C# Markup-style or hand-written C# UI may use it. 7.0 removes them, so this code
is now a compile error. Set the content property instead:

| Removed | Replacement |
| --- | --- |
| `new StackPanel { a, b }` (any `Panel`) | `new StackPanel { Children = { a, b } }` |
| `new Border { child }` | `new Border { Child = child }` |
| `new ScrollViewer { content }` | `new ScrollViewer { Content = content }` |

XAML is unaffected.

## Custom `IAnimatedVisualSource` implementations

`Microsoft.UI.Xaml.Controls.IAnimatedVisualSource` was a nine-method Uno-only contract. WinUI's
is a single method, and 7.0 now matches it:

```csharp
IAnimatedVisual TryCreateAnimatedVisual(Compositor compositor, out object diagnostics);
```

`Update`, `Load`, `Unload`, `Play`, `Stop`, `Pause`, `Resume`, `SetProgress` and `Measure` are
gone from the interface. Instead of driving playback itself, a source now just hands the player
an `IAnimatedVisual` — a composition `RootVisual` plus its `Size` and `Duration` — and
`AnimatedVisualPlayer` owns play, pause, resume, stop, progress and measurement from there.
The same nine members were removed from `LottieVisualSourceBase`.

This is what unblocks WinUI Lottie code: output generated by **LottieGen**
(`LottieGen -Language CSharp -Public -WinUIVersion 3.0`) implements exactly the WinUI contract,
so it now compiles and runs against Uno Platform unchanged — no hand-editing, no Uno-specific
shim.

Only **custom** source implementations need work. Consuming a source is unaffected: XAML such as
`<AnimatedVisualPlayer><lottie:LottieVisualSource UriSource="ms-appx:///Assets/anim.json" /></AnimatedVisualPlayer>`,
and the player's own `PlayAsync`/`Pause`/`Resume`/`Stop`/`SetProgress` API, are unchanged.

To port a custom source, move the work as follows:

| Removed member | Where it goes |
| --- | --- |
| `Update(player)` | Drop it — the player no longer pushes itself into the source. |
| `Load()` / `Unload()` | Do the work in `TryCreateAnimatedVisual`; release with the returned `IAnimatedVisual`. |
| `Play` / `Stop` / `Pause` / `Resume` / `SetProgress` | Drop them — `AnimatedVisualPlayer` drives the composition animation. |
| `Measure(availableSize)` | Report the natural size as `IAnimatedVisual.Size`; the player measures from it. |

---
uid: Uno.Development.MigratingToUno7.Renames
---

# Namespace, assembly and host renames

Part of [Migrating to Uno Platform 7.0](xref:Uno.Development.MigratingToUno7).

**Read this if** your code or XAML names `Uno.UI.Toolkit`, a `Uno.UI.Runtime.Skia.*` or `Uno.WinUI.Runtime.Skia.*` type or package, or an assembly-qualified type name such as `Type.GetType("…, Uno.UI")`.

**Skip it if** the compiler finds none of these names. The two Android head classes every app has to rename are covered by [the upgrade steps](xref:Uno.Development.MigratingToUno7#upgrade-step-by-step).

## `Uno.UI.Toolkit` types move to the `Uno.UI.*` namespaces

The `Uno.UI.Toolkit` namespace — which ships inside the `Uno.WinUI` package — was routinely
mistaken for the separate [Uno Toolkit](xref:Toolkit.GettingStarted) product
(`Uno.Toolkit.UI`); the two names are a word-order swap apart. Its types now sit in the
namespace each one belongs to, alongside the rest of the framework. Type names and behavior
are unchanged, and there is **no** forwarding shim: the old namespaces stop resolving.

| Type | Before | After |
|---|---|---|
| `ElevatedView`, `CommandBarExtensions`, `SplitViewExtensions` | `Uno.UI.Toolkit` | `Uno.UI.Xaml.Controls` |
| `UIElementExtensions` | `Uno.UI.Toolkit` | `Uno.UI.Xaml` |
| `AutomationPropertiesExtensions` | `Uno.UI.Toolkit` | `Uno.UI.Xaml.Automation` |
| `ScrollBarExtensions` | `Uno.UI.Toolkit` | `Uno.UI.Xaml.Controls.Primitives` |
| `VisibleBoundsPadding` | `Uno.UI.Toolkit` | `Uno.UI.Behaviors` |
| `StorageFileHelper` | `Uno.UI.Toolkit` | `Uno.Storage` |
| input injection dev helpers | `Uno.UI.Toolkit.DevTools.Input` (and `.DevTools.Xaml`) | `Uno.UI.DevTools.Input` (and `.DevTools.Xaml`) |

XAML declarations follow the same mapping, for example
`xmlns:toolkit="using:Uno.UI.Toolkit"` becomes `xmlns:uuxc="using:Uno.UI.Xaml.Controls"` for
`ElevatedView` and the `CommandBar`/`SplitView` extensions, or
`xmlns:uub="using:Uno.UI.Behaviors"` for `VisibleBoundsPadding`.

In C#, replace `using Uno.UI.Toolkit;` with the namespace from the table. Be careful with
`using Uno.UI.Xaml.Controls;`: that namespace also contains the Uno-specific `ScrollViewer`,
`ComboBox` and `ScrollContentPresenter` attached-property holders, so importing it next to
`Microsoft.UI.Xaml.Controls` makes those three names ambiguous (`CS0104`). Import it only in
files that don't use those controls, or pin the names to the WinUI controls with aliases. This
matters most for global usings:

```xml
<ItemGroup>
  <Using Include="Uno.UI.Xaml.Controls" />
  <Using Include="Microsoft.UI.Xaml.Controls.ScrollViewer" Alias="ScrollViewer" />
  <Using Include="Microsoft.UI.Xaml.Controls.ComboBox" Alias="ComboBox" />
  <Using Include="Microsoft.UI.Xaml.Controls.ScrollContentPresenter" Alias="ScrollContentPresenter" />
</ItemGroup>
```

The Uno holders stay reachable by their full name, for example
`Uno.UI.Xaml.Controls.ScrollViewer.UpdatesModeProperty`.

The assembly that holds these types is renamed too, from `Uno.UI.Toolkit` to `Uno.UI.Extras`. It
still ships in the `Uno.WinUI` package, so package references don't change, but anything that names
the assembly does: assembly-qualified type names, and trimming or IL merge configuration.

```diff
- Type.GetType("Uno.UI.Toolkit.ElevatedView, Uno.UI.Toolkit")
+ Type.GetType("Uno.UI.Xaml.Controls.ElevatedView, Uno.UI.Extras")
```

The other Uno Platform-specific namespaces keep their names, so code using
`DiagnosticsOverlay` (`Uno.Diagnostics.UI`), `FromJsonExtension` (`Uno.UI.Markup`) or
`ColorExtensions` / `ImageHelper` (`Uno.Helpers`) needs no change.

## Runtime host renames

The platform hosts no longer depend on SkiaSharp, so "Skia" is dropped from their assembly, namespace
and public type names as well as from their packages. The remaining "Skia" names (`Uno.WinUI.Composition.Skia`,
`SKCanvasElement`, the `skia` feature) refer to the SkiaSharp drawing backend. There is **no** forwarding
shim: the old names stop resolving.

Apps using the `Uno.SDK` and the `UnoPlatformHostBuilder` chain (`UseWin32()`, `UseX11()`, `UseAndroid()`, …)
compile unchanged, because those entry points live in `Uno.UI.Hosting`. Only code that names a host type
or configures a host through its builder callback needs updating.

| Before | After |
|---|---|
| Assemblies and namespaces `Uno.UI.Runtime.Skia`, `Uno.UI.Runtime.Skia.<Platform>` | `Uno.UI.Runtime`, `Uno.UI.Runtime.<Platform>` |
| Namespaces `Uno.WinUI.Runtime.Skia.<Platform>` (X11, Linux framebuffer, Android, iOS) | `Uno.UI.Runtime.<Platform>` |
| Assemblies and namespaces `Uno.UI.MediaPlayer.Skia.<Platform>`, `Uno.UI.WebView.Skia.X11` | `Uno.UI.MediaPlayer.<Platform>`, `Uno.UI.WebView.X11` |
| `SkiaHost` | removed — the hosts derive from `Uno.UI.Hosting.UnoPlatformHost` directly |
| `MacSkiaHost` | `MacOSHost` |
| `IAndroidSkiaHostBuilder` | `IAndroidHostBuilder` |
| `IAppleUIKitSkiaHostBuilder` | `IAppleUIKitHostBuilder` |
| `Microsoft.UI.Xaml.ApplicationActivity` (Android) | `Uno.UI.Runtime.Android.ApplicationActivity` |
| `Microsoft.UI.Xaml.NativeApplication` (Android) | `Uno.UI.Runtime.Android.NativeApplication` |
| `Microsoft.UI.Xaml.Controls.NativePage` (Android) | `Uno.UI.Runtime.Android.NativePage` |

For example, `using Uno.UI.Runtime.Skia.Win32;` becomes `using Uno.UI.Runtime.Win32;`, and
`UseAndroid(b => …)` now takes an `Action<IAndroidHostBuilder>`.

The three Android types are Uno-only host types that WinUI does not have, so they leave the WinUI
namespaces, the same way the iOS `UnoUISceneDelegate` lives in `Uno.UI.Runtime.AppleUIKit`. Every
Android head names two of them, so this one does need an edit in each app:

```csharp
// Before
public class MainActivity : Microsoft.UI.Xaml.ApplicationActivity
public class Application : Microsoft.UI.Xaml.NativeApplication

// After
public class MainActivity : Uno.UI.Runtime.Android.ApplicationActivity
public class Application : Uno.UI.Runtime.Android.NativeApplication
```

A XAML-defined `NativePage` needs its `xmlns` pointed at `using:Uno.UI.Runtime.Android`.

## MRT Core moves to the `Uno.WinRT` package

The MRT Core surface — the `Microsoft.Windows.ApplicationModel.Resources` namespace — now lives in
the assembly matching its Windows App SDK home:

| | 6.x | 7.0 |
|---|---|---|
| Assembly | `Uno.UI` | `Uno.WinRT` |
| Package | `Uno.WinUI` | `Uno.WinRT` |

**Source code needs no change.** The namespace is unchanged, so
`using Microsoft.Windows.ApplicationModel.Resources;` still resolves, and `Uno.WinUI` depends on
`Uno.WinRT`, so both assemblies are already referenced. The types that moved are
`ResourceLoader`, `ResourceManager`, `ResourceContext`, `ResourceMap`, `ResourceCandidate`,
`ResourceCandidateKind`, `ResourceNotFoundEventArgs`, `KnownResourceQualifierName`,
`MrtCoreContract`, `IResourceManager`, and `IResourceContext`.

**Compiled binaries must be rebuilt.** Moving a type between assemblies is a binary break, and
there is no forwarding shim: a library built against 6.x resolves these types from `Uno.UI` and
fails at run time with

```text
Could not load type 'Microsoft.Windows.ApplicationModel.Resources.ResourceLoader'
from assembly 'Uno.UI, Version=…'.
```

Rebuild every library that uses MRT Core against 7.0, and update anything that names the
assembly explicitly — assembly-qualified type names, `TypeForwardedTo`, or an IL merge/repack
configuration:

```diff
- Type.GetType("Microsoft.Windows.ApplicationModel.Resources.ResourceLoader, Uno.UI")
+ Type.GetType("Microsoft.Windows.ApplicationModel.Resources.ResourceLoader, Uno.WinRT")
```

The older WinRT loader — `Windows.ApplicationModel.Resources.ResourceLoader`, without the
`Microsoft.` prefix — did **not** change assemblies; only the `Microsoft.Windows.*` MRT Core
surface moved. It does still need the same assembly-qualified-name update, because the WinRT
assembly it has always lived in is itself renamed `Uno` → `Uno.WinRT` in 7.0.

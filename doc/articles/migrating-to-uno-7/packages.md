---
uid: Uno.Development.MigratingToUno7.Packages
---

# Packages and Uno Features

Part of [Migrating to Uno Platform 7.0](xref:Uno.Development.MigratingToUno7).

**Read this if** you have explicit Uno Platform `PackageReference`s, list `Prism`, `Lottie` or `Skia` in `UnoFeatures`, restore with lock files, sideload a packaged Windows app, or your code uses AndroidX, `LibVLCSharp`, `SKXamlCanvas` or `Microsoft.Windows.Compatibility` directly.

**Skip it if** none of that applies: your project uses the `Uno.Sdk` with default `UnoFeatures` and no lock files, and your code only uses Uno Platform APIs.

## The rule of thumb

With the `Uno.Sdk`, Uno Platform packages are referenced for you, so most of this page needs no action. It matters when one of these is true:

- You have an explicit `PackageReference` to an Uno Platform package, or pin one of its versions.
- Your own code uses a package that used to arrive implicitly. Add your own `PackageReference` to it, unless the section below says otherwise.
- You list an Uno Feature that changed, in `UnoFeatures`.
- You build a head without the `Uno.Sdk`, or restore with a lock file.

## Renamed and removed Uno Platform packages

### `Uno.WinUI.Runtime.Skia.*` host packages renamed to `Uno.WinUI.Runtime.*`

The hosts are no longer tied to SkiaSharp: the drawing backend is chosen at run time, so the `Skia` segment is dropped. `Uno.WinUI.Runtime.Skia.Win32` becomes `Uno.WinUI.Runtime.Win32`, and likewise for `X11`, `MacOS`, `Linux.FrameBuffer`, `Headless`, `Android`, `AppleUIKit` and `WebAssembly.Browser`; the shared `Uno.WinUI.Runtime.Skia` becomes `Uno.WinUI.Runtime`. The desktop add-ins follow: `Uno.WinUI.MediaPlayer.Skia.Win32` / `.X11` become `Uno.WinUI.MediaPlayer.Win32` / `.X11`, and `Uno.WinUI.WebView.Skia.X11` becomes `Uno.WinUI.WebView.X11`. With the `Uno.SDK` these are referenced implicitly, so there is nothing to change. Replace any explicit `PackageReference` to an old id (typically `Uno.WinUI.Runtime.Skia.Headless`) with the new one; the old ids receive no 7.0 builds. The assemblies, namespaces and a few public types are renamed the same way — see [Runtime host renames](xref:Uno.Development.MigratingToUno7.Renames#runtime-host-renames).

### `Uno.WinUI.WebAssembly` package removed (and the older `Uno.WinUI.Runtime.WebAssembly`)

Use `Uno.WinUI.Runtime.WebAssembly.Browser`. The UI renders to a canvas; there is no DOM tree. With the `Uno.SDK`, the browser head is referenced implicitly — there is nothing to add.

> [!NOTE]
> Referencing `Uno.WinUI.WebAssembly` (or the older `Uno.WinUI.Runtime.WebAssembly`)
> alongside the Skia browser head raises the `UNOB0017` build diagnostic. Removing the
> explicit reference resolves it.

See also: [Native element hosting](xref:Uno.Development.MigratingToUno7.RemovedApis#native-element-hosting), [WebAssembly interop](xref:Uno.Development.MigratingToUno7.RemovedApis#webassembly-interop).

### `Uno.WinUI.Skia.X11`, `Uno.WinUI.Skia.MacOS`, and `Uno.WinUI.Skia.Linux.FrameBuffer` bootstrapper packages removed

These were empty meta-packages that only redirected to the real head. With the `Uno.SDK`, remove the reference — the matching `Uno.WinUI.Runtime.*` head is referenced implicitly for executable heads. For a hand-rolled (non-`Uno.SDK`) head, replace it with the corresponding `Uno.WinUI.Runtime.<variant>` package.

See also: [The `Uno.Sdk` is required for applications](xref:Uno.Development.MigratingToUno7.ProjectSetup#the-unosdk-is-required-for-applications).

### `Uno.UI.BindingHelper.Android` assembly removed

Remove the reference; Skia-on-Android needs no Java/JNI binding.

### `Uno.UI.FluentTheme.v1` assembly removed

The Fluent Design **V1** styles were deleted several releases ago and the assembly has shipped empty since. `Uno.UI.FluentTheme` still ships and is what `XamlControlsResources` has always loaded, so there is nothing to change unless you referenced the V1 types directly — see [Fluent Design resource-version types](xref:Uno.Development.MigratingToUno7.RemovedApis#fluent-design-resource-version-types).

### `Uno.UI.FluentTheme.v2` assembly merged into `Uno.UI.FluentTheme`

With V1 gone there is a single set of Fluent styles, so the two assemblies were collapsed into one. All the styles ship in `Uno.UI.FluentTheme`; both assemblies come from the `Uno.WinUI` package, so no reference changes. Only a direct reference to the `Uno.UI.FluentTheme.v2` assembly, or to `XamlControlsResourcesV2`, needs updating.

See also: [The Fluent theme-resources dictionary](xref:Uno.Development.MigratingToUno7.Xaml#the-fluent-theme-resources-dictionary-is-now-themeresourcesxaml).

### `Uno.WinUI.Lottie` package and `Lottie` Uno Feature removed

`AnimatedVisualPlayer`, `LottieVisualSource` and `ThemableLottieVisualSource` are part of `Uno.WinUI`, and the Skottie renderer now comes with the Skia renderer, so Lottie needs no package. Remove any explicit `PackageReference` to `Uno.WinUI.Lottie`; it receives no 7.0 builds. The `Lottie` feature is ignored if still listed: it never added a Lottie package on Windows, so a project that also targets Windows still references [`CommunityToolkit.WinUI.Lottie`](https://www.nuget.org/packages/CommunityToolkit.WinUI.Lottie) for that target itself, and the build reports [UNOB0034](xref:Build.Solution.error-codes#unob0034-lottie-xaml-on-winappsdk-needs-the-communitytoolkitwinuilottie-package) when it doesn't. See [Lottie](xref:Uno.Features.Lottie).

See also: [Custom `IAnimatedVisualSource` implementations](xref:Uno.Development.MigratingToUno7.RemovedApis#custom-ianimatedvisualsource-implementations), [`ProgressRing` asset flags](xref:Uno.Development.MigratingToUno7.FeatureFlags#progressringprogressringasset-and-determinateprogressringasset).

### `Uno.UI.Maps` AddIn removed

The native Google Maps control has no core Skia equivalent — use a third-party/Skia map or custom rendering.

### `Uno.WinUI` no longer ships `net*-android/ios/tvos/maccatalyst` UI assemblies

The package's `lib` folder holds only `netX.0` (the Skia build, used by every target framework except the WinAppSDK one) and `netX.0-windows10.0.19041.0`. Your projects keep their platform target frameworks; they all compile against the `netX.0` assemblies. Libraries built against 6.x are binary-incompatible, so recompile all of them against 7.0 and remove native bootstrap. See [Platform targeting in multi-targeted libraries](xref:Uno.Development.MigratingToUno7.Libraries#platform-targeting-in-multi-targeted-libraries) for which target frameworks a library keeps.

### Runtime hosts no longer carry SkiaSharp or HarfBuzz native assets

The browser host linked `libSkiaSharp.a` into every WebAssembly head, so a WebGPU-only app shipped about 6 MB of Skia it never used, and every `Uno.WinUI.Runtime.*` host referenced HarfBuzzSharp whichever font provider the app used. The native libraries now come with the head: with the `Uno.Sdk`, nothing changes, because it references the `HarfBuzzSharp.NativeAssets.*` package for each target, and the `SkiaSharp.NativeAssets.*` package when the `Skia` feature is on. Listing `WebGpu` without `Skia` turns `Skia` off, so an app that uses SkiaSharp itself, or links native code against SkiaSharp's native library, lists `Skia;WebGpu`. A head that doesn't use the `Uno.Sdk` adds those `PackageReference`s itself, for every target it builds.

See also: [SkiaSharp 4 is the default](xref:Uno.Development.MigratingToUno7.Behavior#skiasharp-4-is-the-default).

### `Uno.UI.Toolkit` types moved to the `Uno.UI.*` namespaces

The old name was routinely confused with the separate Uno Toolkit (`Uno.Toolkit.UI`). Each type now sits in the namespace it belongs to — see [the mapping table](xref:Uno.Development.MigratingToUno7.Renames#unouitoolkit-types-move-to-the-unoui-namespaces). Type names and behavior are unchanged. `Uno.Diagnostics.UI`, `Uno.UI.Markup`, `Uno.Helpers` and `Uno.UI.Maps` are unaffected — only the `Uno.UI.Toolkit*` namespaces moved.

> [!IMPORTANT]
> The `Uno.UI.Toolkit` namespace move is a hard rename — there is no type-forwarder and no
> `xmlns` alias, so a library compiled against Uno 6.x that uses those types no longer binds.
> Recompile every dependent library against 7.0 rather than mixing majors.

## Uno Features

### `Skia` Uno Feature off on Windows (WinAppSDK)

`Skia` is still implied on every Uno Platform target of an app that lists no renderer feature (`WebGpu` counts as one), but it is off on Windows, even when `UnoFeatures` lists it or `SkiaRenderer`. The Windows target no longer references `SkiaSharp.Views.WinUI`, `Uno.WinUI.Graphics2DSK` or, with the `Toolkit` feature, `Uno.Toolkit.Skia.WinUI`, and ships none of the SkiaSharp and ANGLE native libraries. If code built for Windows uses [`SKCanvasElement`](xref:Uno.Controls.SKCanvasElement), `SKXamlCanvas`, Toolkit Skia controls such as `ShadowContainer` or SkiaSharp itself, the build now fails until you list `Skia` and set `<UnoEnableSkiaOnWinAppSdk>true</UnoEnableSkiaOnWinAppSdk>`. The PDBs of those native libraries are no longer copied to the output in either case.

### `Skia` Uno Feature no longer implied on libraries

The renderer is the app's choice, so a library no longer gets the implied `Skia` feature, and a legacy `SkiaRenderer` entry is ignored there. Its package no longer depends on `Uno.WinUI.Composition.Skia` and `Uno.WinUI.Graphics2DSK`, nor through them on SkiaSharp. A library that uses [`SKCanvasElement`](xref:Uno.Controls.SKCanvasElement), the `SkiaExtensions` conversions or Toolkit Skia controls such as `ShadowContainer` adds `Skia` to `UnoFeatures`; the build reports [UNOB0030](xref:Build.Solution.error-codes#unob0030-the-skiarenderer-uno-feature-is-ignored-in-a-library) while a library still lists `SkiaRenderer`. Apps using the `Uno.Sdk` are unchanged. A project is an app when its project file sets `OutputType` to `Exe` or `WinExe`, or sets `IsUnoHead` to `true`; a target framework where it doesn't, or an `OutputType` set later, for example in `Directory.Build.targets`, is built as a library. A head that doesn't take the `Uno.Sdk` implicit packages, and got `Uno.WinUI.Composition.Skia` only through such a library, references it itself; otherwise it fails at startup with *No drawing backend could be resolved*.

See also: [Which target frameworks should a library keep](xref:Uno.Development.MigratingToUno7.Libraries#which-target-frameworks-should-a-library-keep).

### `Prism` Uno Feature removed

The `Uno.Sdk` no longer references Prism. The Prism packages published so far (`Prism.Uno.WinUI` 9.0.537 and earlier) were built against Uno Platform 5 and don't build with 7.0. Remove `Prism` from `UnoFeatures`, and add a `PackageReference` to `Prism.DryIoc.Uno.WinUI` in the app (or `Prism.Uno.WinUI` in a library), with a Prism release built for Uno Platform 7.0. The build reports [UNOB0035](xref:Build.Solution.error-codes#unob0035-the-prism-uno-feature-is-no-longer-supported) while `UnoFeatures` still lists `Prism`, and the `PrismVersion` property is silently ignored. A `Navigation` entry next to `Prism` now references `Uno.Extensions.Navigation`, which the `Prism` feature used to leave out.

### `webview` Uno Feature only brings the native WebView2 loader (Desktop)

The implicit `Microsoft.Web.WebView2` reference is now `IncludeAssets="native"`: it ships `WebView2Loader.dll`, which the `WebView2Aot` backend needs, but no longer the managed `Microsoft.Web.WebView2.Core`/`.WinForms` assemblies and their XML docs, and no longer the `mswebview2` extern alias. The `UnoWebView2Aliases` property is removed and is silently ignored if still set. If your own code uses the `Microsoft.Web.WebView2` managed API, add an explicit `PackageReference` with `Aliases` (for example `<PackageReference Include="Microsoft.Web.WebView2" Aliases="mswebview2" />`); the Uno SDK applies that alias to `Microsoft.Web.WebView2.Core` so it doesn't clash with Uno's own `Microsoft.Web.WebView2.Core` types.

### `Uno.HotTesting.Reactive` referenced implicitly by MVUX in non-optimized builds

Projects using the `MVUX` feature, class libraries included, now restore MVUX feed mocks when `$(Optimize)` is not `true`, so the Debug/Release lock-file and `--no-restore` guidance of [the `Uno.UI.HotDesign` section](#unouihotdesign-unouiappmcp-mcp-support-and-unowinuidevserver-no-longer-resolve-implicitly-in-release-builds) applies to them too. Code using its types must only compile in non-optimized builds. Set `UnoDisableHotTesting` to `true` to opt out; see [MVUX feed mocks](xref:Uno.Features.Uno.Sdk#mvux-feed-mocks-in-non-optimized-builds).

## Packages no longer referenced implicitly

### `Xamarin.AndroidX.Legacy.Support.V4` no longer injected (Android)

The `Uno.Sdk` added it to every Android app head. It is the AndroidX wrapper for the Android Support Library v4, whose job was bridging pre-AndroidX *view* code — with the native Android Views renderer gone, nothing needs it. Ten packages leave the graph: `Legacy.Support.V4`, `Legacy.Support.Core.UI`, `Legacy.Support.Core.Utils`, `AsyncLayoutInflater`, `LocalBroadcastManager`, `Media`, `Print`, `SlidingPaneLayout`, `Window` and its `Window.*Core*` companions. Nothing changes version. `AndroidX.Window` still arrives with the `foldable` feature. If *your own* code uses any of these, add an explicit `PackageReference`. The `AndroidXLegacySupportV4Version` property is removed and is silently ignored if still set.

### `Xamarin.AndroidX.*` transitive deps removed (AppCompat, RecyclerView, Activity, Browser, SwipeRefreshLayout)

If *your own* code uses AndroidX, add explicit `PackageReference`s.

### `SkiaSharp.Views.Uno.WinUI` no longer referenced implicitly

The `Uno.Sdk` used to add it to every Uno Platform target, and to WebAssembly heads using the `lottie`, `svg`, `material`, `cupertino`, or `simpletheme` features. Nothing in Uno Platform needs it anymore — SVG draws through `Uno.WinUI.Graphics2DSK` and Lottie through `SkiaSharp.Skottie`. If *your own* code uses `SKXamlCanvas` or `SKSwapChainPanel`, switch to [`SKCanvasElement`](xref:Uno.Controls.SKCanvasElement), which is hardware-accelerated and referenced implicitly. Do **not** add an explicit `PackageReference` to `SkiaSharp.Views.Uno.WinUI`: every published build was compiled against Uno Platform 6.x assemblies that 7.0 no longer ships, so the app throws `FileNotFoundException` from `SkiaSharp.Views.Windows.GlobalStaticResources.Initialize()` at startup.

See also: [SkiaSharp 4 is the default](xref:Uno.Development.MigratingToUno7.Behavior#skiasharp-4-is-the-default).

### `Microsoft.Windows.Compatibility` no longer referenced implicitly (WebAssembly)

The `Uno.Sdk` added this .NET Framework porting meta-package to every WebAssembly executable, pulling in 56 extra packages — 47% of a blank app's restore graph — including 21 RID-specific native packages for a target that has no RIDs. It arrived as a .NET 5 migration workaround and nothing in Uno Platform uses it. Most of what it provides is Windows-only and throws `PlatformNotSupportedException` in a browser regardless. If *your own* code uses one of its assemblies, add an explicit `PackageReference` to that specific package — the ones that genuinely work in a browser are `System.ServiceModel.*`, `System.ServiceModel.Syndication`, `System.Runtime.Caching`, `System.IO.Packaging`, `System.Configuration.ConfigurationManager`, `System.ComponentModel.Composition`, `System.CodeDom`, `System.Reflection.Context` and `System.Security.Cryptography.Pkcs`/`.Xml` — or reference `Microsoft.Windows.Compatibility` itself. The `WindowsCompatibilityVersion` MSBuild property is removed with it; specify a version on your own `PackageReference`.

### `LibVLCSharp` no longer referenced implicitly

`Uno.WinUI.Runtime.X11` — which every desktop head references implicitly — carried a `LibVLCSharp` dependency it never used, so the managed assembly landed in every desktop app's output. It now arrives only with the `MediaPlayerElement` (or `MediaElement`) feature, through `Uno.WinUI.MediaPlayer.X11` / `Uno.WinUI.MediaPlayer.Win32`, which have always declared it themselves. `MediaPlayerElement` is unaffected — it already required that feature. If *your own* code uses `LibVLCSharp` types directly, add an explicit `PackageReference`.

### `Uno.UniversalImageLoader` no longer injected (Android)

Skia handles image loading internally. If you initialized it manually, remove the `ConfigureUniversalImageLoader();` call. The `UnoUniversalImageLoaderVersion` property is removed and is silently ignored if still set.

### `Uno.UI.HotDesign`, `Uno.UI.App.Mcp` (MCP support) and `Uno.WinUI.DevServer` no longer resolve implicitly in Release builds

This covers [`Uno.UI.HotDesign`](xref:Uno.HotDesign.Overview), `Uno.UI.App.Mcp` ([MCP support](xref:Uno.Features.Uno.MCPs)) and [`Uno.WinUI.DevServer`](xref:Uno.DevServer).

These development-time packages were nominally "excluded" from Release restores via `Exclude="all" IncludeAssets="None"`, which never actually worked — `Exclude` is not a package-id filter and `IncludeAssets="None"` still resolves, downloads and audits the whole closure. The implicit references are now genuinely absent when `$(Optimize) == 'true'`; an explicit `PackageReference` with a version that you add yourself is still honored in every configuration. If your project restores with `packages.lock.json` and `RestoreLockedMode=true`, give each configuration its own lock file (`<NuGetLockFilePath>packages.$(Configuration).lock.json</NuGetLockFilePath>`), regenerate both after upgrading, and restore the matching configuration before any `--no-restore` build — a single lock file no longer satisfies both Debug and Release. `UnoDisableHotDesign` and `UnoDisableMCPSupport` still suppress their respective implicit package reference in any configuration; `Uno.WinUI.DevServer` has no such switch, since it is gated by `$(Optimize)` alone.

## Windows

### Windows App SDK minimum raised from 1.x to 2.4.0

Windows heads now build against Windows App SDK 2.x, so packaged apps take a framework dependency on `Microsoft.WindowsAppRuntime.2` and end users need the matching [Windows App Runtime](https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads) — 2.4.0 or later from the **Stable release** section — installed. `Uno.WinUI` 7.0 requires `Microsoft.WindowsAppSDK` 2.4.0 or later (and `Microsoft.Windows.SDK.BuildTools` 10.0.28000.2526 or later) for its Windows target frameworks, so a Windows head cannot stay on 1.x: pinning `<WinAppSdkVersion>` to a 1.x version fails restore with `NU1605`. Use 2.4.0 or later.

See also: [Minimum OS versions raised](xref:Uno.Development.MigratingToUno7.ProjectSetup#minimum-os-versions-raised).

### Packaged Windows (WinAppSDK) heads no longer bundle the Windows App SDK

`WindowsAppSDKSelfContained` now defaults to `true` only for unpackaged heads (`WindowsPackageType` `None`). An MSIX head declares the Windows App Runtime as a framework dependency instead, which makes it about 75 MB smaller. The Microsoft Store installs that dependency for you. If you sideload the package, install the [Windows App Runtime](https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads) on the target machine, or set `<WindowsAppSDKSelfContained>true</WindowsAppSDKSelfContained>` to keep bundling it.

### `Microsoft.Web.WebView2` WebView backend removed (Windows)

`WebView2Aot` is now the only backend. Remove any `Environment.SetEnvironmentVariable("UNO_WEBVIEW2_BACKEND", "microsoft.web.webview2")` call — the variable is still read, but any value other than `webview2aot` now only logs a warning and `WebView2Aot` is used regardless. The `UnoUseWebView2WPF` opt-in is removed with it — it kept the `Microsoft.Web.WebView2.Wpf` reference for the WPF host, which is gone. Set `<UnoDisableWebView2Workarounds>true</UnoDisableWebView2Workarounds>` instead if your own `Microsoft.Web.WebView2` reference needs that assembly (it also turns off the package-alias workaround for `Microsoft.Web.WebView2.Core`).

See also: [`INativeWebViewProvider` moved](xref:Uno.Development.MigratingToUno7.RemovedApis#inativewebviewprovider-moved-to-unowebwebview2core).

## Build properties and asset schemes

### `bundle://` image scheme removed (iOS/tvOS)

The native UIKit bundle-resource loader is gone with the native rendering layer. Use `ms-appx:///` assets instead — see [Working with assets](xref:Uno.Features.Assets).

See also: [Relative URIs in XAML](xref:Uno.Development.MigratingToUno7.Xaml#relative-uris), [Retargeted drawables](xref:Uno.Development.MigratingToUno7.Behavior#retargeted-drawables-are-renamed).

### `UnoDragDropExternalSupport` trimming property removed

It could only trim the native WebAssembly drag-and-drop implementation, which is gone. On WebAssembly, drag and drop from outside the app is now handled by the browser host and is always available. The property is silently ignored if still set.

### `UnoEnableAlcAppSupport` is now a trimming switch (default `false`)

Trimmed builds now stub `Application.HasSecondaryApps` to `false`, dropping the ALC-aware resource resolution that secondary `Application` instances in their own `AssemblyLoadContext` depend on. Set `<UnoEnableAlcAppSupport>true</UnoEnableAlcAppSupport>` if your app hosts other Uno apps in separate ALCs.

## Related pages

- [Namespace, assembly and host renames](xref:Uno.Development.MigratingToUno7.Renames) for the assemblies and namespaces renamed along with the packages.
- [Project format, target frameworks and OS versions](xref:Uno.Development.MigratingToUno7.ProjectSetup) for target frameworks and the `Uno.Sdk`.
- [Libraries and conditional code](xref:Uno.Development.MigratingToUno7.Libraries) if you publish a package.
- [Errors, symptoms and code search](xref:Uno.Development.MigratingToUno7.Lookup) to go from an error or symptom to its fix.
- [Upgrade steps](xref:Uno.Development.MigratingToUno7) to return to the main path.

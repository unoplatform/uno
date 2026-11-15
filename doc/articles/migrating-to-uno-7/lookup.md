---
uid: Uno.Development.MigratingToUno7.Lookup
---

# Errors, symptoms and code search

Part of [Migrating to Uno Platform 7.0](xref:Uno.Development.MigratingToUno7). Use this page to jump straight from a build error, a symptom or a piece of your code to the section that explains it.

## Build errors and warnings

Look up the code or message you got, then follow the link for the fix.

| Code or message | Cause | Details |
|---|---|---|
| `NU1202` | A project still targets `net9.0-*` | [.NET 9 is no longer supported](xref:Uno.Development.MigratingToUno7.ProjectSetup#net-9-is-no-longer-supported) |
| `NU1605` | `WinAppSdkVersion` is pinned to 1.x | [Windows App SDK minimum](xref:Uno.Development.MigratingToUno7.Packages#windows-app-sdk-minimum-raised-from-1x-to-240) |
| `CS1705` on `WinRT.Runtime` | `WindowsSdkPackageVersion` selects a projection older than `.87` | [Windows SDK projection](xref:Uno.Development.MigratingToUno7.ProjectSetup#the-windows-sdk-projection-is-picked-by-the-unosdk) |
| `MSB4100` | A condition uses the removed `IsMacCatalyst` or `IsIOSOrCatalyst` | [Mac Catalyst is removed](xref:Uno.Development.MigratingToUno7.ProjectSetup#mac-catalyst-is-removed) |
| `CS0104` on `ScrollViewer`, `ComboBox` or `ScrollContentPresenter` | `using Uno.UI.Xaml.Controls;` next to the WinUI namespace | [`Uno.UI.Toolkit` types](xref:Uno.Development.MigratingToUno7.Renames#unouitoolkit-types-move-to-the-unoui-namespaces) |
| `ThumbnailMode` not found | `Windows.Storage.ThumbnailMode` is removed | [`Windows.Storage.ThumbnailMode`](xref:Uno.Development.MigratingToUno7.WinUIParity#windowsstoragethumbnailmode-is-removed) |
| `CS7036` on `GetTemplateChild` | `Control.GetTemplateChild` is no longer public, so an extension method bound instead | [`Control.GetTemplateChild`](xref:Uno.Development.MigratingToUno7.RemovedApis#controlgettemplatechild-is-no-longer-public) |
| `CS1729` or `CS1503` on `new DataTemplate(`, `new ControlTemplate(` or `new ItemsPanelTemplate(` | The `Func<View>` and `(owner, builder)` template constructors are removed | [Templates can no longer be built from a factory delegate](xref:Uno.Development.MigratingToUno7.WinUIParity#templates-can-no-longer-be-built-from-a-factory-delegate) |
| `UNOB0017` | Reference to `Uno.WinUI.WebAssembly` or `Uno.WinUI.Runtime.WebAssembly` | [`Uno.WinUI.WebAssembly`](xref:Uno.Development.MigratingToUno7.Packages#unowinuiwebassembly-package-removed-and-the-older-unowinuiruntimewebassembly) |
| `UNOB0020` | A multi-targeted library was built against 6.x | [Platform targeting](xref:Uno.Development.MigratingToUno7.Libraries#platform-targeting-in-multi-targeted-libraries) |
| `UNOB0022` | The `SingleProject` property | [The `Uno.Sdk` is required](xref:Uno.Development.MigratingToUno7.ProjectSetup#the-unosdk-is-required-for-applications) |
| `UNOB0023` | A runtime-enabled package provided no runtime assembly | [Runtime identifier properties](xref:Uno.Development.MigratingToUno7.Libraries#runtime-identifier-properties-removed), [Cross-runtime libraries](xref:Uno.Development.MigratingToUno7.Libraries#cross-runtime-libraries) |
| `UNOB0024` | `UnoRuntimeIdentifier` or a sibling property is set | [Runtime identifier properties](xref:Uno.Development.MigratingToUno7.Libraries#runtime-identifier-properties-removed) |
| `UNOB0025` | Runtime-enabled packages without a runtime host | [Runtime identifier properties](xref:Uno.Development.MigratingToUno7.Libraries#runtime-identifier-properties-removed) |
| `UNOB0026` | A referenced assembly was built for a native renderer | [Runtime identifier properties](xref:Uno.Development.MigratingToUno7.Libraries#runtime-identifier-properties-removed) |
| `UNOB0027` | A removed file suffix such as `*.Apple.cs` | [Removed file suffixes](xref:Uno.Development.MigratingToUno7.Libraries#removed-file-suffixes) |
| `UNOB0028` | `UnoSplashScreenColor` | [`UnoSplashScreenColor` removed](xref:Uno.Development.MigratingToUno7.ProjectSetup#unosplashscreencolor-removed) |
| `UNOB0029` | An app head doesn't use the `Uno.Sdk` | [The `Uno.Sdk` is required](xref:Uno.Development.MigratingToUno7.ProjectSetup#the-unosdk-is-required-for-applications) |
| `UNOB0030` | A library lists `SkiaRenderer` | [`Skia` on libraries](xref:Uno.Development.MigratingToUno7.Packages#skia-uno-feature-no-longer-implied-on-libraries) |
| `UNOB0033` | A leftover `Platforms/MacCatalyst` folder | [Mac Catalyst is removed](xref:Uno.Development.MigratingToUno7.ProjectSetup#mac-catalyst-is-removed) |
| `UNOB0034`, `WMC0001: Unknown type 'LottieVisualSource'` | Lottie XAML on a Windows target without `CommunityToolkit.WinUI.Lottie` | [Lottie](xref:Uno.Development.MigratingToUno7.Packages#unowinuilottie-package-and-lottie-uno-feature-removed) |
| `UNOB0035` | `UnoFeatures` lists `Prism` | [`Prism` Uno Feature removed](xref:Uno.Development.MigratingToUno7.Packages#prism-uno-feature-removed) |
| `UXAML0001` | Binding an attached property whose owner type can't be found | [Attached property owners](xref:Uno.Development.MigratingToUno7.Xaml#binding-an-attached-property-whose-owner-type-cannot-be-found-is-a-build-error) |
| `UXAML0006` | A `clr-namespace:` declaration | [`clr-namespace:` xmlns form](xref:Uno.Development.MigratingToUno7.Xaml#the-wpf-style-clr-namespace-xmlns-form-is-no-longer-accepted) |
| `UXAML0007` | A removed conditional XAML prefix such as `skia:` | [Removed XAML prefixes](xref:Uno.Development.MigratingToUno7.Libraries#removed-xaml-prefixes) |
| `Uno0001` | A not-implemented API, which can fail builds that set `TreatWarningsAsErrors` | [`Uno0001`](xref:Uno.Development.MigratingToUno7.Libraries#the-not-implemented-warning-uno0001-follows-the-target-framework) |
| `Property 'AreDimensionsConstrained' does not exist` | Layout-constraint members are removed | [Layout-constraint optimization](xref:Uno.Development.MigratingToUno7.RemovedApis#layout-constraint-optimization) |
| XAML build failure on `IsNativeStyle` | The attribute is no longer recognized | [Native-style declaration](xref:Uno.Development.MigratingToUno7.RemovedApis#native-style-declaration) |
| XAML build failure on `ControlsResourcesVersion` | The member is removed | [Fluent resource-version types](xref:Uno.Development.MigratingToUno7.RemovedApis#fluent-design-resource-version-types) |

## Failures at run time

| Symptom | Cause | Details |
|---|---|---|
| *No drawing backend could be resolved* at startup | A head without the `Uno.Sdk` implicit packages got `Uno.WinUI.Composition.Skia` only through a library | [`Skia` on libraries](xref:Uno.Development.MigratingToUno7.Packages#skia-uno-feature-no-longer-implied-on-libraries) |
| `FileNotFoundException` from `SkiaSharp.Views.Windows.GlobalStaticResources.Initialize()` | Explicit `SkiaSharp.Views.Uno.WinUI` reference | [`SkiaSharp.Views.Uno.WinUI`](xref:Uno.Development.MigratingToUno7.Packages#skiasharpviewsunowinui-no-longer-referenced-implicitly) |
| `Could not load type … from assembly 'Uno.UI'` on an MRT Core type | A library or assembly-qualified name still targets 6.x | [MRT Core](xref:Uno.Development.MigratingToUno7.Renames#mrt-core-moves-to-the-unowinrt-package) |
| Failures in code from a multi-targeted library | A stale `net*-ios` or `net*-android` asset built against 6.x is now deployed | [Platform targeting](xref:Uno.Development.MigratingToUno7.Libraries#platform-targeting-in-multi-targeted-libraries) |
| `(MyEnum)GetValue(...)` throws | Custom value-type dependency properties now default to `null` | [Custom value types default to `null`](xref:Uno.Development.MigratingToUno7.Behavior#custom-value-types-default-to-null) |
| `XamlParseException` from `XamlReader.Load` or Hot Reload | A `clr-namespace:` declaration | [`clr-namespace:` xmlns form](xref:Uno.Development.MigratingToUno7.Xaml#the-wpf-style-clr-namespace-xmlns-form-is-no-longer-accepted) |
| Crash on launch on Android, where the app used to start degraded | Startup failures now propagate | [Android head uses the host builder](xref:Uno.Development.MigratingToUno7.AppStartup#android-head-uses-the-host-builder) |
| Failure at run time after pinning SkiaSharp | Managed and native SkiaSharp packages at different versions | [SkiaSharp 4 is the default](xref:Uno.Development.MigratingToUno7.Behavior#skiasharp-4-is-the-default) |

## Changes the compiler won't catch

These build cleanly and only show up when you run the app.

| What you notice | Why | Details |
|---|---|---|
| iOS or tvOS settings seem to have disappeared after updating | Settings moved to a new store and aren't migrated for you | [Application settings](xref:Uno.Development.MigratingToUno7.AppStartup#application-settings-on-ios-and-tvos) |
| `ItemClickCommand="{Binding}"` or `Command="{Binding}"` on a list does nothing | The XAML still compiles, but the property is gone | [`ItemClickCommand`](xref:Uno.Development.MigratingToUno7.RemovedApis#listviewbaseitemclickcommand-and-listviewbasecommand) |
| Markup under a `skia:` or `not_skia:` style prefix applies everywhere, or nowhere | A removed prefix is treated as an ordinary namespace | [Removed XAML prefixes](xref:Uno.Development.MigratingToUno7.Libraries#removed-xaml-prefixes) |
| `#if __IOS__` or `ios:` code in a library runs for the first time | Platform assets of a library are now deployed | [Platform targeting](xref:Uno.Development.MigratingToUno7.Libraries#platform-targeting-in-multi-targeted-libraries) |
| A `*.skia.cs` file compiles on mobile and WebAssembly too | The suffix now covers every target except WinAppSDK | [Platform targeting](xref:Uno.Development.MigratingToUno7.Libraries#platform-targeting-in-multi-targeted-libraries) |
| Number and date formats don't follow `PrimaryLanguageOverride` until restart | The setter no longer swaps the culture | [`UseLegacyPrimaryLanguageOverride`](xref:Uno.Development.MigratingToUno7.FeatureFlags#applicationlanguagesuselegacyprimarylanguageoverride) |
| The backdrop material is hidden | Backgrounds are no longer made transparent for you | [`SystemBackdrop`](xref:Uno.Development.MigratingToUno7.Behavior#systembackdrop-no-longer-rewrites-your-contents-backgrounds) |
| A custom `ContentControl` renders nothing | A control with no template no longer bypasses the `ContentPresenter` | [`ContentControl` subclasses](xref:Uno.Development.MigratingToUno7.Behavior#contentcontrol-subclasses-without-a-controltemplate) |
| A selected list item isn't scrolled into view | Only a focused list brings a new selection into view | [List selection](xref:Uno.Development.MigratingToUno7.Behavior#a-new-selection-is-brought-into-view-only-while-the-list-has-focus) |
| A long press no longer clicks or selects | A touch or pen hold is no longer a tap | [Holds](xref:Uno.Development.MigratingToUno7.Behavior#a-touch-or-pen-hold-is-no-longer-a-tap) |
| A drag or splitter lags the pointer slightly | No `ManipulationDelta` right after `ManipulationStarted` | [Manipulation deltas](xref:Uno.Development.MigratingToUno7.Behavior#no-manipulationdelta-immediately-after-manipulationstarted) |
| A page loses its state on Back | Pages follow `NavigationCacheMode`, if the app set `UseWinUIBehavior` to `false` | [`Frame.UseWinUIBehavior`](xref:Uno.Development.MigratingToUno7.FeatureFlags#frameusewinuibehavior-and-pageispoolingenabled) |
| Content draws under the Android status bar | Apps are always edge-to-edge | [`IsEdgeToEdgeEnabled`](xref:Uno.Development.MigratingToUno7.FeatureFlags#androidsettingsisedgetoedgeenabled) |
| A popup no longer closes on outside tap, or content alignment changed | A compatibility flag the app set is gone | [Compatibility flags](xref:Uno.Development.MigratingToUno7.FeatureFlags#cross-platform-compatibility-flags), [`UseLegacyContentAlignment`](xref:Uno.Development.MigratingToUno7.FeatureFlags#controluselegacycontentalignment) |
| An MSIX package installed outside the Microsoft Store fails to install or start | Packaged heads depend on the Windows App Runtime instead of bundling it | [Packaged Windows heads](xref:Uno.Development.MigratingToUno7.Packages#packaged-windows-winappsdk-heads-no-longer-bundle-the-windows-app-sdk) |
| `Suspending` no longer fires on desktop | Skia Desktop doesn't report suspension | [App lifecycle events](xref:Uno.Development.MigratingToUno7.AppStartup#app-lifecycle-events-move-to-coreapplication) |
| `OnLaunched` sees `Launch` for a URL or shortcut launch on iOS | A cold-start activation arrives after `OnLaunched` | [Activation](xref:Uno.Development.MigratingToUno7.AppStartup#activation-is-read-from-appinstance) |
| A relative `WebView2.Source` or `NavigateUri` silently fails | Relative URIs now compile to `ms-resource:///Files/…` | [Relative URIs](xref:Uno.Development.MigratingToUno7.Xaml#a-relative-uri-on-a-uri-typed-property-now-compiles-to-ms-resourcefiles) |
| Fluent controls have no template | They come only from `XamlControlsResources` | [Fluent styles](xref:Uno.Development.MigratingToUno7.Xaml#styles-that-winui-ships-in-xamlcontrolsresources-now-come-only-from-there) |
| Letterbox bars are transparent, not black | `MediaPlayerElement` no longer paints a backdrop | [`MediaPlayerPresenter`](xref:Uno.Development.MigratingToUno7.WinUIParity#mediaplayerpresenter) |
| Rendering differences or more memory on some GPUs | Vulkan is the default on Android, Linux and Windows | [Vulkan](xref:Uno.Development.MigratingToUno7.Behavior#vulkan-is-the-default-rendering-backend) |
| An Android drawable no longer resolves | Retargeted drawables keep their file extension | [Retargeted drawables](xref:Uno.Development.MigratingToUno7.Behavior#retargeted-drawables-are-renamed), [`ms-appx:///` lookups](xref:Uno.Development.MigratingToUno7.Behavior#drawables-you-add-under-platformsandroidresourcesdrawable-no-longer-resolve-through-ms-appx) |
| A WebAssembly sign-in timeout looks different | A timeout returns `ErrorHttp` instead of `UserCancel` | [`WebAuthenticationBroker`](xref:Uno.Development.MigratingToUno7.Behavior#a-timeout-returns-errorhttp-instead-of-usercancel) |

## Search your code

Each row is a literal string to search for across your C#, XAML and MSBuild files. Any match means that section applies to you, so this is the quickest way to scope an upgrade by hand or with an AI agent. The list covers the most common names rather than every change, so the compiler still has the last word.

| Search for | Go to |
|---|---|
| `Uno.UI.Toolkit` | [`Uno.UI.Toolkit` types](xref:Uno.Development.MigratingToUno7.Renames#unouitoolkit-types-move-to-the-unoui-namespaces) |
| `Runtime.Skia`, `MediaPlayer.Skia`, `WebView.Skia`, `SkiaHost`, `SkiaHostBuilder` | [Runtime host renames](xref:Uno.Development.MigratingToUno7.Renames#runtime-host-renames) |
| `Microsoft.UI.Xaml.NativeApplication`, `Microsoft.UI.Xaml.ApplicationActivity`, `NativePage`, `AppBuilder` | [Runtime host renames](xref:Uno.Development.MigratingToUno7.Renames#runtime-host-renames), [Android head](xref:Uno.Development.MigratingToUno7.AppStartup#android-head-uses-the-host-builder) |
| `Microsoft.Windows.ApplicationModel.Resources` in a string, `, Uno.UI"`, `, Uno"` | [MRT Core](xref:Uno.Development.MigratingToUno7.Renames#mrt-core-moves-to-the-unowinrt-package) |
| `OnActivated`, `unoprotocolactivation` | [Activation](xref:Uno.Development.MigratingToUno7.AppStartup#activation-is-read-from-appinstance) |
| `.Suspending`, `.Resuming`, `.EnteredBackground`, `.LeavingBackground`, `RequiresPointerMode` | [App lifecycle events](xref:Uno.Development.MigratingToUno7.AppStartup#app-lifecycle-events-move-to-coreapplication) |
| `LocalSettings`, `RoamingSettings`, `StandardUserDefaults` | [Application settings](xref:Uno.Development.MigratingToUno7.AppStartup#application-settings-on-ios-and-tvos) |
| `ContextHelper`, `GetBaseActivityEvents` | [Android context APIs](xref:Uno.Development.MigratingToUno7.AppStartup#android-context-apis) |
| `FeatureConfiguration`, `PrimaryLanguageOverride` | [Removed feature flags](xref:Uno.Development.MigratingToUno7.FeatureFlags) |
| `Sdk="Microsoft.NET.Sdk"` on an app head, `SingleProject`, `uap10.0` | [The `Uno.Sdk` is required](xref:Uno.Development.MigratingToUno7.ProjectSetup#the-unosdk-is-required-for-applications) |
| `net9.0` | [.NET 9 is no longer supported](xref:Uno.Development.MigratingToUno7.ProjectSetup#net-9-is-no-longer-supported) |
| `maccatalyst`, `__MACCATALYST__`, `IsMacCatalyst`, `IsIOSOrCatalyst`, `MacCatalystProjectFolder` | [Mac Catalyst is removed](xref:Uno.Development.MigratingToUno7.ProjectSetup#mac-catalyst-is-removed) |
| `SupportedOSPlatformVersion`, `TargetPlatformMinVersion` | [Minimum OS versions](xref:Uno.Development.MigratingToUno7.ProjectSetup#minimum-os-versions-raised) |
| `WindowsSdkPackageVersion` | [Windows SDK projection](xref:Uno.Development.MigratingToUno7.ProjectSetup#the-windows-sdk-projection-is-picked-by-the-unosdk) |
| `UnoDisableHotRestartHelperGeneration` | [iOS Hot Restart](xref:Uno.Development.MigratingToUno7.ProjectSetup#ios-hot-restart-is-not-supported) |
| `UnoSplashScreenColor` | [`UnoSplashScreenColor` removed](xref:Uno.Development.MigratingToUno7.ProjectSetup#unosplashscreencolor-removed) |
| `UnoRuntimeIdentifier`, `UnoUIRuntimeIdentifier`, `UnoWinRTRuntimeIdentifier` | [Runtime identifier properties](xref:Uno.Development.MigratingToUno7.Libraries#runtime-identifier-properties-removed) |
| `HAS_UNO_SKIA`, `__UNO_SKIA`, `UNO_REFERENCE_API` | [Removed symbols](xref:Uno.Development.MigratingToUno7.Libraries#removed-preprocessor-symbols), [Symbols and project shape](xref:Uno.Development.MigratingToUno7.Libraries#preprocessor-symbols-no-longer-depend-on-the-project-shape) |
| `skia:`, `netstdref:`, `androidskia:`, `iosskia:`, `macos:`, `not_mux:`, `xamarin:`, `legacy:` | [Removed XAML prefixes](xref:Uno.Development.MigratingToUno7.Libraries#removed-xaml-prefixes) |
| Files named `*.Apple.cs`, `*.reference.cs`, `*.iOSmacOS.cs`, `*.skia.cs` | [Removed file suffixes](xref:Uno.Development.MigratingToUno7.Libraries#removed-file-suffixes), [Platform targeting](xref:Uno.Development.MigratingToUno7.Libraries#platform-targeting-in-multi-targeted-libraries) |
| `Prism`, `Lottie` in `UnoFeatures`; `Uno.WinUI.Lottie` | [`Prism`](xref:Uno.Development.MigratingToUno7.Packages#prism-uno-feature-removed), [Lottie](xref:Uno.Development.MigratingToUno7.Packages#unowinuilottie-package-and-lottie-uno-feature-removed) |
| `SKXamlCanvas`, `SKSwapChainPanel`, `SkiaSharp.Views.Uno.WinUI` | [`SkiaSharp.Views.Uno.WinUI`](xref:Uno.Development.MigratingToUno7.Packages#skiasharpviewsunowinui-no-longer-referenced-implicitly) |
| `SKPaint`, `SKCanvas`, `SkiaSharpVersion` | [SkiaSharp 4 is the default](xref:Uno.Development.MigratingToUno7.Behavior#skiasharp-4-is-the-default) |
| `GLCanvasElement`, `CullFaceMode` | [`GLCanvasElement`](xref:Uno.Development.MigratingToUno7.Behavior#glcanvaselement-subclasses-compile-against-silknetopengl-223) |
| `UseVulkanOn`, `UseOpenGLOn`, `PreferGLESOverGLOnX11` | [Vulkan](xref:Uno.Development.MigratingToUno7.Behavior#vulkan-is-the-default-rendering-backend) |
| `ConfigureUniversalImageLoader`, `bundle://` | [`Uno.UniversalImageLoader`](xref:Uno.Development.MigratingToUno7.Packages#unouniversalimageloader-no-longer-injected-android), [`bundle://`](xref:Uno.Development.MigratingToUno7.Packages#bundle-image-scheme-removed-iostvos) |
| `AdaptNative`, `HtmlId`, `HtmlElementAttribute`, `SetHtmlAttribute`, `RegisterHtmlEventHandler` | [Native element hosting](xref:Uno.Development.MigratingToUno7.RemovedApis#native-element-hosting) |
| `BindableView`, `BindableUIView`, `Android.Views.View`, `UIKit.UIView` | [Native base classes](xref:Uno.Development.MigratingToUno7.RemovedApis#native-base-classes--identity) |
| `NativeDefault`, `IsNativeStyle`, `UseNativePopup`, `AreDimensionsConstrained` | [Native rendering types and helpers](xref:Uno.Development.MigratingToUno7.RemovedApis#native-rendering-types-and-helpers) |
| `IJSObject`, `InvokeJSWithInterop`, `InvokeAsync`, `InvokeJSUnmarshalled`, `TSInteropMessage` | [WebAssembly interop](xref:Uno.Development.MigratingToUno7.RemovedApis#webassembly-interop) |
| `ItemClickCommand`, and `Command=` on `ListView` or `GridView` | [`ItemClickCommand`](xref:Uno.Development.MigratingToUno7.RemovedApis#listviewbaseitemclickcommand-and-listviewbasecommand) |
| `GetTemplateChild`, `VisualTreeHelper.GetChildren` | [`GetTemplateChild`](xref:Uno.Development.MigratingToUno7.RemovedApis#controlgettemplatechild-is-no-longer-public), [`GetChildren`](xref:Uno.Development.MigratingToUno7.RemovedApis#visualtreehelpergetchildren-is-no-longer-public) |
| `WindowSizeChangedEventHandler`, `VisibilityChangedEventArgs` | [`Window` events](xref:Uno.Development.MigratingToUno7.RemovedApis#window-events) |
| `Thickness.Empty`, `CornerRadius.None`, `Brush brush =`, `GridLength width =` | [Implicit conversions](xref:Uno.Development.MigratingToUno7.RemovedApis#uno-only-implicit-conversions-and-static-fields-removed) |
| `new StackPanel {`, `new Border {`, `new ScrollViewer {` | [Collection initializers](xref:Uno.Development.MigratingToUno7.RemovedApis#collection-initializers-on-panels-border-and-scrollviewer-removed) |
| `IAnimatedVisualSource`, `LottieVisualSourceBase` | [`IAnimatedVisualSource`](xref:Uno.Development.MigratingToUno7.RemovedApis#custom-ianimatedvisualsource-implementations) |
| `DependencyPropertyValuePrecedences` | [`DependencyPropertyValuePrecedences`](xref:Uno.Development.MigratingToUno7.RemovedApis#dependencypropertyvalueprecedences-members-changed) |
| `IsLocalResource` | [`IsLocalResource` is renamed](xref:Uno.Development.MigratingToUno7.RemovedApis#unoextensionsuriextensionsislocalresource-is-renamed-to-ismsappx) |
| `new Control()`, `new Panel()`, `new Timeline()`, `new DataTemplate(`, `new ControlTemplate(`, `new ItemsPanelTemplate(`, `FrameworkTemplateBuilder` | [Constructor accessibility](xref:Uno.Development.MigratingToUno7.WinUIParity#constructor-accessibility), [template factory delegates](xref:Uno.Development.MigratingToUno7.WinUIParity#templates-can-no-longer-be-built-from-a-factory-delegate) |
| `is ContentControl`, `is TextBox`, `is Border` | [Type-hierarchy changes](xref:Uno.Development.MigratingToUno7.WinUIParity#type-hierarchy-changes) |
| `.Background` on a `FrameworkElement`, `TextBlock`, `Image` or `Shape`; `FrameworkElement.BackgroundProperty` | [`Background`](xref:Uno.Development.MigratingToUno7.WinUIParity#background-is-no-longer-on-frameworkelement) |
| `ref thickness.`, `Duration.Type`, `.GridUnitType =`, `GuidHelper.Equals`, `.LoadContent()` | [WinRT projection alignment](xref:Uno.Development.MigratingToUno7.WinUIParity#winrt-projection-alignment) |
| `clr-namespace:`, `legacy:ListView`, `legacy:GridView` | [`clr-namespace:` xmlns form](xref:Uno.Development.MigratingToUno7.Xaml#the-wpf-style-clr-namespace-xmlns-form-is-no-longer-accepted), [`legacy:` lists](xref:Uno.Development.MigratingToUno7.Xaml#legacylistview-and-legacygridview-no-longer-compile) |
| `XamlDefault`, `XamlAppBar`, `DefaultFlyoutPresenter`, `BasedOn="{StaticResource Xaml` | [`XamlDefault*` styles](xref:Uno.Development.MigratingToUno7.Xaml#the-xamldefault-styles-are-removed) |
| `themeresources_v2`, `XamlControlsResourcesV2`, `ControlsResourcesVersion` | [Fluent theme resources](xref:Uno.Development.MigratingToUno7.Xaml#the-fluent-theme-resources-dictionary-is-now-themeresourcesxaml), [Resource-version types](xref:Uno.Development.MigratingToUno7.RemovedApis#fluent-design-resource-version-types) |
| `SystemBackdrop` | [`SystemBackdrop`](xref:Uno.Development.MigratingToUno7.Behavior#systembackdrop-no-longer-rewrites-your-contents-backgrounds) |
| `Resource.Drawable`, `FindResourceId` | [Retargeted drawables](xref:Uno.Development.MigratingToUno7.Behavior#retargeted-drawables-are-renamed) |
| `WithUnoHelpers`, `WebAuthenticationBroker` | [Authentication on WebAssembly](xref:Uno.Development.MigratingToUno7.Behavior#authentication-on-webassembly) |

## Related pages

- [Full checklist](xref:Uno.Development.MigratingToUno7.Checklist) for every migration task in one list.
- [Removed public APIs](xref:Uno.Development.MigratingToUno7.RemovedApis) for removed types and members.
- [Behavior changes to re-test](xref:Uno.Development.MigratingToUno7.Behavior) for what to re-test.
- [Upgrade steps](xref:Uno.Development.MigratingToUno7) to return to the main path.

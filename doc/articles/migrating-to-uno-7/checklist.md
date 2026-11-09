---
uid: Uno.Development.MigratingToUno7.Checklist
---

# Full checklist

Part of [Migrating to Uno Platform 7.0](xref:Uno.Development.MigratingToUno7). Every item links to its details. Items that don't apply to your app can be skipped.

## Project and targets

- Convert the application to a single `Sdk="Uno.Sdk"` project and delete the per-platform head projects. [Details](xref:Uno.Development.MigratingToUno7.ProjectSetup#the-unosdk-is-required-for-applications).
- Retarget any `net9.0-*` head or library to `net10.0-*`: 7.0 ships no `net9.0` assets. [Details](xref:Uno.Development.MigratingToUno7.ProjectSetup#net-9-is-no-longer-supported).
- Retarget any `net*-maccatalyst` head to `net10.0-desktop` and delete `Platforms/MacCatalyst/`. [Details](xref:Uno.Development.MigratingToUno7.ProjectSetup#mac-catalyst-is-removed).
- Remove any `WindowsSdkPackageVersion` property or environment variable: the `Uno.Sdk` picks the Windows SDK projection, and setting it fails the Windows build with `NETSDK1230`. [Details](xref:Uno.Development.MigratingToUno7.ProjectSetup#the-windows-sdk-projection-is-picked-by-the-unosdk).
- Raise `SupportedOSPlatformVersion` to **15.0** (iOS/tvOS) and **24.0** (Android), and `TargetPlatformMinVersion` to **10.0.19041.0** (WinAppSDK), in any head that pins them explicitly. [Details](xref:Uno.Development.MigratingToUno7.ProjectSetup#minimum-os-versions-raised).
- Replace the WASM DOM head with the Skia WebAssembly Browser head; remove any DOM/CSS customization and `HtmlElement` usage. [Details](xref:Uno.Development.MigratingToUno7.Packages#unowinuiwebassembly-package-removed-and-the-older-unowinuiruntimewebassembly).
- Remove any `UnoRuntimeIdentifier`, `UnoUIRuntimeIdentifier` or `UnoWinRTRuntimeIdentifier` property from application heads, and replace `UnoRuntimeIdentifier` with `UnoRuntimeVariant` in cross-runtime libraries — UNOB0024 points them out. [Details](xref:Uno.Development.MigratingToUno7.Libraries#runtime-identifier-properties-removed).

## Packages and Uno Features

- Remove `<UnoFeatures>skiarenderer</UnoFeatures>` (now implicit in apps) — and any native-only feature switches. Replace it with `Skia` instead if the app also lists `WebGpu` and still draws with SkiaSharp, or in a library that uses `SKCanvasElement`, the `SkiaExtensions` conversions or Toolkit Skia controls. Code built for Windows that draws with Skia also needs `<UnoEnableSkiaOnWinAppSdk>true</UnoEnableSkiaOnWinAppSdk>`. [Details](xref:Uno.Development.MigratingToUno7.ProjectSetup#rendering-is-skia-everywhere).
- Remove references to the deleted assemblies/types and to native element hosting. [Details](xref:Uno.Development.MigratingToUno7.RemovedApis#native-rendering-types-and-helpers).
- Remove manual `ConfigureUniversalImageLoader();` (Android) and other native bootstrap. [Details](xref:Uno.Development.MigratingToUno7.Packages#unouniversalimageloader-no-longer-injected-android).

## Libraries

- Recompile **every** Uno-dependent library against 7.0. [Details](xref:Uno.Development.MigratingToUno7.Libraries#platform-targeting-in-multi-targeted-libraries).
- Re-test `#if __IOS__` / `#if __ANDROID__` code and `ios:` / `android:` XAML inside multi-targeted libraries — those assets are now deployed on mobile heads instead of being replaced by the `netX.0` asset. [Details](xref:Uno.Development.MigratingToUno7.Libraries#platform-targeting-in-multi-targeted-libraries).

## Renames

- Rename `Uno.UI.Toolkit` usings and `xmlns` declarations to their new `Uno.UI.*` namespaces. Likewise drop `Skia` from `Uno.UI.Runtime.Skia.*` / `Uno.WinUI.Runtime.Skia.*` usings and host package references — see [Runtime host renames](xref:Uno.Development.MigratingToUno7.Renames#runtime-host-renames). [Details](xref:Uno.Development.MigratingToUno7.Renames#unouitoolkit-types-move-to-the-unoui-namespaces).
- Update assembly-qualified type names that reach MRT Core (`Microsoft.Windows.ApplicationModel.Resources.*`) — the assembly is now `Uno.WinRT`, not `Uno.UI`. [Details](xref:Uno.Development.MigratingToUno7.Renames#mrt-core-moves-to-the-unowinrt-package).

## App startup and platform state

- Convert the Android `Application` class to override `CreateHost()` instead of passing an `AppBuilder` delegate to the base constructor. Derive it and `MainActivity` from `Uno.UI.Runtime.Android.NativeApplication` / `Uno.UI.Runtime.Android.ApplicationActivity` instead of the `Microsoft.UI.Xaml` types. [Details](xref:Uno.Development.MigratingToUno7.AppStartup#android-head-uses-the-host-builder).
- Replace every `Application.OnActivated` override with `AppInstance.GetCurrent().GetActivatedEventArgs()` read from `OnLaunched`, plus an `AppInstance.GetCurrent().Activated` handler for activations that arrive while the app runs. [Details](xref:Uno.Development.MigratingToUno7.AppStartup#activation-is-read-from-appinstance).
- Move `Suspending`, `Resuming`, `EnteredBackground` and `LeavingBackground` subscriptions from `Application` to `CoreApplication`, and save state from `Window.Closed` on desktop, where suspension is no longer reported. [Details](xref:Uno.Development.MigratingToUno7.AppStartup#app-lifecycle-events-move-to-coreapplication).
- On iOS/tvOS, call `Uno.Storage.ApplicationDataMigrator.MigrateSettings()` at startup to bring pre-7.0 application settings into the `UnoApplicationData` container, and update any native/interop code that read them from `NSUserDefaults.StandardUserDefaults`. [Details](xref:Uno.Development.MigratingToUno7.AppStartup#application-settings-on-ios-and-tvos).

## Code and XAML

- Convert every `xmlns:…="clr-namespace:…"` declaration in your XAML to the `using:` form. [Details](xref:Uno.Development.MigratingToUno7.Xaml#the-wpf-style-clr-namespace-xmlns-form-is-no-longer-accepted).
- Delete native-only `FeatureConfiguration` calls. [Details](xref:Uno.Development.MigratingToUno7.FeatureFlags#native-only-flags-delete-the-call).
- Replace `ItemClickCommand` / `Command` on `ListView` and `GridView` with an `ItemClick` handler or the Uno Toolkit `CommandExtensions.Command` attached property. [Details](xref:Uno.Development.MigratingToUno7.RemovedApis#listviewbaseitemclickcommand-and-listviewbasecommand).
- Retype `Window.VisibilityChanged` handlers to `WindowVisibilityChangedEventArgs`, and drop any explicit `Window*EventHandler` delegate construction. [Details](xref:Uno.Development.MigratingToUno7.RemovedApis#window-events).
- Move `Background` reads/writes off `FrameworkElement`-typed references and off `TextBlock`/`Image`/`Shape`, and re-point `FrameworkElement.BackgroundProperty` at the declaring type. [Details](xref:Uno.Development.MigratingToUno7.WinUIParity#background-is-no-longer-on-frameworkelement).
- Move `.DataContext` reads/writes and `DataContextChanged` subscriptions off non-`FrameworkElement` objects (`Brush`, `Transform`, `FlyoutBase`, …) onto the owning element — `{Binding}` on those objects still resolves. [Details](xref:Uno.Development.MigratingToUno7.WinUIParity#datacontext-is-on-frameworkelement-only).
- Replace `SetBinding` calls on non-`FrameworkElement` objects with `BindingOperations.SetBinding`, and pass `FrameworkElement`-typed references to the `Grid` row/column accessors. [Details](xref:Uno.Development.MigratingToUno7.WinUIParity#setbinding-is-on-frameworkelement-only).
- Replace Uno-only implicit conversions (`Brush brush = Colors.Red`, `GridLength width = "Auto"`, …) and `Thickness.Empty` / `CornerRadius.None` with explicit constructors, and collection initializers on panels, `Border` and `ScrollViewer` with `Children = { … }`, `Child =` or `Content =`. [Details](xref:Uno.Development.MigratingToUno7.RemovedApis#uno-only-implicit-conversions-and-static-fields-removed).

## Behavior and testing

- If you use `Window.SystemBackdrop`, make your own root `Page`/panel transparent — the framework no longer does it for you. [Details](xref:Uno.Development.MigratingToUno7.Behavior#systembackdrop-no-longer-rewrites-your-contents-backgrounds).
- Re-baseline visual/snapshot tests and re-test text, lists/scroll, IME, pickers, and safe-area/notch handling on devices. [Details](xref:Uno.Development.MigratingToUno7.Behavior#rendering-differences).

## If you are moving from native rendering

See the [Uno 6.0 migration guide](xref:Uno.Development.MigratingToUno6#optional-use-of-skia-rendering-for-ios-android-and-webassembly)
for the full Android/iOS/WebAssembly Skia bootstrapping steps.

## Related pages

- [Errors, symptoms and code search](xref:Uno.Development.MigratingToUno7.Lookup) to go from an error or symptom to its fix.
- [Behavior changes to re-test](xref:Uno.Development.MigratingToUno7.Behavior) for what to re-test.
- [Upgrade steps](xref:Uno.Development.MigratingToUno7) to return to the main path.

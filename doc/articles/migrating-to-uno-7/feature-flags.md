---
uid: Uno.Development.MigratingToUno7.FeatureFlags
---

# Removed feature flags

Part of [Migrating to Uno Platform 7.0](xref:Uno.Development.MigratingToUno7).

**Read this if** your code contains `FeatureConfiguration` or `WinRTFeatureConfiguration`, or your app sets `ApplicationLanguages.PrimaryLanguageOverride`, which changes behavior even for apps that never touched the removed flag.

**Skip it if** you find neither in your code.

## Native-only flags: delete the call

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
  `NavigationView.EnableUno19516Workaround`.
- **iOS:** `Image.LegacyIosAlignment`,
  `FrameworkElement.IOsAllowSuperviewNeedsLayoutWhileInLayoutSubViews`,
  `CommandBar.AllowNativePresenterContent`, `DatePicker.UseLegacyStyle`,
  `TimePicker.UseLegacyStyle`, `UIElement.FailOnNSObjectExtensionsValidateDispose`
  (see [`NSObjectExtensions.ValidateDispose`](xref:Uno.Development.MigratingToUno7.RemovedApis#deprecated-uikit-disposal-helper)).
- **WebAssembly:** `Interop.ForceJavascriptInterop`, `UIElement.AssignDOMXamlName`,
  `UIElement.AssignDOMXamlProperties`, `UIElement.RenderToStringWithId`,
  `TextBlock.IsMeasureCacheEnabled`, `Shape.WasmDelayUpdateUntilFirstArrange`,
  `Shape.WasmCacheBBoxCalculationResult`, `Shape.WasmBBoxCacheSize`,
  `Cursors.UseHandForInteraction` (the "hand" cursor for interactive controls is
  now never used).
- **Native (Android + iOS):** `ListViewBase.AnimateScrollIntoView`.
- **Native styling:** the whole `Style` holder — `Style.UseUWPDefaultStyles`,
  `Style.UseUWPDefaultStylesOverride`, `Style.SetUWPDefaultStylesOverride<TControl>()`, and
  `Style.ConfigureNativeFrameNavigation()`. The WinUI default styles are now the only ones.
- **Skia overlay:** `TextBox.UseOverlayOnSkia`.

`WebView2.IsInspectable` is also removed; it was an obsolete alias, so switch to
`WebView2.EnableDevTools` instead.

## Flags whose removal can change behavior

### `ApplicationLanguages.UseLegacyPrimaryLanguageOverride`

The cross-platform `WinRTFeatureConfiguration.ApplicationLanguages.UseLegacyPrimaryLanguageOverride`
flag is also removed. Unlike the native-only flags it defaulted to `true`, so this changes behavior for
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

### `Frame.UseWinUIBehavior` and `Page.IsPoolingEnabled`

The cross-platform `Frame.UseWinUIBehavior` and `Page.IsPoolingEnabled` flags are also removed, along
with the `Microsoft.UI.Xaml.PagePool` type and the `FrameNavigationHelper.GetInstance` and
`FrameNavigationHelper.EnsurePageInitialized` helpers. `Frame` now always uses the WinUI navigation
model, which was already the default on Skia, so only apps that set `UseWinUIBehavior` to `false` see
a difference:

- Back-stack pages are no longer all kept in memory. A page is cached according to its
  `NavigationCacheMode` and the frame's `CacheSize`, so a page with the default
  `NavigationCacheMode.Disabled` is recreated when you navigate back to it. Set
  `NavigationCacheMode="Required"` (or `Enabled`) on pages that must keep their state.
- `GetNavigationState` and `SetNavigationState` now serialize and restore the navigation history
  instead of only storing the string.

### `Control.UseLegacyContentAlignment`

The cross-platform `Control.UseLegacyContentAlignment` flag is also removed. It opted into the
legacy Top/Left default for `HorizontalContentAlignment`/`VerticalContentAlignment`; the default is
now always the WinUI-correct **Center/Center**. Apps that set it to `true` should instead set
`HorizontalContentAlignment`/`VerticalContentAlignment` explicitly (via a `Style` or per control).

### `AndroidSettings.IsEdgeToEdgeEnabled`

`FeatureConfiguration.AndroidSettings.IsEdgeToEdgeEnabled` is also removed, and Android apps are now
always edge-to-edge. In 6.x the Android host read the flag at startup and, when it was `false`, left
the system bars opaque on Android 14 and earlier (Android 15 enforces edge-to-edge regardless). An app
that set it to `false` now draws behind the status and navigation bars on every Android version, so
keep content out of them with the [Uno Toolkit `SafeArea`](xref:Toolkit.Controls.SafeArea) or
`ApplicationView.VisibleBounds`.

### Cross-platform compatibility flags

The following cross-platform compatibility flags are also removed. Each one restored an earlier Uno
Platform behavior that differed from WinUI; the WinUI behavior was already their default and is now the
only one, so only apps that changed one of them see a difference:

| Removed flag | Behavior apps now get |
| --- | --- |
| `FeatureConfiguration.FrameworkElement.UseLegacyHitTest` | A non-null `Background` or `BorderBrush` makes a `Panel`, `Border`, `ContentPresenter` or `ScrollPresenter` hit-testable, but not other elements such as a `Control`. |
| `FeatureConfiguration.Popup.EnableLightDismissByDefault` | `Popup.IsLightDismissEnabled` defaults to `false`. Set it to `true` on each popup that should close when the user taps outside it. |
| `FeatureConfiguration.VisualState.ApplySettersBeforeTransition` | A `VisualState`'s `Setters` are applied when its `VisualTransition` storyboard ends, not when the state change starts. |
| `FeatureConfiguration.Binding.IgnoreINPCSameReferences` | A `PropertyChanged` notification re-evaluates the binding path even when the property returns the same reference. |
| `FeatureConfiguration.DataTemplateSelector.UseLegacyTemplateSelectorOverload` | When `SelectTemplateCore(object)` returns `null`, `SelectTemplateCore(object, DependencyObject)` is called with the container. |
| `FeatureConfiguration.ManipulationRoutedEventArgs.IsAbsolutePositionEnabled` | `Position` in `ManipulationStarted`, `ManipulationDelta` and `ManipulationCompleted` is relative to the manipulation's `Container`, not to the window. |
| `FeatureConfiguration.UIElement.DisablePointersSpecificEventPrevention` | When a gesture event such as `Tapped` bubbles up from a child, an ancestor only stops recognizing that same gesture for the pointer, instead of ending all of its gesture recognition. |
| `FeatureConfiguration.UIElement.UseInvalidateMeasurePath` / `UseInvalidateArrangePath` | Layout always re-measures and re-arranges only the elements on an invalidated path. To turn this off for one subtree, use `FrameworkElementHelper.SetUseMeasurePathDisabled` / `SetUseArrangePathDisabled`. |

### `ProgressRing.ProgressRingAsset` and `DeterminateProgressRingAsset`

`FeatureConfiguration.ProgressRing.ProgressRingAsset` and `.DeterminateProgressRingAsset` are also
removed. In 6.x they were still read when the app referenced `Uno.UI.Lottie`: they named the Lottie file
a `ProgressRing` played when its `IndeterminateSource` or `DeterminateSource` was not set. Without
`Uno.UI.Lottie`, 6.x showed a placeholder (`UNOX0001`) instead, so those apps see no change here. With
it, a ring that sets neither source now plays the built-in
WinUI animation, so an app that pointed them at its own animation sets `ProgressRing.IndeterminateSource`
/ `.DeterminateSource` instead, for example in an implicit `ProgressRing` style.

### `Font.FallbackService`

`FeatureConfiguration.Font.FallbackService`, `IFontFallbackService` and `EmptyFontFallbackService` are
removed: font fallback is now part of the font provider. To turn fallback off,
register the built-in provider with `SkiaBackend.CreateFontProvider(enableFallback: false)` (or
`ManagedBackend.CreateFontProvider(enableFallback: false)` with the WebGPU backend); to customize it,
implement `IFontProvider`. See [Font fallback for unsupported codepoints](xref:Uno.Features.CustomFonts#font-fallback-for-unsupported-codepoints).

## Flags with no remaining effect

The following flags are also removed. Their readers were removed earlier (with the native renderers,
or when a control was rewritten), or they could not be set, so removing them does not change behavior on
any currently-supported target:

- `FeatureConfiguration.ContentPresenter.UseImplicitContentFromTemplatedParent`.
- `FeatureConfiguration.Control.UseLegacyLazyApplyTemplate` and `.UseDeferredOnApplyTemplate`. In 6.x
  only the native renderers read them; the Skia control lifecycle, which every target now uses, applies
  the template as WinUI does.
- `FeatureConfiguration.UIElement.ShowClippingBounds`, documented as iOS-only and no longer read.
- `FeatureConfiguration.Timeline.DefaultsStartingValueFromAnimatedValue`, which had no setter. An
  animation without `From` still starts from the current animated value.
- `WinRTFeatureConfiguration.Focus.EnableExperimentalKeyboardFocus` (iOS/tvOS) — keyboard focus
  handling is always enabled.
- `WinRTFeatureConfiguration.GestureRecognizer.InterpretMouseLeftLongPressAsRightTap` (Android) and
  `.InterpretForceTouchAsRightTap` (iOS/tvOS).

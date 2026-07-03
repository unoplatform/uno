# Uno Platform — Breaking-Changes Rollout (Epic [#8339](https://github.com/unoplatform/uno/issues/8339))

> Ordered, danger-ranked checklist for the next major. Items are sequenced **least dangerous first** (native-only & dead code) to **most dangerous last** (pervasive type-system changes). Each unchecked item is meant to be picked up by a single subagent in its own worktree.

_Generated 2026-06-15 from a per-item investigation of the current `dev/mazi/breaking` worktree (see `assessments-raw.json` for the raw danger/effort/impact data behind every line)._

## Ground rules (decided)

1. **Hard-remove everything now** — no `[Obsolete]`/`[EditorBrowsable]` deprecation shims. This is THE breaking major; many items have been pending since 2022.
2. **Match WinUI exactly** — when reducing visibility / changing signatures, the target shape is the WinUI one. Do not invent intermediate Uno-only surfaces. Native divergences are removed, not preserved.
3. **Native targets are being dropped** (native Android Views, iOS/UIKit, WASM DOM). Native-only changes are therefore very safe and ship first. _Non-UI WinRT APIs in `Uno.UWP`/`Uno.Foundation` are NOT being dropped_ (Skia-on-Android still consumes them) — those are judged on normal merit.
4. **The 5 pervasive type-system items get their own impact specs** (BC14, BC26, BC38, BC58 + BC54 folded into BC58) with Pros/Cons. They ship last, one stabilized PR each.
5. **Validate at runtime, not compile-only.** For anything that changes behaviour (Phase 5 especially) prove it with a runtime test that fails-before/passes-after.

## How a subagent picks up an item

1. Take the **topmost unchecked** item (respect the cross-item ordering in the watchlist below).
2. Spin up a worktree; make the change per the item's action note + its WinUI reference.
3. Build the relevant Skia slice; run/extend runtime tests; for behavioural items add a fails-before/passes-after test.
4. One focused Conventional-Commit PR per item, referencing #8339. Check the box here.

**Legend:** `dN` = danger 1 (safest) → 5 (most dangerous) · `S/M/L` = effort · ⚠️ = has an open decision/verify-first flag (see the item note).

---

## Phase 1 — Native-only removals & inert dead-code

_Danger 1. The safe vanguard: deletions scoped to native targets being dropped, plus orphaned public stubs with zero consumers. Cannot affect Skia runtime. Ship as one batch._

- [ ] **BC05** — Remove no-op `RethrowNativeExceptions` config  `d1·S`
  - Hard-delete.
  - Files: `src/Uno.Foundation/FoundationFeatureConfiguration.cs`
- [ ] **BC29** — Remove iOS `BypassCheckToCloseKeyboard`  `d1·S`
  - Hard-delete.
  - Files: `src/Uno.UI/UI/Xaml/Window/Native/NativeWindow.UIKit.cs`
- [ ] **BC30** — Delete `WindowView.ios.cs`  `d1·S`
  - Hard-delete.
  - Files: `src/Uno.UI/UI/Xaml/Controls/WindowView.iOS.cs`
- [ ] **BC31** — Delete `BinderDetails` stub  `d1·S` · PR #16757
  - Hard-delete.
  - Files: `src/Uno.UI/DataBinding/BinderDetails.Android.cs`, `src/SourceGenerators/Uno.UI.SourceGenerators/DependencyObject/DependencyObjectGenerator.cs`, `src/Uno.UI/DataBinding/BinderReferenceHolder.cs`
- [ ] **BC40** — Delete legacy `ListViewBase` folder  `d1·S`
  - Hard-delete.
  - Files: `src/Uno.UI/UI/Xaml/Controls/ListViewBase/Legacy/`, `src/Uno.UI/UI/Xaml/Controls/Grid/GridExtensions.cs`, `src/SamplesApp/SamplesApp.Samples/Windows_UI_Xaml_Controls/ListView/ListViewLegacy.xaml`
- [ ] **BC47** — Remove `NSObjectExtensions.ValidateDispose`  `d1·S`
  - Hard-delete.
  - Files: `src/Uno.UI/Extensions/NSObjectExtensions.UIKit.cs`, `src/Uno.UI/FeatureConfiguration.cs`, `doc/articles/feature-flags.md`
- [ ] **BC61** — Delete iOS `StyleSelector2`/`StyleSelectorCollection`  `d1·S`
  - Hard-delete.
  - Files: `src/Uno.UI/Controls/StyleSelectorCollection.UIKit.cs`, `src/Uno.UI/Controls/StyleSelector2.UIKit.cs`
- [ ] **BC60** — Remove Android native popups  `d1·M`
  - Hard-delete.
  - Files: `src/Uno.UI/UI/Xaml/Controls/Popup/Popup.Android.cs`, `src/Uno.UI/UI/Xaml/Controls/Popup/NativePopup.Android.cs`, `src/Uno.UI/UI/Xaml/Controls/Popup/NativePopupAdapter.cs`
- [ ] **BC35** — Remove `IgnoreINPCSameReferences` flag  `d1·S`
  - Hard-delete the flag; inline the always-taken branch.
  - Files: `src/Uno.UI/FeatureConfiguration.cs`, `src/Uno.UI/DataBinding/BindingPath.BindingItem.cs`
- [ ] **BC67** — Remove `ColorKeyFrameCollection` throwing shadow setters  `d1·S`
  - Delete the entire `new`-shadow back-compat block to match `DoubleKeyFrameCollection`/`ObjectKeyFrameCollection`.
  - Files: `src/Uno.UI/UI/Xaml/Media/Animation/ColorKeyFrameCollection.cs`, `src/Uno.UI/UI/Xaml/DependencyObjectCollection.cs`, `src/Uno.UI/UI/Xaml/Media/Animation/DoubleKeyFrameCollection.cs`

---

## Phase 2 — Native-only public-API removals

_Danger 2. Native-scoped, but delete public members a Skia user still sees in IntelliSense / compiles against. Native rendering is gone so the symbols are meaningless; hard-remove._

- [ ] **BC23** — Remove `AdaptNative` native-hosting path  `d2·S`
  - Hard-remove the public `AdaptNative`/`TryAdaptNative` surface (native hosting gone).
  - Files: `src/Uno.UI/UI/Xaml/Media/VisualTreeHelper.cs`, `src/Uno.UI/UI/Xaml/UIElementCollectionExtensions.Xamarin.cs`, `src/Uno.UI/UI/Xaml/Controls/Border/Border.cs`
- [ ] **BC66** — Remove Android `Window.IsStatusBarTranslucent()`  `d2·S`
  - Hard-remove the public Android `Window.IsStatusBarTranslucent()`; repoint the internal consumer to `NativeWindowWrapper`.
  - Files: `src/Uno.UI/UI/Xaml/Window/Window.Android.cs`, `src/Uno.UI/UI/Xaml/Window/Native/NativeWindowWrapper.Android.cs`, `src/Uno.UI/UI/Xaml/Controls/ComboBox/ComboBox.custom.cs`

---

## Phase 3 — Internal feature-flag, dead-code & shim removals

_Danger 2. Cross-target but low blast radius: delete always-on/off flags (inline the live branch), dead helpers, binary-compat shims, no-op members. Behaviour unchanged on defaults. The Setter pipeline trio BC37 -> BC44 -> BC63 must land adjacently in that order._

- [ ] **BC04** — Remove `EventsBubblingInManagedCode` DP  `d2·S`
  - Hard-remove the public DP — native bubbling is gone.
  - Files: `src/Uno.UI/UI/Xaml/UIElement.RoutedEvents.cs`, `src/Uno.UI/UI/Xaml/RoutedEventArgs.cs`, `src/Uno.UI/UI/Xaml/Input/PointerRoutedEventArgs.cs`
- [ ] **BC02** — Remove legacy WASM `IJSObject` DOM-interop API  `d2·S` · PR #19230
  - Hard-delete.
  - Files: `src/Uno.Foundation.Runtime.WebAssembly/Interop/IJSObject.wasm.cs`, `src/Uno.Foundation.Runtime.WebAssembly/Interop/IJSObjectMetadata.wasm.cs`, `src/Uno.Foundation.Runtime.WebAssembly/Interop/JSObjectHandle.wasm.cs`
- [ ] **BC32** — Delete `DelegatedInkTrailVisual` stub  `d2·S`
  - Hard-delete.
  - Files: `src/Uno.UI.Composition/Composition/DelegatedInkTrailVisual.cs`, `build/PackageDiffIgnore.xml`
- [ ] **BC46** — Remove `ShowClippingBounds` flag  `d2·S`
  - Hard-delete the flag; inline the always-taken branch.
  - Files: `src/Uno.UI/FeatureConfiguration.cs`
- [ ] **BC06** — Remove `DefaultsStartingValueFromAnimatedValue` flag  `d2·S`
  - Hard-delete the flag; inline the always-taken branch.
  - Files: `src/Uno.UI/FeatureConfiguration.cs`, `src/Uno.UI/UI/Xaml/Media/Animation/Timeline.animation.cs`
- [ ] **BC69** — Remove `ApplySettersBeforeTransition` flag  `d2·S` · #13326
  - Hard-delete the flag; inline the always-taken branch.
  - Files: `src/Uno.UI/FeatureConfiguration.cs`, `src/Uno.UI/UI/Xaml/VisualStateGroup.cs`, `src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml/Given_VisualStateManager.UnitTests.cs`
- [ ] **BC33** — Remove `UseInvalidate(Measure/Arrange)Path` flags  `d2·M`
  - Hard-delete both flags; inline the always-true dirty-path branch.
  - Files: `src/Uno.UI/FeatureConfiguration.cs`, `src/Uno.UI/UI/Xaml/UIElement.Layout.crossruntime.cs`, `src/Uno.UI/UI/Xaml/UIElement.skia.cs`
- [ ] **BC49** — Remove empty `RestoreBindings`/`ClearBindings`  `d2·S` · #13046
  - Hard-delete.
  - Files: `src/Uno.UI/UI/Xaml/DependencyObjectStore.Binder.cs`, `src/SourceGenerators/Uno.UI.SourceGenerators/DependencyObject/DependencyObjectGenerator.cs`, `src/SourceGenerators/Uno.UI.SourceGenerators.Internal/Mixins/FrameworkElementUIKitMixinGenerator.cs`
- [ ] **BC70** — Remove no-op `AdjustArrange`  `d2·S` · #14478
  - Hard-delete.
  - Files: `src/Uno.UI/UI/Xaml/IFrameworkElement.cs`, `src/Uno.UI/UI/Xaml/FrameworkElement.Interface.skia.cs`, `src/Uno.UI/UI/Xaml/FrameworkElement.Interface.reference.cs`
- [ ] **BC72** — Delete `Uno.UWP` `Vector2Extensions`  `d2·S`
  - Hard-delete.
  - Files: `src/Uno.UWP/Extensions/Vector2Extensions.cs`, `src/Uno.Foundation/Extensions/VectorExtensions.cs`, `build/PackageDiffIgnore.xml`
- [ ] **BC07** — Remove redundant bootstrapper meta-packages ⚠️  `d2·M` · PR #17788
  - **Verify** not already removed, then hard-delete the meta-packages.
  - Files: `build/Uno.UI.Build.csproj`, `build/nuget/Uno.WinUI.Skia.X11.nuspec`, `build/nuget/Uno.WinUI.Skia.MacOS.nuspec`
- [ ] **BC43** — Remove `PointerPoint.op_Explicit` shim  `d2·S`
  - Hard-delete.
  - Files: `src/Uno.UI/UI/Input/WinRT/PointerPoint.cs`, `src/Uno.ReferenceImplComparer/Program.cs`
- [ ] **BC18** — Remove `UseLegacyHitTest` flag  `d2·S`
  - Hard-delete the flag; inline the always-taken branch.
  - Files: `src/Uno.UI/FeatureConfiguration.cs`, `src/Uno.UI/UI/Xaml/FrameworkElement.cs`, `src/Uno.UI/UI/Xaml/UIElement.Pointers.Managed.cs`
- [ ] **BC45** — Remove `UseLegacyContentAlignment` flag  `d2·S`
  - Hard-delete the flag; inline the always-taken branch.
  - Files: `src/Uno.UI/FeatureConfiguration.cs`, `src/SourceGenerators/Uno.UI.SourceGenerators.Internal/Mixins/DependencyPropertyMixinGenerator.cs`
- [ ] **BC55** — Replace deprecated Android `PreferenceManager` ⚠️  `d2·S` · #1833
  - Use `Context.GetSharedPreferences` with the legacy default name (or `AndroidX.Preference`) — MUST read the **same backing file** so existing user settings survive upgrade.
  - Files: `src/Uno.UWP/Storage/ApplicationDataContainer.Android.cs`
- [ ] **BC37** — Remove non-DP (CLR) `Setter` codegen fallback  `d2·S` · PR #16134
  - Hard-delete.
  - Files: `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.cs`, `src/Uno.UI/UI/Xaml/Setter.Generic.cs`, `src/SamplesApp/SamplesApp.Samples/Windows_UI_Xaml_Controls/ComboBox/ComboBox_FullScreen_Popup.xaml`
- [ ] **BC44** — Remove `Setter<T>`  `d2·S`
  - Hard-delete.
  - Files: `src/Uno.UI/UI/Xaml/Setter.Generic.cs`, `src/Uno.UI/UI/Xaml/SetterBase.cs`, `src/Uno.UI/UI/Xaml/Style/Style.cs`
- [ ] **BC63** — Remove `SetterBase.set_Property` shim  `d2·S` · #13050
  - Hard-delete.
  - Files: `src/Uno.UI/UI/Xaml/SetterBase.cs`, `src/Uno.UI/UI/Xaml/Setter.Generic.cs`, `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.cs`

---

## Phase 4 — Visibility reductions & signature/rename tweaks

_Danger 2. Tighten Uno-only public surface to match WinUI (internal/protected/sealed/explicit-interface/hide) plus tiny signature & typo fixes. Each is narrow and independent; effort S. Ideal single sweep._

- [ ] **BC24** — Hide `BrushConverter` from IntelliSense  `d2·S`
  - Reduce visibility to match WinUI.
  - Files: `src/Uno.UI/UI/Xaml/Media/BrushConverter.cs`, `src/Uno.UI/UI/Xaml/Media/Brush.cs`, `src/Uno.UI/LinkerDefinition.Wasm.xml`
- [ ] **BC15** — Seal `PropertyPath`  `d2·S`
  - Reduce visibility to match WinUI.
  - Files: `src/Uno.UI/UI/Xaml/Data/PropertyPath.cs`, `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml/PropertyPath.cs`
- [ ] **BC22** — `IMenu`/`IMenuPresenter`/`ISubMenuOwner` -> non-public  `d2·S`
  - Reduce visibility to match WinUI.
  - Files: `src/Uno.UI/UI/Xaml/Controls/MenuFlyout/IMenu.cs`, `src/Uno.UI/UI/Xaml/Controls/MenuFlyout/IMenuPresenter.cs`, `src/Uno.UI/UI/Xaml/Controls/MenuFlyout/ISubMenuOwner.cs`
- [ ] **BC57** — `OnTemplateRecycled` -> explicit interface impl  `d2·S` · #13083
  - Reduce visibility to match WinUI.
  - Files: `src/Uno.UI/UI/Xaml/IFrameworkTemplatePoolAware.cs`, `src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.cs`, `src/Uno.UI/UI/Xaml/Controls/ToggleSwitch/ToggleSwitch.cs`
- [ ] **BC48** — `MaterializableList<T>` -> internal  `d2·S`
  - Reduce visibility to match WinUI.
  - Files: `src/Uno.UWP/Collections/MaterializableList.cs`, `src/Uno.UI/UI/Xaml/Media/VisualTreeHelper.cs`, `src/Uno.UI/UI/Xaml/UIElement.crossruntime.cs`
- [ ] **BC08** — `NavigatingCancelEventArgs` ctor -> non-public  `d2·S`
  - Reduce visibility to match WinUI.
  - Files: `src/Uno.UI/UI/Xaml/Navigation/NavigatingCancelEventArgs.cs`, `src/Uno.UI/UI/Xaml/Navigation/NavigationHelpers.cs`, `src/Uno.UI/UI/Xaml/Controls/Frame/Frame.legacy.cs`
- [ ] **BC11** — `PageStackEntry.SourcePageType` setter -> non-public  `d2·S`
  - Reduce visibility to match WinUI.
  - Files: `src/Uno.UI/UI/Xaml/Navigation/PageStackEntry.Properties.cs`, `src/Uno.UI/UI/Xaml/Navigation/PageStackEntry.cs`, `src/Uno.UI/UI/Xaml/Controls/Frame/Frame.legacy.cs`
- [ ] **BC12** — `Frame.BackStack` setter -> non-public  `d2·S`
  - Reduce visibility to match WinUI.
  - Files: `src/Uno.UI/UI/Xaml/Controls/Frame/Frame.Properties.cs`, `src/Uno.UI/UI/Xaml/Controls/Frame/Frame.legacy.cs`, `src/Uno.UI/UI/Xaml/Controls/Frame/Frame.partial.mux.cs`
- [ ] **BC09** — `ComboBox.OnIsDropDownOpenChanged` -> private protected  `d2·S`
  - Make `private protected`.
  - Files: `src/Uno.UI/UI/Xaml/Controls/ComboBox/ComboBox.custom.cs`, `src/Uno.UI/UI/Xaml/Controls/ComboBox/ComboBox.partial.mux.cs`
- [ ] **BC20** — `FrameworkPropertyMetadata.DefaultUpdateSourceTrigger` -> internal ⚠️  `d2·S`
  - Make `DefaultUpdateSourceTrigger` internal/remove. Optional stretch: make the whole `FrameworkPropertyMetadata` internal (verify no custom-control author relies on it).
  - Files: `src/Uno.UI/UI/Xaml/FrameworkPropertyMetadata.cs`, `src/Uno.UI/DataBinding/BindingExpression.cs`, `src/Uno.UI/UI/Xaml/FrameworkPropertyMetadataOptions.cs`
- [ ] **BC25** — `Border.ChildProperty` -> internal  `d2·S`
  - Make the DP `internal` (keep the DP, hide the field) to match WinUI.
  - Files: `src/Uno.UI/UI/Xaml/Controls/Border/Border.cs`, `src/Uno.UI.UnitTests/DependencyProperty/Given_DependencyProperty.cs`
- [ ] **BC16** — Re-apply `PropertyChangedParams` change ⚠️  `d2·M` · PR #17414
  - **VERIFY design first.** The post-revert pooled `DependencyPropertyChangedEventArgs` may already capture the allocation win, and the struct drops precedence/bypass fields. Decide whether re-applying is still desired before doing it.
  - Files: `src/Uno.UI/UI/Xaml/PropertyChangedParams.cs`, `src/Uno.UI/UI/Xaml/IDependencyObjectInternal.cs`, `src/SourceGenerators/Uno.UI.SourceGenerators/DependencyObject/DependencyObjectGenerator.cs`
- [ ] **BC62** — Fix `Lauched`->`Launched` trace constants  `d2·S` · #13709
  - Adjust signature to match WinUI.
  - Files: `src/Uno.UI/UI/Xaml/Application.cs`, `src/Uno.UI/UI/Xaml/Application.skia.cs`, `src/Uno.UI/UI/Xaml/Application.wasm.cs`
- [ ] **BC41** — `VisualInteractionSource` param -> `Microsoft.UI.Input`  `d2·S`
  - Adjust signature to match WinUI.
  - Files: `src/Uno.UI.Composition/Composition/VisualInteractionSource.cs`, `src/Uno.UI.Composition/Composition/ICompositionTarget.cs`, `src/Uno.UI/UI/Xaml/Media/CompositionTarget.cs`
- [ ] **BC28** — `CompositionSpriteShape.StrokeDashArray` get-only  `d2·S`
  - Adjust signature to match WinUI.
  - Files: `src/Uno.UI.Composition/Composition/CompositionSpriteShape.cs`, `src/Uno.UI.Composition/Composition/CompositionSpriteShape.skia.cs`, `src/Uno.UI.Composition/Composition/CompositionStrokeDashArray.cs`
- [ ] **BC64** — `Duration.TimeSpan` -> read-only property  `d2·S` · #13096
  - Make `TimeSpan` a read-only property (WinUI). Confirm treatment of the WinUI-divergent public `Type` field (issue notes UWP may differ).
  - Files: `src/Uno.UI/UI/Xaml/Duration.cs`, `src/Uno.UI/UI/Xaml/DurationType.cs`, `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml/Duration.cs`

---

## Phase 5 — Behavioural defaults, base-class & type tweaks

_Danger 3. Wider but localized: visibility on more-derivable hooks, per-type base-class realignments, enum/type-shape changes, and behaviour-changing defaults. Several silently change runtime behaviour even on defaults — gate each on a migration note + runtime/visual validation, not compile-only._

- [ ] **BC17** — `XamlCompositionBrushBase.CompositionBrush` -> protected  `d3·S`
  - Reduce visibility to match WinUI.
  - Files: `src/Uno.UI/UI/Xaml/Media/XamlCompositionBrushBase.cs`, `src/Uno.UI/UI/Xaml/Media/XamlCompositionBrushBase.skia.cs`, `src/Uno.UI/UI/Xaml/Media/AcrylicBrush/AcrylicBrush.skia.cs`
- [ ] **BC19** — Remove `FlyoutBase.Close()` (use `Hide()`)  `d3·S`
  - Reduce visibility to match WinUI.
  - Files: `src/Uno.UI/UI/Xaml/Controls/Flyout/FlyoutBase.cs`, `src/Uno.UI/UI/Xaml/Controls/Flyout/Flyout.cs`, `src/Uno.UI/UI/Xaml/Controls/Button/Button.cs`
- [ ] **BC27** — `DoubleCollection`: composition not `List<T>`  `d3·S`
  - Reparent to match WinUI.
  - Files: `src/Uno.UI/UI/Xaml/Media/DoubleCollection.cs`, `src/Uno.UI/UI/Xaml/Media/PointCollection.cs`, `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Media/DoubleCollection.cs`
- [ ] **BC73** — `TimePickerFlyoutPresenter` -> `Control` base  `d3·S`
  - Reparent to match WinUI.
  - Files: `src/Uno.UI/UI/Xaml/Controls/TimePicker/TimePickerFlyoutPresenter.cs`, `src/Uno.UI/UI/Xaml/Controls/TimePicker/TimePickerFlyoutPresenter.Properties.cs`, `src/Uno.UI/UI/Xaml/Controls/TimePicker/TimePickerFlyoutPresenter.partial.mux.cs`
- [ ] **BC13** — Fix `WindowActivatedEventArgs.WindowActivationState` type  `d3·M`
  - See notes.
  - Files: `src/Uno.UI/UI/Xaml/Window/WindowActivatedEventArgs.cs`, `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml/WindowActivationState.cs`, `src/Uno.WinAppSDKSyncGenerator/Helpers/SymbolMatchingHelpers.cs`
- [ ] **BC65** — `FrameworkElement`/`ContentControl`: drop `IEnumerable`  `d3·S`
  - See notes.
  - Files: `src/Uno.UI/UI/Xaml/FrameworkElement.crossruntime.cs`, `src/Uno.UI/UI/Xaml/FrameworkElement.skia.cs`, `src/Uno.UI/UI/Xaml/FrameworkElement.wasm.cs`
- [ ] **BC34** — Remove `TextBox.OnVerticalContentAlignmentChanged` override  `d3·S`
  - Delete the `TextBox` override; make base `OnVerticalContentAlignmentChanged` `private protected`.
  - Files: `src/Uno.UI/UI/Xaml/Controls/TextBox/TextBox.cs`, `src/Uno.UI/UI/Xaml/Controls/ContentPresenter/ContentPresenter.cs`
- [ ] **BC36** — `ContentPresenter.ContentTemplateRoot` -> internal  `d3·S` · #16148
  - Make `internal` (not `private` — in-assembly callers read it cross-type).
  - Files: `src/Uno.UI/UI/Xaml/Controls/ContentPresenter/ContentPresenter.cs`, `src/Uno.UI/UI/Xaml/Controls/Button/HyperlinkButton.mux.cs`, `src/Uno.UI/UI/Xaml/FrameworkElement.cs`
- [ ] **BC52** — Reparent `RadioMenuFlyoutItem` -> `MenuFlyoutItem`  `d3·M`
  - Reparent to `MenuFlyoutItem`; re-implement the toggle behavior currently inherited from `ToggleMenuFlyoutItem`.
  - Files: `src/Uno.UI/UI/Xaml/Controls/RadioMenuFlyoutItem/RadioMenuFlyoutItem.cs`, `src/Uno.UI/UI/Xaml/Controls/RadioMenuFlyoutItem/RadioMenuFlyoutItem.Properties.cs`, `src/Uno.WinAppSDKSyncGenerator/Generator.cs`
- [ ] **BC74** — Android drawable extension in retarget keys  `d3·S` · PR #15891
  - Adjust signature to match WinUI.
  - Files: `src/Uno.UWP/Helpers/AndroidResourceNameEncoder.cs`, `src/Uno.UWP/Helpers/DrawableHelper.Android.cs`, `src/SourceGenerators/Uno.UI.Tasks/ResourceConverters/AndroidResourceConverter.cs`
- [ ] **BC50** — Default `UseLegacyPrimaryLanguageOverride` = false  `d3·S` · #13704
  - Flip default to `false` (WinUI restart-to-apply). **Behavior change** — runtime language-switch apps must opt back in; needs a migration note.
  - Files: `src/Uno.UWP/FeatureConfiguration/WinRTFeatureConfiguration.cs`, `src/Uno.UWP/Globalization/ApplicationLanguages.cs`, `src/Uno.UI/UI/Xaml/Application.cs`
- [ ] **BC42** — Remove `clr-namespace:` XAML leniency  `d3·M`
  - Remove the leniency; migrate repo XAML/tests/docs to `using:`. Hard-error, no deprecation cycle.
  - Files: `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeGeneration.cs`, `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.Reflection.cs`, `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlCodeBehindParser.cs`
- [ ] **BC51** — Implement `ms-resource:///` URI rewrite  `d3·M`
  - Implement the rewrite to `ms-resource:///Files/`. **Changes compiled URI value** — behavior-changing, validate at runtime.
  - Files: `src/SourceGenerators/Uno.UI.SourceGenerators/XamlGenerator/XamlFileGenerator.cs`, `src/Uno.UI/UI/Xaml/XamlFilePathHelper.shared.cs`
- [ ] **BC21** — Delete legacy `AutomationProperties` member ⚠️  `d3·S`
  - **INVESTIGATE / likely drop.** The member at ~L44 (`AnnotationsProperty`) is valid WinUI and must NOT be deleted — the line ref drifted. Real target is probably the native `OnAutomationIdChanged` branches in `AutomationProperties.uno.cs`.
  - Files: `src/Uno.UI/UI/Xaml/Automation/AutomationProperties.cs`, `src/Uno.UI/UI/Xaml/Automation/AutomationProperties.uno.cs`
- [ ] **BC75** — Move MRT Core (`ApplicationModel.Resources`) -> `Uno.UWP`  `d3·M`
  - Move the MRT-Core types to `Uno.UWP` (lower risk than a new assembly).
  - Files: `src/Uno.UI/Windows/ApplicationModel/Resources/ResourceLoader.cs`, `src/Uno.UI/Windows/ApplicationModel/Resources/IResourceContext.cs`, `src/Uno.UI/Windows/ApplicationModel/Resources/IResourceManager.cs`

---

## Phase 6 — Larger structural moves & legacy-mechanism removal

_Danger 3-4. Heavier multi-file changes: remove the legacy templated-parent mechanism, repurpose the precedence enum, tear down Fluent V1 scaffolding, rename the Toolkit assembly, and remove the legacy `Windows.UI.Input.*` surface. Each is its own PR._

- [ ] **BC01** — Remove legacy templated-parent mechanism  `d3·L` · #18672
  - Hard-remove legacy path incl. public `FrameworkTemplateBuilder` delegate + legacy ctors. **First** fix the tests that currently force `_isLegacyTemplate=true`.
  - Files: `src/Uno.UI/UI/Xaml/FrameworkTemplate.cs`, `src/Uno.UI/UI/Xaml/TemplatedParentScope.cs`, `src/Uno.UI/UI/Xaml/ControlTemplate.cs`
- [ ] **BC39** — Clean up `DependencyPropertyValuePrecedences` enum  `d2·M` · PR #15684
  - Hard-remove obsolete enum members (no `[EditorBrowsable]` aliases).
  - Files: `src/Uno.UI/UI/Xaml/DependencyPropertyValuePrecedences.cs`, `src/Uno.UI/UI/Xaml/DependencyObjectStore.cs`, `src/Uno.UI/UI/Xaml/Internal/DependencyPropertyHelper.cs`
- [ ] **BC71** — Remove Fluent V1 public surface  `d2·M` · #14765
  - Hard-remove `XamlControlsResourcesV1` + `ControlsResourcesVersion.Version1` + build/package scaffolding (V1 content is already gone).
  - Files: `src/Uno.UI.FluentTheme.v1/XamlControlsResourcesV1.cs`, `src/Uno.UI.FluentTheme.v1/Uno.UI.FluentTheme.v1.Skia.csproj`, `src/Uno.UI.FluentTheme.v1/Uno.UI.FluentTheme.v1.Wasm.csproj`
- [ ] **BC53** — Rename `Uno.UI.Toolkit` assembly/namespace ⚠️  `d4·M` · #12322
  - **DECIDE the new assembly/namespace name.** Hard rename, no type-forwarders / xmlns alias (per hard-remove policy).
  - Files: `src/Uno.UI.Toolkit/`, `src/Uno.UI.Toolkit/Uno.UI.Toolkit.Skia.csproj`, `src/Uno.UI.Toolkit/Uno.UI.Toolkit.Wasm.csproj`
- [ ] **BC76** — Remove/internalize `Windows.UI.Input.*` (~173 types)  `d4·M` · #18875
  - Hard-remove the ~173 stub types. The **real** `InputInjector` family (test infra) -> move to `Microsoft.UI.Input.Preview.Injection` and update call sites.
  - Files: `src/Uno.UWP/Generated/3.0.0.0/Windows.UI.Input`, `src/Uno.UWP/Generated/3.0.0.0/Windows.UI.Input.Spatial`, `src/Uno.UWP/Generated/3.0.0.0/Windows.UI.Input.Inking`

---

## Phase 7 — Pervasive type-system changes (each has its own impact spec)

_Danger 4-5. Ship last, never batched — each lands as its own separately-stabilized PR with full runtime-test passes. See the dedicated spec per item for Pros/Cons and impact._

- [ ] **BC58** — `DataContext` on `FrameworkElement` only  `d4·M` · #13201 · **[impact spec](bc58-datacontext-frameworkelement-only.md)**
  - Adjust signature to match WinUI.
  - Files: `src/SourceGenerators/Uno.UI.SourceGenerators/DependencyObject/DependencyObjectGenerator.cs`, `src/Uno.UI/UI/Xaml/DependencyObjectStore.Binder.cs`, `src/Uno.UI.UnitTests/DependencyProperty/Given_DependencyProperty.DataContext.cs`
- [ ] **BC54** — `FlyoutBase.DataContext` -> non-public  `d4·L` · #12491
  - Keep `DataContext` internal so `FlyoutBase`->`Popup` forwarding still works; hide only the public surface. **Folded into the BC58 spec.**
  - Files: `src/SourceGenerators/Uno.UI.SourceGenerators/DependencyObject/DependencyObjectGenerator.cs`, `src/Uno.UI/UI/Xaml/Controls/Flyout/FlyoutBase.cs`, `src/Uno.UI/UI/Xaml/DependencyObjectStore.Binder.cs`
- [ ] **BC26** — `DependencyObject` becomes a class  `d4·L` · #17099 · **[impact spec](bc26-dependencyobject-as-class.md)**
  - See notes.
  - Files: `src/Uno.UI/UI/Xaml/DependencyObject.cs`, `src/SourceGenerators/Uno.UI.SourceGenerators/DependencyObject/DependencyObjectGenerator.cs`, `src/Uno.UI/UI/Xaml/UIElement.skia.cs`
- [ ] **BC14** — `UserControl` inherits `Control`  `d5·L` · **[impact spec](bc14-usercontrol-to-control.md)**
  - Reparent to match WinUI.
  - Files: `src/Uno.UI/UI/Xaml/Controls/UserControl/UserControl.cs`, `src/Uno.UI/UI/Xaml/Controls/Page/Page.cs`, `src/Uno.UI/UI/Xaml/Controls/ContentControl/ContentControl.cs`
- [ ] **BC38** — Move `Background` `FrameworkElement` -> `Control`  `d4·L` · **[impact spec](bc38-background-to-control.md)**
  - Reparent to match WinUI.
  - Files: `src/Uno.UI/UI/Xaml/FrameworkElement.Interface.skia.cs`, `src/Uno.UI/UI/Xaml/FrameworkElement.Interface.wasm.cs`, `src/Uno.UI/UI/Xaml/FrameworkElement.Interface.reference.cs`

---

## Standalone — large but low runtime-risk (run anytime)

- [ ] **BC59** — Enable nullable reference types by default  `d1·L` · #9964
  - Standalone. Flip `<Nullable>enable</Nullable>` globally + annotate ~4300 files. Low runtime risk, large churn; can run anytime independent of the phases.
  - Files: `src/Directory.Build.props`, `src/Uno.UI/Uno.UI.Skia.csproj`, `src/Uno.UI/Uno.UI.Reference.csproj`

---

## Sequencing watchlist

- **Setter pipeline (Phase 3):** `BC37` (remove CLR-property Setter codegen) → `BC44` (remove `Setter<T>`) → `BC63` (remove `SetterBase.set_Property`). Land adjacently, in order.
- **DataContext / DependencyObject (Phase 7):** do `BC58` (generator-wide DataContext->FE-only) **first**; it gates `BC54` (FlyoutBase symptom) and de-risks `BC26` (DependencyObject->class). All touch the same generated DO mixin.
- **Background / UserControl (Phase 7):** `BC38` (Background -> Control) precedes `BC14` (UserControl -> Control) so the property relocation is not redone.
- **Behaviour-drift items (validate at runtime):** `BC50` (culture no longer changes on setter), `BC45` (Center/Center default), `BC74` (drawable key collision), `BC42` (drop `clr-namespace:`), `BC51` (`ms-resource:///` rewrite) — each changes runtime behaviour even for default users.
- **Verify-first / open-decision items:** `BC07` (maybe already removed), `BC16` (struct design maybe obsolete), `BC21` (line ref drifted; may drop), `BC53` (needs a new name), `BC55` (must preserve the prefs backing file).

---

## Appendix A — Already done / obsolete (verify & check off, no work expected)

- [ ] **BC03** — `BaseActivity.Current` annotated nullable — _moot (native drop)_ · PR #18582
  - Still technically applies (the file keeps the non-nullable `public static BaseActivity Current`), but `BaseActivity.Android.cs` is a **native-Android** host file deleted wholesale when native targets drop — no separate task needed. PR #18582 was closed unmerged (2025-09-15, touched only that file, +3/-1).
- [ ] **BC56** — Remove Android `EnableExperimentalKeyboardFocus` — _already-done_
  - The Android-specific removal this item describes is already implemented. In WinRTFeatureConfiguration.Focus.cs the property is gated `#if __IOS__ || __TVOS__` (line 7) and its XML doc already says "keyboard focus is now always enabled on An
- [ ] **BC77** — Adjust Android conditionals — _already-done_ · PR #12563
  - Verified against the live worktree: grep for `__ANDROID_\d+__` across all of src returns ZERO matches, so every numbered conditional this PR targeted is already gone. Spot-checked the representative cases: BindableGridView.Android.cs no lon
- [ ] **BC78** — `BorderLayerRenderer` refactor — _already-done_ · PR #12593
  - PR #12593 is CLOSED, but the refactor it proposed has already landed on this branch. Commit 3887af4454 ("refactor: Move border-related properties into a IBorderInfoProvider") plus follow-ups (a0018d8a1d, 3c16c1fc64, f32e017186, dced5e6424) 
- [ ] **BC10** — `ItemsControl.OnItemsChanged` parameter type — _obsolete_ · (WinUI already matches)
  - Located at src/Uno.UI/UI/Xaml/Controls/ItemsControl/ItemsControl.cs:254 — `protected virtual void OnItemsChanged(object e)`. The internal caller OnItemsVectorChanged (line 90-93) does pass an IVectorChangedEventArgs into it, which is presum
- [ ] **BC68** — WinUI sync-generator fix — _obsolete_ · PR #13867
  - PR #13867 ("fix: WinUI sync generator", CLOSED, by Youssef1313, labeled stale) modified the old UWPSyncGenerator/DocGenerator tooling to emit a WinUI/WinAppSDK API surface. Searched the repo: there is NO UWPSyncGenerator project or any refe
- [ ] **BC79** — `Brush` change-subscription perf — _obsolete_ · PR #12595
  - PR #12595 is CLOSED (WIP perf, authored by MartinZikmund, replaced #12234). It added an internal closure-free brush-change subscription (SubscribeToChanges, BrushChangedCallback delegate, BrushChangedDisposable, RaiseBrushChanged) and migra

_BC54 is listed in Phase 7 but has no standalone task — it is implemented as part of BC58._

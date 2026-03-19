# WinUI Sync Branch Review: Dangerous Changes

Branch: `dev/mazi/winui-sync` vs `master`
Scope: 5,432 files changed, 140,703 insertions, 65,960 deletions across 77 commits.

---

## CRITICAL: Compilation Errors

### 1. Duplicate `Point(float, float)` constructor — WILL NOT COMPILE
- **File**: `src/Uno.Foundation/Point.cs:21` and `src/Uno.Foundation/Point.cs:33`
- **Severity**: **Build-breaking** (CS0111)
- Line 21: unconditional `public Point(float x, float y)` (pre-existing)
- Line 33: `#if HAS_UNO_WINUI` duplicate `public Point(float x, float y)` (newly added)
- `HAS_UNO_WINUI` is always defined for non-UWP builds (via `Uno.CrossTargetting.targets` and now also `Directory.Build.props`), so both constructors are included.

### 2. Duplicate `Size(float, float)` constructor — WILL NOT COMPILE
- **File**: `src/Uno.Foundation/Size.cs:19` and `src/Uno.Foundation/Size.cs:32`
- **Severity**: **Build-breaking** (CS0111)
- Same pattern as Point. Two identical `public Size(float width, float height)` constructors.

### 3. Duplicate `Rect(float, float, float, float)` constructor — WILL NOT COMPILE
- **File**: `src/Uno.Foundation/Rect.cs:49` and `src/Uno.Foundation/Rect.cs:71`
- **Severity**: **Build-breaking** (CS0111)
- Same pattern. Two identical `public Rect(float x, float y, float width, float height)` constructors.

### 4. `ColorHelper` partial class `static` mismatch — WILL NOT COMPILE
- **Generated**: `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI/ColorHelper.cs:9` — `public partial class ColorHelper` (NOT static)
- **Existing**: `src/Uno.UI/UI/ColorHelper.cs` — `public static partial class ColorHelper`
- **Severity**: **Build-breaking** (CS0106) — One partial declaration says `static`, the other doesn't. The generated file's `#if false` guard only controls the `[NotImplemented]` attribute, NOT the class declaration itself.

### 5. `Colors` partial class `static` mismatch — WILL NOT COMPILE
- **Generated**: `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI/Colors.cs` — `public partial class Colors` (NOT static)
- **Existing**: `src/Uno.UI/UI/Colors.cs` — `static partial class Colors`
- **Severity**: **Build-breaking** (CS0106) — Same issue as ColorHelper.

### 6. `VectorExtensions` partial mismatch — WILL NOT COMPILE
- **Generated**: `src/Uno.Foundation/Generated/2.0.0.0/System.Numerics/VectorExtensions.cs` — `public static partial class VectorExtensions`
- **Existing**: `src/Uno.Foundation/Extensions/VectorExtensions.cs` — `public static class VectorExtensions` (NOT `partial`)
- **Severity**: **Build-breaking** (CS0260) — Existing class needs the `partial` keyword added.

### 7. `StatusBar` reference will break on non-mobile platforms
- **File**: `src/Uno.UI.Toolkit/Diagnostics/DiagnosticsOverlay.Placement.cs:48,58-59`
- Uses `StatusBar?` field and `typeof(StatusBar)` inside `#if HAS_UNO_WINUI` (always true).
- The `using Windows.UI.ViewManagement;` imports `StatusBar`, but the generated stub was deleted.
- Non-generated `StatusBar` only exists for Android/iOS — **Skia, WASM, unit test targets will fail to compile**.

---

## CRITICAL: Code Bug

### 8. Rect float constructor uses wrong parameter name in height validation
- **File**: `src/Uno.Foundation/Rect.cs:60` (and duplicated at line 82)
- **Severity**: **Bug** (incorrect error message)
- The height validation `throw new ArgumentOutOfRangeException(nameof(width), ...)` should be `nameof(height)`.
- This bug exists in BOTH copies of the float constructor.

---

## HIGH: Breaking Changes — Public API Removal

### 9. Entire `Windows.Phone.*` namespace removed (BREAKING)
- **Commit**: `56ca41cf99 fix!: Remove Windows.Phone namespaces`
- **Severity**: **Binary + source breaking** for any consumer using these types
- **79 public types deleted** across these namespaces:
  - `Windows.Phone.Devices.Notification` — `VibrationDevice` (generated stub only; hand-written implementations on Android/iOS/Wasm survive)
  - `Windows.Phone.Devices.Power` — `Battery`
  - `Windows.Phone.Management.Deployment` — 7 types (Enterprise, InstallationManager, etc.)
  - `Windows.Phone.Media.Devices` — 3 types (AudioRoutingManager, enums)
  - `Windows.Phone.Notification.Management` — 29 types (AccessoryManager, trigger details, enums)
  - `Windows.Phone.PersonalInformation` — 15 types (ContactStore, ContactInformation, etc.)
  - `Windows.Phone.PersonalInformation.Provisioning` — 2 types
  - `Windows.Phone.System` — SystemProtection
  - `Windows.Phone.System.Power` — PowerManager, PowerSavingMode
  - `Windows.Phone.System.Profile` — RetailMode
  - `Windows.Phone.System.UserProfile.GameServices.Core` — 4 types
  - `Windows.Phone.UI.Input` — BackPressedEventArgs, CameraEventArgs, HardwareButtons
  - `Windows.Phone.ApplicationModel` — ApplicationProfile, ApplicationProfileModes
  - `Windows.Phone.Speech.Recognition` — SpeechRecognitionUIStatus
  - `Windows.Phone` — PhoneContract
- All were `[NotImplemented]` stubs. Only `VibrationDevice` had real implementations (which survive).
- **Impact**: Any Uno app using `Windows.Phone.*` types will fail to compile.

### 10. `IBindableIterable` and `IBindableVector` interfaces deleted
- **Files deleted**: `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml.Interop/IBindableIterable.cs`, `IBindableVector.cs`
- **Severity**: **Binary + source breaking** — Public interfaces removed entirely.
- `IBindableObservableVector` changed base from `: IBindableVector, IBindableIterable` → `: IList, ICollection, IEnumerable` (BCL types).
- `IBindableVectorView` changed base from `: IBindableIterable` → `: IEnumerable`.
- **Mitigation**: Non-generated code references are in comments or use `using` aliases mapping to BCL types already.

### 11. `CoreWebView2HttpHeadersCollectionIterator` interface change
- **Old**: `: IIterator<KeyValuePair<string, string>>`
- **New**: `: IEnumerator<KeyValuePair<string, string>>, IEnumerator, IDisposable`
- `HasCurrent` property removed, `GetMany()` removed. `Reset()` and `Dispose()` added.
- **Severity**: **Binary + source breaking** — Changed from WinRT iterator to .NET enumerator pattern.

### 12. `Windows.UI.ViewManagement.StatusBar` generated stub removed
- **File deleted**: `src/Uno.UWP/Generated/3.0.0.0/Windows.UI.ViewManagement/StatusBar.cs`
- Non-generated implementation exists only for Android/iOS at `src/Uno.UWP/UI/ViewManagement/StatusBar/`.
- The generated stub provided `ForegroundColor`, `BackgroundOpacity`, `BackgroundColor`, `OccludedRect`, `ProgressIndicator`, `GetForCurrentView()`, `ShowAsync()`, `HideAsync()` as NotImplemented stubs on other platforms. These stubs are now gone.
- **Direct impact**: DiagnosticsOverlay compilation failure on non-mobile platforms (see #7).
- `StatusBarProgressIndicator` generated stub also deleted (no non-generated implementation exists).

### 13. `Windows.UI.Composition` easing function types removed from Uno.UWP
- **14 files deleted** from `src/Uno.UWP/Generated/3.0.0.0/Windows.UI.Composition/`:
  - `BackEasingFunction`, `BounceEasingFunction`, `CircleEasingFunction`, `ElasticEasingFunction`, `ExponentialEasingFunction`, `PowerEasingFunction`, `SineEasingFunction`
  - `CompositionEasingFunctionMode` (enum), `DelegatedInkTrailVisual`, `RectangleClip`
  - `ICompositionSupportsSystemBackdrop`, `ICompositionSurfaceFacade`, `IVisualElement2`, `InkTrailPoint`
- **Mitigation**: All exist under `Microsoft.UI.Composition` namespace in `src/Uno.UI.Composition/`.
- **Risk**: Low — no non-generated code references the `Windows.UI.Composition` versions.

### 14. `Windows.UI.ViewManagement.Core` types removed
- `CoreFrameworkInputView`, `CoreFrameworkInputViewAnimationStartingEventArgs`, `CoreFrameworkInputViewOcclusionsChangedEventArgs`, `CoreInputViewAnimationStartingEventArgs`
- **Severity**: **Binary breaking** — public types removed entirely.

### 15. Multiple `Windows.ApplicationModel.Calls` types removed
- `PhoneCall`, `PhoneCallInfo`, `PhoneCallsResult`, `PhoneLineDialResult`
- **Severity**: **Binary breaking** — public types removed.

### 16. Multiple Windows SDK types removed (not in latest WinUI)
- `Windows.ApplicationModel.ConversationalAgent` — 2 types
- `Windows.ApplicationModel.Store.Preview.WebAuthenticationCoreManagerHelper`
- `Windows.Devices.Bluetooth` — 4 Bluetooth LE types
- `Windows.Devices.Display.Core.DisplayTaskResult`
- `Windows.Devices.Printers` — 7 IPP types (IppAttributeError, IppPrintDevice, etc.)
- `Windows.Graphics.Holographic.HolographicDepthReprojectionMethod`
- `Windows.Graphics.Printing.PrintSupport` — 8 types
- `Windows.Graphics.Printing.Workflow` — 12 types
- `Windows.Management.Deployment` — 3 types
- `Windows.Media.Capture` — `ScreenCapture`, `SourceSuspensionChangedEventArgs`
- `Windows.Media.Core` — `TimedTextBouten`, `TimedTextRuby`
- `Windows.Media.Devices` — 6 types (Camera/DigitalWindow)
- `Windows.Media.Effects.SlowMotionEffectDefinition`
- `Windows.Media.SpeechRecognition` — `VoiceCommandManager`, `VoiceCommandSet`
- `Windows.Networking.NetworkOperators` — 5 MobileBroadband types
- `Windows.Networking.Vpn.VpnForegroundActivatedEventArgs`
- `Windows.Storage.StorageLibraryChangeTrackerOptions`
- `Windows.System.RemoteDesktop.Input.RemoteTextConnection`
- `Windows.System.UserAgeConsentResult`
- `Windows.UI.Notifications.ToastNotificationMode`
- `Windows.UI.Shell` — `ShareWindowCommandEventArgs`, `ShareWindowCommandSource`
- `Windows.UI.Core.CoreIndependentInputSourceController`
- `Windows.UI.Text.FontWeights` (generated stub; non-generated impl in Uno.UI survives)

### 17. Deleted generated stubs from Uno.UI
- `GeneratorPositionHelper` — public, was NotImplemented, no non-generated impl. WinUI removed it.
- `DataErrorsChangedEventArgs` — public, was NotImplemented. WinUI removed it.
- `RepeatBehaviorHelper`, `Matrix3DHelper`, `MatrixHelper` — public helpers, all NotImplemented.
- `PropertyChangedEventHandler`, `ICommand` — were empty "Skipped type" shells (no actual API).
- `INotifyCollectionChanged`, `NotifyCollectionChangedEventArgs` — WinRT projections of BCL types.
- `CornerRadiusHelper`, `DurationHelper`, `GridLengthHelper`, `ThicknessHelper` — safe, had non-generated implementations with `#if false` generated stubs.
- `INotifyDataErrorInfo`, `NotifyCollectionChangedAction`, `NotifyCollectionChangedEventHandler`, `PropertyChangedEventArgs` — WinRT projections removed.

### 18. Deleted generated stubs from Uno.Foundation
- `IIterator<T>`, `IPropertyValue`, `IReferenceArray<T>` — all had non-generated implementations with complete API surface. **Safe deletion.**

---

## HIGH: Binary Breaking Changes — Type Kind Changes

### 19. Contract structs changed to enums
- `FoundationContract`: `public partial struct` → `public enum` (Uno.Foundation, `#if false` — compiled out)
- `UniversalApiContract`: `public partial struct` → `public enum` (Uno.UWP, `#if` platform-guarded — **compiled**)
- `WinUIContract`: `public partial struct` → `public enum` (Uno.UI)
- `XamlContract`: `public partial struct` → `public enum` (Uno.UI)
- + Multiple new contract enums added: `MrtCoreContract`, `TextApiContract`, `WindowsAppSDKContract`, `StoreContract`, `WwanContract`, `CallsPhoneContract`, `AppLifecycleContract`
- **Severity**: **Binary breaking** — `struct` → `enum` changes layout, calling convention, boxing behavior.

---

## HIGH: Breaking Changes — Base Class / Interface Changes

### 20. `NumberBoxAutomationPeer` base class changed
- **Old**: `public partial class NumberBoxAutomationPeer : AutomationPeer`
- **New**: `public partial class NumberBoxAutomationPeer : FrameworkElementAutomationPeer`
- **Severity**: **Binary breaking** — base class change affects vtable layout, type checks, casting.

### 21. `IBindableObservableVector` base interfaces completely changed
- **Old**: `: IBindableVector, IBindableIterable`
- **New**: `: IList, ICollection, IEnumerable`
- **Severity**: **Binary + source breaking** — switches from WinRT collection interfaces to BCL interfaces.

### 22. `IBindableVectorView` base interface changed
- **Old**: `: IBindableIterable`
- **New**: `: IEnumerable`
- **Severity**: **Binary breaking**.

---

## MEDIUM: Interface Changes (Additive but Potentially Breaking)

### 23. `IObservableMap<K,V>` gains additional base interfaces
- **Old**: `: IDictionary<K, V>`
- **New**: `: IDictionary<K, V>, ICollection<KeyValuePair<K, V>>, IEnumerable<KeyValuePair<K, V>>, IEnumerable`
- **Severity**: Generally non-breaking since `IDictionary<>` already implies `ICollection<>` and `IEnumerable<>`. Custom implementors should already have these.

### 24. `IObservableVector<T>` gains additional base interfaces
- **Old**: `: IList<T>`
- **New**: `: IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable`
- Same analysis as IObservableMap.

### 25. `IPropertySet` gains additional base interfaces
- Adds `ICollection<KeyValuePair<string, object>>` and `IEnumerable`

### 26. `SceneLightingEffect` gains `IGraphicsEffectSource` interface
- **Old**: `: IGraphicsEffect`
- **New**: `: IGraphicsEffect, IGraphicsEffectSource`

### 27. 40+ collection classes gain additional base interfaces
- Pattern: `IList<T>, IEnumerable<T>` → `IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable`
- Affects: `ColumnDefinitionCollection`, `RowDefinitionCollection`, `BlockCollection`, `InlineCollection`, `ItemCollection`, `SwipeItems`, `DependencyObjectCollection`, `ResourceDictionary`, `SetterBaseCollection`, various KeyFrame collections, Composition collections, Http collections, etc.

---

## MEDIUM: New Types That May Conflict or Shadow

### 28. `ReadOnlyArrayAttribute` and `WriteOnlyArrayAttribute` added to Uno.Foundation
- **Namespace**: `System.Runtime.InteropServices.WindowsRuntime`
- **Risk**: May shadow BCL-provided attributes of the same name and namespace.
- **Asymmetry bug**: `ReadOnlyArrayAttribute` has `false` for `__WASM__` guard (constructor available on WASM) while `WriteOnlyArrayAttribute` uses `__WASM__` (constructor guarded). Inconsistent platform availability.

### 29. `XamlParseException` added to Uno.Foundation in TWO namespaces
- `Microsoft.UI.Xaml.Markup.XamlParseException` and `Windows.UI.Xaml.Markup.XamlParseException`
- **Bug**: Constructors call `: base()` instead of `: base(message)` / `: base(message, innerException)` — if ever used, message and inner exception would be silently lost.

### 30. `LayoutCycleException` added to Uno.Foundation in TWO namespaces
- Same `: base()` bug as XamlParseException.

### 31. `ElementNotAvailableException` / `ElementNotEnabledException` added to Uno.Foundation
- Both in `Windows.UI.Xaml.Automation` and `Microsoft.UI.Xaml.Automation` namespaces
- Existing implementations in `src/Uno.UI/UI/Xaml/Automation/` are in `Microsoft.UI.Xaml.Automation`

### 32. `BindableCustomProperty` and `IBindableCustomPropertyImplementation` added to Uno.Foundation
- New public types in `Microsoft.UI.Xaml.Data` namespace, living in `Uno.Foundation`
- This namespace is typically owned by `Uno.UI`.

### 33. `DispatcherQueueSynchronizationContext` added to Uno.UI.Dispatching
- Entirely new public type extending `SynchronizationContext`.
- No backing implementation — `Post()`, `Send()`, `CreateCopy()` all throw NotImplementedException.
- Users could discover and attempt to use this key concurrency type.

---

## MEDIUM: Project/Build Infrastructure Changes

### 34. `HAS_UNO_WINUI` define added to `Directory.Build.props`
- Was already defined in `src/Uno.CrossTargetting.targets` for the same condition.
- Adding it to `Directory.Build.props` may activate it for projects that don't import `Uno.CrossTargetting.targets`.
- **Direct consequence**: This causes the duplicate constructor compilation errors in Point, Size, Rect (#1-#3).

### 35. Build project target framework changed: `net462` → `net7.0`
- **File**: `build/Uno.UI.Build.csproj`
- **Risk**: CI/build agents that don't have .NET 7.0+ SDK will fail.

### 36. Old reference project removed, new one added
- **Removed**: `src/Uno.UWPSyncGenerator.Reference/` (3 files)
- **Added**: `src/Uno.UWPSyncGenerator.Reference.WinUI/` (targeting `net10.0-windows10.0.19041.0`, references `Microsoft.WindowsAppSDK` 1.8)
- Solution files (.slnx, .slnf) updated to reference new project.
- `src/Uno.UI-Tools.slnf` includes BOTH old and new reference projects — old project no longer exists, may cause solution load errors.

---

## LOW: New API Surface (Worth Verifying)

### 37. ~700 new generated types added to Uno.UWP
- Major namespaces: `Windows.AI.MachineLearning` (30+ types), `Windows.Media.Capture` (72 types), `Windows.Devices.SmartCards` (39 types), `Windows.Graphics.Printing3D` (34 types), `Windows.Security.Isolation` (25 types)
- All `[NotImplemented]` stubs — expected behavior for sync generator.

### 38. New `Microsoft.UI` types added to Uno.UWP
- `DisplayId`, `IconId`, `WindowId`, `Win32Interop`, `IClosableNotifier`, `ClosableNotifierHandler`
- `WindowId` properly pairs with existing non-generated implementation.

### 39. New `ThemeSettings` type added to Uno.UI
- `Microsoft.UI.System.ThemeSettings` — new NotImplemented stub (new namespace).

### 40. New struct constructors with named parameters
- `PhysicalKeyStatus(uint _RepeatCount, uint _ScanCode, ...)`
- `ManipulationDelta(Point _Translation, float _Scale, ...)`
- `ManipulationVelocities(Point _Linear, float _Angular, ...)`
- `TextRange(int _StartIndex, int _Length)`
- `XmlnsDefinition(string _XmlNamespace, string _Namespace)`
- Note: Underscore-prefixed parameter names are unusual but match WinUI convention.

---

## LOW: Behavioral Changes

### 41. Classes made `partial` to support generated extensions
- `WindowsRuntimeStorageExtensions`, `WindowsRuntimeStreamExtensions`, `AsyncInfo`, `WindowsRuntimeBufferExtensions`, `WindowsRuntimeSystemExtensions` — all `static class` → `static partial class`
- `ElementNotAvailableException`, `ElementNotEnabledException` — `class` → `partial class`
- **Risk**: Low — adding `partial` modifier doesn't change behavior.

### 42. Explicit interface implementations added under `HAS_UNO_WINUI`
- `PropertySet.IObservableMap<string, object>.MapChanged`
- `StringMap.IObservableMap<string, string>.MapChanged`
- `ItemCollection.IObservableVector<object>.VectorChanged`
- `TransitionCollection.GetEnumerator()` (new `public new` GetEnumerator)
- `DoubleCollection.GetEnumerator()` (new `public new` GetEnumerator)
- **Risk**: The `new GetEnumerator()` hides inherited `List<T>.GetEnumerator()`. Could change `foreach` behavior.

### 43. `DependencyObject` generated interface stub removed
- The generated partial interface `DependencyObject` in `src/Uno.UI/Generated/3.0.0.0/Microsoft.UI.Xaml/DependencyObject.cs` was replaced with a "Skipped type" comment.
- Non-generated implementation exists. On Android/iOS where DependencyObject is an interface, verify the generated partial was not contributing members.

---

## Summary by Severity

| Severity | Count | Key Items |
|----------|-------|-----------|
| **CRITICAL (Build-breaking)** | 7 | Duplicate constructors (3), static mismatch (2), partial mismatch (1), StatusBar platform break (1) |
| **CRITICAL (Bug)** | 1 | Wrong parameter name in Rect validation |
| **HIGH (API removal/change)** | 10 | Windows.Phone removal (79 types), IBindable interfaces deleted/changed, WebView2 iterator changed, StatusBar stub deleted, Composition types moved, contract struct→enum, NumberBoxAutomationPeer base class |
| **MEDIUM (Potentially breaking)** | 12 | Interface expansions, new type conflicts/shadows, build infra changes, constructor bugs in generated exceptions |
| **LOW (Verify)** | 7 | New API surface, behavioral changes, generated stub removals with existing impls |

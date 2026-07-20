# Quickstart: Mobile Accessibility and Automation

This guide covers the existing Skia mobile build/test paths and the native observables that
the implementation must validate.

## 1. Prepare the mobile build

From `src`, use a single target framework and fast local build:

```powershell
Set-Location src
Copy-Item crosstargeting_override.props.sample crosstargeting_override.props
```

Set one target in `crosstargeting_override.props`:

```xml
<Project>
  <PropertyGroup>
    <UnoTargetFrameworkOverride>net10.0-android</UnoTargetFrameworkOverride>
    <UnoFastDevBuild>true</UnoFastDevBuild>
  </PropertyGroup>
</Project>
```

Use `net10.0-ios` instead on a macOS machine for iOS work. Do not commit this file.

## 2. Build the Android runtime and SamplesApp

```powershell
Set-Location src
dotnet build Uno.UI-netcoremobile-only.slnf `
  -c Release `
  -p:UnoTargetFrameworkOverride=net10.0-android `
  -p:NetPrevious=net10.0 `
  -p:UnoFastDevBuild=true

$project = "SamplesApp\SamplesApp.Skia.netcoremobile\SamplesApp.Skia.netcoremobile.csproj"
$properties = @(
  "-p:Configuration=Release",
  "-p:RuntimeIdentifier=android-x64",
  "-p:UnoSampleAppRuntimeIdentifiers=android-x64",
  "-p:UnoTargetFrameworkOverride=net10.0-android",
  "-p:NetPrevious=net10.0",
  "-p:UnoFastDevBuild=true"
)

dotnet restore $project @properties
dotnet clean $project -c Release -f net10.0-android @properties
dotnet publish $project `
  -f net10.0-android `
  -c Release `
  @properties `
  -p:PublishTrimmed=false `
  -p:RunAOTCompilation=false `
  -p:AndroidPackageFormat=apk `
  --no-restore
```

Use a clean publish after changing Android linker or embedding settings. Incremental packaging
can otherwise retain stale marshal registrations. The untrimmed local package also preserves
`kotlinx-coroutines-android`, which is required by the runtime-test app.

The CI-equivalent Skia Android runner is:

```text
build/test-scripts/android-run-skia-runtime-tests.sh
```

It runs against an Android API 34 emulator in the `runtime_tests_skia_android` PR stage.

## 3. Build the iOS runtime and SamplesApp

iOS build and execution require macOS with the repository's normal iOS prerequisites:

```bash
export BUILD_SOURCESDIRECTORY="$(pwd)"
export BUILD_ARTIFACTSTAGINGDIRECTORY="/tmp/artifacts"
bash build/test-scripts/skia-ios-uitest-build.sh
```

The CI-equivalent runtime-test runner is:

```bash
export UITEST_IS_LOCAL=true
export UITEST_AUTOMATED_GROUP=RuntimeTests
export UITEST_RUNTIME_TEST_GROUP=0
export UITEST_RUNTIME_TEST_GROUP_COUNT=4
export UITEST_TEST_TIMEOUT=90m
export SAMPLESAPP_BUNDLE_ID=uno.platform.samplesapp.skia
export UITEST_VARIANT=skia
export UNO_UITEST_IOSBUNDLE_PATH="src/SamplesApp/SamplesApp.Skia.netcoremobile/bin/Release/net10.0-ios/iossimulator-x64/SamplesApp.app"
export BUILD_SOURCESDIRECTORY="$(pwd)"
export BUILD_ARTIFACTSTAGINGDIRECTORY="/tmp/artifacts"
bash build/test-scripts/ios-uitest-run.sh
```

This is the `runtime_tests_skia_ios` PR stage (iOS 17.5 simulator, four groups).

## 4. Run an accessibility-only mobile filter

Runtime tests use a base64-encoded filter. Example for the automation test namespace:

```powershell
$filter = "Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation"
$env:UITEST_RUNTIME_TESTS_FILTER =
  [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($filter))
```

Set the variable before invoking the platform runner. While iterating, prefer a single
class or method name rather than the entire namespace.

## 5. Test-authoring pattern

Add shared and platform tests under:

```text
src/Uno.UI.RuntimeTests/Tests/Windows_UI_Xaml_Automation/
```

### Android native-node assertion

1. Load a control into `WindowHelper.WindowContent`.
2. Obtain the Skia render view's existing `ExploreByTouchHelper`.
3. Use the narrow internal test accessor to resolve the control's virtual ID.
4. Ask the native node provider for `AccessibilityNodeInfoCompat`.
5. Assert the real node: class, name, ID, bounds, state, range/collection data, actions.
6. Perform a native action and assert the Uno control/provider changed.
7. Mutate a peer property and assert node invalidation plus the re-queried value.

Do not assert only a mapping helper or `AutomationPeer` return value; that does not prove
what TalkBack/UIAutomator receives.

### iOS native-element assertion

1. Load a control into the Skia iOS runtime-test host.
2. Resolve its stable `UnoUIAccessibilityElement` through the adapter's internal test accessor.
3. Assert label, hint, value, traits, identifier, frame, container order, and actions.
4. Execute activation/adjustment/custom actions and assert the Uno provider changed.
5. Mutate a peer property and assert the same element object reports the new value.
6. Remove the control and assert it leaves the container and rejects stale actions.

Do not stop at peer-level tests; before this feature, those tests can pass while the Metal
canvas exposes no native elements.

## 6. Automation smoke tests

Reuse:

```text
src/SamplesApp/SamplesApp.UITests/Windows_UI_Xaml_Automation/
```

Add tests that:

- locate Android virtual nodes through the native test ID/resource identity;
- locate iOS elements through `AccessibilityIdentifier`;
- prove AutomationId and accessible name remain distinct;
- activate representative Invoke, Toggle, Selection, ExpandCollapse, RangeValue, Value,
  Scroll, and ScrollItem behaviors;
- confirm secure values are not returned.

No new Appium test project is required. Appium compatibility is validated against the same
native trees through a focused manual smoke matrix.

## 7. Manual TalkBack validation

Use the Skia Android SamplesApp on an API 34 or API 36 emulator, or a representative
physical device:

1. Enable TalkBack.
2. Traverse by swipe and explore by touch.
3. Verify headings, static text, images, controls, lists, dialogs, and virtualized items
   appear in reading order even when they are not keyboard tab stops.
4. Verify role/name/state/value and disabled/read-only announcements.
5. Exercise click, toggle, select, expand/collapse, adjust, edit, and scroll actions.
6. Trigger live regions, notifications, property changes, structure changes, and popups.
7. Confirm the focus highlight matches transformed/scrolled bounds.
8. Navigate between pages repeatedly and check that removed nodes do not remain discoverable.

Use `adb shell uiautomator dump` or an inspector to confirm AutomationId/resource identity is
present and is not copied into the spoken description.

## 8. Manual VoiceOver validation

Use the Skia iOS SamplesApp on an iOS 17+ simulator and a representative physical device:

1. Inspect the Metal view with Accessibility Inspector; it must expose virtual elements.
2. Enable VoiceOver and traverse the same control fixture.
3. Verify label, hint, value, traits, heading/landmark navigation, and container order.
4. Exercise activation, adjustable controls, scrolling, custom actions, and escape/dismiss.
5. Open/close nested modal UI and verify focus stays inside, then restores.
6. Verify dynamic property/tree changes produce layout/screen/announcement notifications.
7. Run XCUITest/Uno.UITest lookup by `AccessibilityIdentifier`.
8. Confirm secure text is never visible in inspector output or test logs.

## 9. Performance and lifecycle checks

Required scenarios:

- 500-node fixture: property/bounds updates complete within the feature's 16 ms p95 target.
- 1,000-item virtualized list: only realized/accessibility-required nodes are registered.
- Add/remove navigation loop: Android reverse-ID maps and iOS element registries return to
  baseline after forced collection.
- Render animation with no semantic changes: no accessibility root invalidation per frame.
- Window close/reopen: no duplicate router owner, listener, native element, or delayed callback.
- Native callback from a non-UI thread: peer work is marshaled safely to the UI thread.

## 10. Evidence discipline

Record separately:

- **Code review**: mapping or lifecycle verified by source inspection.
- **Compile**: exact Android/iOS project and target framework built.
- **Runtime**: exact native-node test, SamplesApp automation test, or TalkBack/VoiceOver
  scenario executed.

Compile-only evidence is not proof that TalkBack, VoiceOver, UIAutomator, or XCUITest can see
the native tree.

## 11. Current local evidence

The Windows implementation worktree has the following reproducible evidence:

```powershell
dotnet build src\SamplesApp\SamplesApp.Skia.Generic\SamplesApp.Skia.Generic.csproj `
  -c Release -f net10.0 `
  -p:UnoFastDevBuild=true `
  -p:UnoTargetFrameworkOverride=net10.0

dotnet build src\Uno.UI.Runtime.Skia.Android\Uno.UI.Runtime.Skia.Android.csproj `
  -c Release -f net10.0-android `
  -p:UnoFastDevBuild=true `
  -p:UnoTargetFrameworkOverride=net10.0-android

dotnet build src\Uno.UI.Runtime.Skia.AppleUIKit\Uno.UI.Runtime.Skia.AppleUIKit.csproj `
  -c Release -f net9.0-ios18.0 -m:1 `
  -p:RuntimeIdentifier=iossimulator-x64 `
  -p:UnoTargetFrameworkOverride=net9.0-ios18.0 `
  -p:UnoFastDevBuild=true `
  -p:WarningsNotAsErrors=NU1701

dotnet build src\Uno.UI-netcoremobile-only.slnf `
  -c Release `
  -p:UnoTargetFrameworkOverride=net10.0-android `
  -p:NetPrevious=net10.0 `
  -p:UnoFastDevBuild=true
```

For the iOS package build, copy `Uno.UI-netcoremobile-only.slnf` to a temporary file and remove
the Android-only `Uno.UI.GooglePlay.netcoremobile` and
`Uno.UI.BindingHelper.Android.netcoremobile` entries, then run:

```powershell
dotnet build <ios-compatible-mobile-subset.slnf> `
  -c Release `
  -p:UnoTargetFrameworkOverride=net10.0-ios `
  -p:NetPrevious=net10.0 `
  -p:UnoFastDevBuild=true `
  -p:WarningsNotAsErrors=NU1701
```

The Android runtime namespace is launched through the app's autostart extras:

```powershell
$filter = [Convert]::ToBase64String(
  [Text.Encoding]::UTF8.GetBytes(
    "Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation"))
$deviceResult =
  "/storage/emulated/0/Android/data/uno.platform.samplesapp.skia/files/mobile-a11y.xml"

adb shell am start `
  -n uno.platform.samplesapp.skia/crc6448f3b0362cbf4bc9.MainActivity `
  -e UITEST_RUNTIME_AUTOSTART_RESULT_FILE $deviceResult `
  -e UITEST_RUNTIME_TESTS_FILTER $filter
```

The direct UIAutomator fixture is executed with the already-installed Skia package:

```powershell
$env:ANDROID_HOME = "$env:LOCALAPPDATA\Android\Sdk"
$env:ANDROID_SDK_ROOT = $env:ANDROID_HOME
$env:ANDROID_SERIAL = "emulator-5580"
$env:UNO_UITEST_PLATFORM = "Android"
$env:UNO_UITEST_APP_ID = "uno.platform.samplesapp.skia"
$env:UITEST_VARIANT = "skia"

dotnet test src\SamplesApp\SamplesApp.UITests\SamplesApp.UITests.csproj `
  -c Release `
  -p:UnoTargetFrameworkOverride=net10.0-android `
  -p:UnoFastDevBuild=true `
  --filter "FullyQualifiedName~MobileAccessibility_Android_UiAutomator_Tests"
```

The AppleUIKit command validates managed source and .NET iOS binding signatures on Windows; it
does not launch a simulator. The Android runtime build may report the repository's existing
`XA0101` content-item warnings.

The pure capability-matrix suite currently covers 39 automation properties, all 34
`PatternInterface` values, all 30 `AutomationEvents` values, state groups, relations, and
unsupported fallbacks. Mobile-gated native-node, lifecycle, performance, and automation tests
compile into the same runtime-test assembly. Android native execution is covered locally; iOS
native execution still requires a macOS/Xcode runner.

Latest local results:

- Skia Desktop `Windows_UI_Xaml_Automation`: 209 passed, 0 failed, 252 platform skips.
- Capability matrix: 25 passed, 0 failed, 4 mobile skips.
- Android API 36 `Windows_UI_Xaml_Automation` with TalkBack and touch exploration enabled:
  328 passed, 0 failed, 133 platform skips.
- Android direct UIAutomator SamplesApp suite with TalkBack enabled: 10 passed, 0 failed.
- Android `Uno.UI-netcoremobile-only.slnf` package build: succeeded with
  `-p:NetPrevious=net10.0`.
- The iOS-compatible `Uno.UI-netcoremobile-only.slnf` subset builds with `net10.0-ios`
  after excluding the Android-only `Uno.UI.GooglePlay.netcoremobile` and
  `Uno.UI.BindingHelper.Android.netcoremobile` projects.
- AppleUIKit managed simulator target: build succeeded with 0 warnings and 0 errors.
- Skia iOS CI now runs the targeted `MobileAccessibility_Tests` simulator group in addition
  to the runtime-test shards.
- `Uno.UI.UnitTests` and `SamplesApp.UITests`: build succeeded; SamplesApp.UITests retains
  its existing NUnit assembly-version warnings.

Android native execution and automation are complete. Native iOS/VoiceOver/XCUITest execution
and the manual cross-platform assistive-technology matrix still require macOS/Xcode and
representative devices.

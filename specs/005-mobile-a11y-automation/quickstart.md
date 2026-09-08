# Quickstart: Mobile Accessibility and Automation

This guide covers the existing Skia mobile build/test paths and the native observables that
the implementation must validate.

## 1. Prepare the mobile build

From the repository root, use a single target framework and fast local build:

```powershell
Copy-Item src\crosstargeting_override.props.sample src\crosstargeting_override.props
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
The commands below explicitly select .NET 10 for local SDK compatibility; CI uses the
SDK/framework versions declared in `.vsts-ci.yml`.

## 2. Build the Android runtime and SamplesApp

```powershell
dotnet build src\Uno.UI.Runtime.Skia.Android\Uno.UI.Runtime.Skia.Android.csproj `
  -c Release -f net10.0-android `
  -p:UnoTargetFrameworkOverride=net10.0-android `
  -p:NetCurrent=net10.0 -p:NetPrevious=net9.0 `
  -p:UnoFastDevBuild=true

$project = "src\SamplesApp\SamplesApp\SamplesApp.csproj"
$properties = @(
  "-p:Configuration=Release",
  "-p:UnoTargetFrameworkOverride=net10.0-android",
  "-p:NetCurrent=net10.0",
  "-p:NetPrevious=net9.0",
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
  -p:DoNotSetAndroidLinkTool=true `
  "-p:AndroidLinkTool=" `
  -p:AndroidEnableProguard=false `
  -p:AndroidEnableMarshalMethods=false `
  -p:AndroidPackageFormat=apk `
  --no-restore
```

Use a clean publish after changing Android linker or embedding settings. Incremental packaging
can otherwise retain stale marshal registrations. These local-test flags disable both managed
trimming, Java shrinking, and optimized JNI marshal methods; this package exercises native
accessibility but does not establish release-shrinker, marshal-method, or AOT correctness.
Leave runtime identifiers to the head project rather than
forcing an Android RID onto its generic project references.

The CI-equivalent Skia Android runner is:

```text
build/test-scripts/android-run-skia-runtime-tests.sh
```

It runs against an Android API 34 emulator in the `runtime_tests_skia_android` stage.
Its CI setup owns its emulator and can reboot devices; on a shared machine use an explicitly
selected emulator serial instead of running that setup against someone else's emulator.

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
export UNO_UITEST_IOSBUNDLE_PATH="src/SamplesApp/SamplesApp/bin/Release/net10.0-ios/iossimulator-x64/SamplesApp.app"
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

While iterating, prefer a single class or method name rather than the entire namespace.
For Android, pass the encoded value as the `UITEST_RUNTIME_TESTS_FILTER` intent extra;
the CI shell runner replaces that variable with its shard/retry filter.

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

## 11. Integrated-head validation

The unified SamplesApp head replaces the old Generic and netcoremobile heads. Build the
desktop runtime-test host with:

```powershell
dotnet build src\SamplesApp\SamplesApp\SamplesApp.csproj `
  -c Release -f net10.0-desktop `
  -p:UnoTargetFrameworkOverride=net10.0-desktop `
  -p:NetCurrent=net10.0 -p:NetPrevious=net9.0 `
  -p:UnoFastDevBuild=true
```

For an installed Android test APK, select the device explicitly and resolve the generated
launcher activity rather than hardcoding a Java wrapper name:

```powershell
$serial = $env:ANDROID_SERIAL
if ([string]::IsNullOrEmpty($serial)) { throw "Select the test emulator with ANDROID_SERIAL." }
$package = "uno.platform.samplesapp.skia"
$activity = (adb -s $serial shell cmd package resolve-activity --brief $package)[-1].Trim()
$filter = [Convert]::ToBase64String(
  [Text.Encoding]::UTF8.GetBytes(
    "Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation"))
$deviceResult = "/storage/emulated/0/Android/data/$package/files/mobile-a11y.xml"
adb -s $serial shell am start -n $activity `
  -e UITEST_RUNTIME_TEST_GROUP 0 `
  -e UITEST_RUNTIME_TEST_GROUP_COUNT 1 `
  -e UITEST_RUNTIME_AUTOSTART_RESULT_FILE $deviceResult `
  -e UITEST_RUNTIME_TESTS_FILTER $filter
```

Record the tested commit, package hash, framework, device, NUnit counts, and any local
packaging overrides with the PR results. Parse NUnit results rather than trusting the
application exit code. Results from a pre-integration package do not validate a new merge.

`Given_SkiaIOSAccessibilityElement.PeerContracts.skia.cs` covers ownerless peers, retained nodes
after EventsSource rebinding/unload, peer identifier/culture overrides, and child-initiated
ancestor scrolling. These require native iOS execution; compiling the tests on Windows
only establishes API/build compatibility. The shared and native mobile action cases also
cover exact Int32 view IDs, including fractional and out-of-range inputs. Backend-only
fixtures use the `.skia.cs` suffix so they do not reference Uno internals when compiling
the native WinUI test project; public WinUI contract tests remain available there.

A PR stacked on `dev/doti/a11y-parity-remediation-impl` is outside `.vsts-ci.yml`'s automatic
PR branch filter. Keeping that stack does not establish a green native CI result: the
current integrated head still needs the required Android/iOS stages on a supported runner
before full merge readiness can be claimed.

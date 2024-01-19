---
uid: Uno.Features.WS
---

# URI Launcher

> [!TIP]
> This article covers Uno-specific information for `Windows.System` namespace. For a full description of the feature and instructions on using it, see [Windows.System Namespace](https://learn.microsoft.com/uwp/api/windows.system).

* The `Windows.System.Launcher` class provides functionality for launching URIs and apps.

## `LaunchUriAsync`

This API is supported on iOS, Android, WASM, and macOS.

On iOS, Android, and macOS the `ms-settings:` special URI is supported.

### Platform-specifics

On iOS, launching the special URI opens the main page of system settings because deep-linking to specific settings is not available.

For WASM, launching the special URI will work properly only when opening the website on Windows. The method will return `true` even if the user cancels the application launch, as there is currently no way to detect if the app was successfully launched.

In case of Android, we support the following nested URIs.

| Settings URI | Android Mapping |
|--------------|----------|
| `ms-settings:sync` | `Settings.ActionSyncSettings` |
| `ms-settings:appsfeatures-app` | `Settings.ActionApplicationDetailsSettings` |
| `ms-settings:appsfeatures` | `Settings.ActionApplicationSettings` |
| `ms-settings:defaultapps` | `Settings.ActionManageDefaultAppsSettings` |
| `ms-settings:appsforwebsites` | `Settings.ActionManageDefaultAppsSettings` |
| `ms-settings:cortana` | `Settings.ActionVoiceInputSettings` |
| `ms-settings:bluetooth` | `Settings.ActionBluetoothSettings` |
| `ms-settings:printers` | `Settings.ActionPrintSettings` |
| `ms-settings:typing` | `Settings.ActionHardKeyboardSettings` |
| `ms-settings:easeofaccess` | `Settings.ActionAccessibilitySettings` |
| `ms-settings:network-airplanemode` | `Settings.ActionAirplaneModeSettings` |
| `ms-settings:network-celluar` | `Settings.ActionNetworkOperatorSettings` |
| `ms-settings:network-datausage` | `Settings.ActionDataUsageSettings` |
| `ms-settings:network-wifiSettings` | `Settings.ActionWifiSettings` |
| `ms-settings:nfctransactions` | `Settings.ActionNfcSettings` |
| `ms-settings:network-vpn` | `Settings.ActionVpnSettings` |
| `ms-settings:network-wifi` | `Settings.ActionWifiSettings` |
| `ms-settings:network` | `Settings.ActionWirelessSettings` |
| `ms-settings:personalization` | `Settings.ActionDisplaySettings` |
| `ms-settings:privacy` | `Settings.ActionPrivacySettings` |
| `ms-settings:about` | `Settings.ActionDeviceInfoSettings` |
| `ms-settings:apps-volume` | `Settings.ActionSoundSettings` |
| `ms-settings:batterysaver` | `Settings.ActionBatterySaverSettings` |
| `ms-settings:display` | `Settings.ActionDisplaySettings` |
| `ms-settings:screenrotation` | `Settings.ActionDisplaySettings` |
| `ms-settings:quiethours` | `Settings.ActionZenModePrioritySettings` |
| `ms-settings:quietmomentshome` | `Settings.ActionZenModePrioritySettings` |
| `ms-settings:nightlight` | `Settings.ActionNightDisplaySettings` |
| `ms-settings:taskbar` | `Settings.ActionDisplaySettings` |
| `ms-settings:notifications` | `Settings.ActionAppNotificationSettings` |
| `ms-settings:storage` | `Settings.ActionInternalStorageSettings` |
| `ms-settings:sound` | `Settings.ActionSoundSettings` |
| `ms-settings:dateandtime` | `Settings.ActionDateSettings` |
| `ms-settings:keyboard` | `Settings.ActionInputMethodSettings` |
| `ms-settings:regionlanguage` | `Settings.ActionLocaleSettings` |
| `ms-settings:developers` | `Settings.ActionApplicationDevelopmentSettings` |

In case of macOS, Uno supports the following nested URIs, mapped to Preference Panes (/System/Library/PreferencePanes)

| Settings URI | macOS Mapping |
|--------------|----------|
| `ms-settings:signinoptions-launchfaceenrollment` | `TouchID` |
| `ms-settings:launchfingerprintenrollment` | `TouchID` |
| `ms-settings:signinoptions` | `Accounts` |
| `ms-settings:emailandaccounts` | `InternetAccounts` |
| `ms-settings:appsforwebsites` | `Settings.ActionManageDefaultAppsSettings` |
| `ms-settings:tabletmode` | `Expose` |
| `ms-settings:personalization-start` | `Expose` |
| `ms-settings:personalization-background` | `DesktopScreenEffectsPref` |
| `ms-settings:personalization` | `Appearance` |
| `ms-settings:bluetooth` | `Bluetooth` |
| `ms-settings:dateandtime` | `DateAndTime` |
| `ms-settings:region` | `Localization` |
| `ms-settings:typing` | `Keyboard` |
| `ms-settings:display` | `Displays` |
| `ms-settings:screenrotation` | `Displays` |
| `ms-settings:taskbar` | `Dock` |
| `ms-settings:batterysaver` | `EnergySaver` |
| `ms-settings:powersleep` | `EnergySaver` |
| `ms-settings:otherusers` | `FamilySharingPrefPane` |
| `ms-settings:mousetouchpad` | `Mouse` |
| `ms-settings:devices-touchpad` | `Trackpad` |
| `ms-settings:network` | `Network` |
| `ms-settings:privacy-notifications` | `Notifications` |
| `ms-settings:printers` | `PrintAndFax` |
| `ms-settings:privacy` | `Security` |
| `ms-settings:crossdevice` | `SharingPref` |
| `ms-settings:quiethours` | `ScreenTime` |
| `ms-settings:quietmomentshome` | `ScreenTime` |
| `ms-settings:sound` | `Sound` |
| `ms-settings:windowsupdate` | `SoftwareUpdate` |
| `ms-settings:cortana-windowssearch` | `Spotlight` |
| `ms-settings:cortana` | `Speech` |
| `ms-settings:storage` | `StartupDisk` |
| `ms-settings:backup` | `TimeMachine` |
| `ms-settings:easeofaccess` | `UniversalAccessPref` |

### Exceptions

* When `uri` argument is `null`, `NullReferenceException` is thrown. Note this differs from UWP where `AccessViolationException` is thrown.
* When the method is called from non-UI thread, `InvalidOperationException` is thrown.

Exceptions are in line with UWP.

## `QueryUriSupportAsync`

This API is supported on iOS, Android, and macOS, and the implementation does not respect the `LaunchQuerySupportType` parameter yet. It also reports the aforementioned special `ms-settings` URIs on Android and iOS as supported.

### Platform-specific requirements

#### Android

When targeting Android 11 (API 30) or newer, you may notice the `QueryUriSupportAsync` returning false. To avoid this, make sure to add any URL schemes passed to it as `<queries>` entries in your `AndroidManifest.xml`:

```xml
<queries>
  <intent>
    <action android:name="android.intent.action.VIEW" />
    <data android:scheme="tel" />
  </intent>

  <intent>
    <action android:name="android.intent.action.VIEW" />
    <category android:name="android.intent.category.DEFAULT" />
    <category android:name="android.intent.category.BROWSABLE" />
    <data android:scheme="https" />
  </intent>
</queries>
```

#### iOS

Add any URL schemes passed to `QueryUriSupportAsync` as `LSApplicationQueriesSchemes` entries in your `Info.plist` file, otherwise, it will return false:

```xml
<key>LSApplicationQueriesSchemes</key>
<array>
  <string>tel</string>
  <string>https</string>
</array>
```

### Exceptions

* When `uri` argument is `null`, `NullReferenceException` is thrown. Note this differs from UWP where a plain `Exception` with HRESULT is thrown.

Exceptions are in line with UWP.

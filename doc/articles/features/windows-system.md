# Uno Support for Windows.System APIs

## `Launcher`

### `LaunchUriAsync` 

This API is supported on iOS, Android, WASM and macOS.

On iOS, Android and macOS the `ms-settings:` special URI is supported. 

In case of iOS, any such URI opens the main page of system settings (there is no settings deep-linking available on iOS).

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
| `ms-settings:signinoptions ` | `Accounts` |
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

#### Exceptions

- When `uri` argument is `null`, `NullReferenceException` is thrown. Note this differs from UWP where `AccessViolationException` is thrown.
- When the method is called from non-UI thread `InvalidOperationException` is thrown.

Exceptions are in line with UWP.

### `QueryUriSupportAsync` 

This API is supported on iOS, Android and macOS, and the implementation does not respect the `LaunchQuerySupportType` parameter yet. It also reports the aforementioned special `ms-settings` URIs on Android and iOS as supported.

#### Exceptions

- When `uri` argument is `null`, `NullReferenceException` is thrown. Note this differs from UWP where a plain `Exception` with HRESULT is thrown.

Exceptions are in line with UWP.

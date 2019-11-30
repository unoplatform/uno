# Uno Support for Windows.System APIs

## `Launcher`

### `LaunchUriAsync` 

This API is supported on iOS, Android and WASM.

On iOS and Android, the `ms-settings:` special URI is supported. 

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


#### Exceptions

- When `uri` argument is `null`, `NullReferenceException` is thrown. Note this differs from UWP where `AccessViolationException` is thrown.
- When the method is called from non-UI thread `InvalidOperationException` is thrown.

Exceptions are in line with UWP.

### `QueryUriSupportAsync` 

This API is supported on iOS and Android and the implementation does not respect the `LaunchQuerySupportType` parameter yet. It also reports the aforementioned special `ms-settings` URIs on Android and iOS as supported.

#### Exceptions

- When `uri` argument is `null`, `NullReferenceException` is thrown. Note this differs from UWP where a plain `Exception` with HRESULT is thrown.

Exceptions are in line with UWP.

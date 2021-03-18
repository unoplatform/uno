# Uno Support for Windows.ApplicationModel

## `PackageId`

**Android**
- Due to the fact that version strings can be arbitrary on Android, we first try to parse `PackageInfo.VersionName`, if it fails, we try to use `PackageInfo.LongVersionCode` and if that fails (this is a `long` value while UWP uses `ushort`), we fall back to default to avoid exceptions (1.0.0.0).
- `FamilyName` and `Name` properties have the same value as the `Publisher` cannot be retrieved programmatically.
- `FullName` property ends with the value of `LongVersionCode` similarly to UWP where it contains the app version.

**iOS**
- Version strings may potentially be arbitrary on iOS as well (such version string would fail validation only during App Store upload), so we first try to parse the user-facing `CFBundleShortVersionString`, if it fails, we try to use `CFBundleVersion` and if that fails, we fall back to default to avoid exceptions (1.0.0.0).
- `FamilyName` and `Name` properties have the same value as the `Publisher` cannot be retrieved programmatically.
- `FullName` property ends with the value of `CFBundleVersion` similarly to UWP where it contains the app version.
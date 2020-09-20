# Uno Support for Windows.System.UserProfile APIs

## `UserProfilePersonalizationSettings`

The functionality of this class is available only on Android. On other platforms the `IsSupported()` method always returns `false`.

To be able to set wallpaper and lock screen image, add the following permission to the Android project head:

```
[assembly: UsesPermission("android.permission.SET_WALLPAPER")]
```

To set the wallpaper, use the following code snippet:

```
using Windows.System.UserProfile;

private async Task<bool> SetWallpaperAsync(StorageFile imageFile) 
{ 
    var success = false;
    if (UserProfilePersonalizationSettings.IsSupported())
    {
        var profileSettings = UserProfilePersonalizationSettings.Current;
        success = await profileSettings.TrySetWallpaperImageAsync(file);
    }
    return success;
} 
```

Analogously for `TrySetLockScreenImageAsync`.
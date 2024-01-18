---
uid: Uno.Features.WSUserProfile
---

# Wallpaper and Lock Screen

> [!TIP]
> This article covers Uno-specific information for `Windows.System.UserProfile` namespace. For a full description of the feature and instructions on using it, consult the UWP documentation: https://learn.microsoft.com/en-us/uwp/api/windows.system.userprofile

* The `Windows.System.UserProfile.UserProfilePersonalizationSettings` class provides functionality for setting the lock screen and wallpaper images.

The functionality of this class is available only on Android. On other platforms the `IsSupported()` method always returns `false`.

To be able to set wallpaper and lock screen image, add the following permission to the Android project head:

```csharp
[assembly: UsesPermission("android.permission.SET_WALLPAPER")]
```

To set the wallpaper, use the following code snippet:

```csharp
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

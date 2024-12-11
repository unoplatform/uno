---
uid: Uno.Features.Capture
---

# Capture

> [!TIP]
> This article covers Uno-specific information for the `Windows.Media.Capture` namespace. For a full description of the feature and instructions on using it, see [Windows.Media.Capture Namespace](https://learn.microsoft.com/uwp/api/windows.media.capture).

- The `Windows.Media.Capture` namespace provides classes for the capture of photos, audio recordings, and videos.

## `CameraCaptureUI`

`CameraCaptureUI` is currently only supported on Android, iOS, and UWP. On other platforms, `CaptureFile` will return `null`.

### Platform-specific

#### Android

If you are planning to use the `CameraCaptureUI`, your app must declare `android.permission.CAMERA` and `android.permission.WRITE_EXTERNAL_STORAGE` permissions, otherwise the functionality will not work as expected:

```csharp
[assembly: UsesPermission("android.permission.CAMERA")]
[assembly: UsesPermission("android.permission.WRITE_EXTERNAL_STORAGE")]
```

#### iOS

On iOS, `CameraCaptureUI` uses the native UIImagePickerController for capturing media. Ensure that the `NSCameraUsageDescription` and `NSMicrophoneUsageDescription` keys are added to the `Info.plist` file to request the necessary permissions.

#### WinUI/UWP

On UWP, `CameraCaptureUI` provides a unified interface for capturing photos and videos, fully leveraging the platform's APIs. WinUI support is coming with v1.7+.

### Example

```csharp
#if __ANDROID__ || __IOS__ || __WINDOWS__
using Windows.Media.Capture;
#endif

public async Task CapturePhotoAsync()
{
#if __ANDROID__ || __IOS__ || __WINDOWS__
    var captureUI = new CameraCaptureUI();
    captureUI.PhotoSettings.Format = CameraCaptureUIPhotoFormat.Jpeg;
    
    var file = await captureUI.CaptureFileAsync(CameraCaptureUIMode.Photo);

    if (file != null)
    {
        // Handle the captured file (e.g., save or display it)
    }
    else
    {
        // Handle the cancellation or error
    }
#endif
}
```

You can also check out our [sample](https://github.com/unoplatform/Uno.Samples/tree/master/UI/CameraCaptureUI) for more details.

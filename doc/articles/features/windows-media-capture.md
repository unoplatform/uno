---
uid: Uno.Features.Capture
---

# Capture

> [!TIP]
> This article covers Uno-specific information for the `Windows.Media.Capture` namespace. For a full description of the feature and its instructions, see [Windows.Media.Capture Namespace](https://learn.microsoft.com/uwp/api/windows.media.capture).

The `Windows.Media.Capture` namespace provides classes for capturing photos, audio recordings, and videos.

## `CameraCaptureUI`

`CameraCaptureUI` is currently supported on Android, iOS, macOS (Skia Desktop), and WinUI. On other platforms, `CaptureFileAsync` will return `null`.

> [!IMPORTANT]
> `CaptureFileAsync` should only be called from the UI thread. Calling them from a background thread will throw an `InvalidOperationException`.

### Platform-specific

#### Android

If you are planning to use the `CameraCaptureUI`, your app must declare `android.permission.CAMERA` and `android.permission.WRITE_EXTERNAL_STORAGE` permissions, otherwise the functionality will not work as expected:

```csharp
[assembly: UsesPermission("android.permission.CAMERA")]
[assembly: UsesPermission("android.permission.WRITE_EXTERNAL_STORAGE")]
```

#### iOS

On iOS, CameraCaptureUI uses the native `UIImagePickerController` to capture media. To request the necessary permissions, ensure that the `NSCameraUsageDescription` and `NSMicrophoneUsageDescription` keys are added to the `Info.plist` file.

> [!NOTE]
> The `NSMicrophoneUsageDescription` key is required only if you are capturing videos. If you are only capturing photos, you can omit this key.

```xml
<?xml version="1.0" encoding="utf-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>NSCameraUsageDescription</key>
    <string>We need access to the camera to take photos.</string>
    <key>NSMicrophoneUsageDescription</key>
    <string>We need access to the microphone to record videos.</string>
</dict>
</plist>
```

> [!IMPORTANT]
> iOS simulators do not have access to a camera. To test the camera functionality, you need to run the app on a physical device. When using a simulator, your app will open the Photo Library instead of the camera, but the functionality will work as expected once the app is deployed to a physical device.

#### macOS

On macOS (Skia Desktop), `CameraCaptureUI` uses the native `IKPictureTaker` API to capture photos from the built-in FaceTime camera or external USB cameras. No special permissions are required in the app manifest, but the system will automatically prompt the user for camera access the first time the app attempts to use the camera.

> [!NOTE]
> Video capture is not yet supported on macOS. Calling `CaptureFileAsync` with `CameraCaptureUIMode.Video` will return `null`.

#### WinUI

On WinUI, `CameraCaptureUI` provides a unified interface for capturing photos and videos, fully leveraging the platform's APIs. WinUI support is coming with v1.7+.

### Example

```csharp
#if __ANDROID__ || __IOS__ || __WINDOWS__ || __SKIA__
using Windows.Media.Capture;
#endif

public async Task CapturePhotoAsync()
{
#if __ANDROID__ || __IOS__ || __WINDOWS__ || __SKIA__
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

---
uid: Uno.Features.Orientation
---

# Controlling orientation

The application's orientation can be controlled using the [DisplayInformation](https://msdn.microsoft.com/en-us/library/windows/apps/windows.graphics.display.displayinformation.aspx) class, more specifically, the static [AutoRotationPreferences](https://msdn.microsoft.com/en-us/library/windows/apps/windows.graphics.display.displayinformation.autorotationpreferences) property.

## Controlling Orientation on iOS

In order for the DisplayInformation's AutoRotationPreferences to work properly, you need to ensure that all potential orientations are supported within the iOS application's `info.plist` file.

**Warning**
On iOS 9 and above, the system does not allow iPad applications to dictate their orientation if they support [Multitasking / Split View](https://developer.apple.com/library/prerelease/content/documentation/WindowsViews/Conceptual/AdoptingMultitaskingOniPad/). In order to control orientation through the DisplayInformation class, you will need to opt-out of Multitasking / Split View by ensuring that you have defined the following in your `info.plist`:

```xml
<key>UIRequiresFullScreen</key>
<true/>
```

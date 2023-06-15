---
uid: Uno.Features.AndroidTv
---

# Support for Android TV

Uno Platform is proud to support Android TV, enabling you to extend your application's reach to this wide family of devices with unique use cases.

## Enabling Android TV support


## Remote control support

The integration of focus management allows the Android TV remote control to work seamlessly, just like normal keyboard focus navigation. To navigate the focus in your application via the the directional pad of the remote control, you need to make sure `XYFocusKeyboardNavigation` is `Enabled` on all your pages:

```xaml
<Page xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
      XYFocusKeyboardNavigation="Enabled">
    <!-- Your app's UI elements go here -->
</Page>
```

To disable the native Android highlighting of focused elements, the `styles.xml` file needs to be updated to make the highlight transparent:

```xml
<item name="android:colorControlHighlight">@android:color/transparent</item>
```

Please note, that this will disable all the highlights, even in embedded native controls you may host within the Uno Platform app.
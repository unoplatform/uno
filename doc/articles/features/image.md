---
uid: Uno.Features.Image
---

# The Image Control

This page details the specific aspects of the `Image` control for Uno Platform. General information about `Image` is available in WinUI [official documentation](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.image).

## Support for `ms-appx`

`ms-appx` supports reading images provided as `Content` assets in your project. See [`StorageFile.GetFileFromApplicationUriAsync`](xref:Uno.Features.FileManagement#support-for-storagefilegetfilefromapplicationuriasync) for more details.

## Support for `ms-appdata`

[`ms-appdata:///`](hhttps://learn.microsoft.com/windows/uwp/app-resources/uri-schemes#path-ms-appdata) supports reading content from the some known folders of an app.

For instance, these are paths which can be used to show images:

- `ms-appdata:///local/myimage.png`, using the path from `ApplicationData.Current.LocalFolder`
- `ms-appdata:///temp/myimage.png`, using the path from `ApplicationData.Current.TemporaryFolder`
- `ms-appdata:///roaming/myimage.png`, using the path from `ApplicationData.Current.RoamingFolder`

Files written at those locations will be shown in the `Image` control.

Here's a simple example:

```csharp
var httpClient = new HttpClient();
var response = await httpClient.GetAsync("https://fakeimg.pl/300/");

var file = await ApplicationData.Current.LocalFolder.CreateFileAsync("image.jpg", CreationCollisionOption.ReplaceExisting);
using (var stream = await file.OpenStreamForWriteAsync())
{
    await response.Content.CopyToAsync(stream);
}

MyImage.UriSource = new BitmapImage { UriSource = new($"ms-appdata:///local/image.jpg") };
```

> [!IMPORTANT]
> When using WinAppSDK Unpackaged mode, `ms-appdata:///` is not supported on `Image`. In this case, you can use [`BitmapImage.SetSourceAsync`](https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.media.imaging.bitmapsource.setsourceasync) to set the image programmatically.

## Gif Support

Displaying animated GIFs is supported on:

- netX.0-desktop (5.4 and later)
- netX.0-windows
- netX.0-browserwasm

Unsupported targets, where only the first frame is shown, as of Uno Platform 5.4:

- netX.0-ios
- netX.0-android

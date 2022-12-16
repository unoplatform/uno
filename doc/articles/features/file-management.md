# File Management

> [!TIP]
> This article covers Uno-specific information for file management. For a full description of the feature and instructions on using it, consult the UWP documentation: https://docs.microsoft.com/en-us/windows/uwp/files/

 * File management allows shared reading and writing of files across all Uno Platform targets

## Supported features

| Feature        |  Windows  | Android |  iOS  |  Web (WASM)  | macOS | Linux (Skia)  | Win 7 (Skia) | 
|---------------|-------|-------|-------|-------|-------|-------|-|
| `StorageFile` | ✔ | ✔ | ✔| ✔ | ✔| ✔ |✔ |
| `StorageFolder` | ✔ | ✔ | ✔| ✔ | ✔| ✔ |✔ |
| `ApplicationData.Current.LocalFolder` | ✔ | ✔ | ✔| ✔ | ✔| ✔ |✔ |
| `ApplicationData.Current.RoamingFolder` | ✔ | ✔ | ✔| ✔ | ✔| ✔ |✔ |
| `CachedFileManager` | ✔ | partial | partial | partial | partial | partial | partial |
| `StorageFileHelper` | X | ✔ | ✔| ✔ | ✔| ✔ |✔ |

## Overview

Uno supports some of the APIs from the `Windows.Storage` namespace, such as `Windows.Storage.StorageFile` and `Windows.Storage.StorageFolder` for all platforms.

Both `Windows.Storage` and `System.IO` APIs are available, with some platform specifics defined below. In general, it is best to use `Windows.Storage` APIs when available, as their asynchronous nature allows for transparent interactions with the underlying file system implementations. In addition, `System.IO` cannot work with files that are not owned by the application directly (e.g. files picked by a dialog).

Note that for file and folder metadata only `BasicProperties` are partially supported for now. 
`FileAttributes` and all "advanced properties" (`StorageItemContentProperties`) related to the content of the file, including the thumbnail, are not yet supported.

## WebAssembly File System

WebAssembly file system APIs are built using [emscripten's POSIX file system APIs](https://emscripten.org/docs/api_reference/Filesystem-API.html). The persistence is done through the use of browser APIs, such as IndexedDB through [emscripten's IDBFS](https://emscripten.org/docs/api_reference/Filesystem-API.html#filesystem-api-idbfs).

While it is possible to write files in any paths, only some folders are persisted across browser refreshes:
- `ApplicationData.Current.LocalFolder`
- `ApplicationData.Current.RoamingFolder`
- `ApplicationData.Current.SharedLocalFolder`

Note that the initialization of the filesystem is asynchronous. This means that reading a file during `Application.OnLaunched` using `System.IO.File.OpenRead` may fail to find a file that was previously written, because the filesystem is not available yet.

The optimal way to open a file is to use the following:

```csharp
var localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
var folder = await localFolder.CreateFolderAsync("myFolder", CreationCollisionOption.OpenIfExists);
File.WriteAllText(Path.Combine(folder.Path, "MyFile.txt"), DateTime.Now.ToLongDateString());
```

Note that for WebAssembly in particular, the `await localFolder.CreateFolderAsync` is important to ensure that the file system has properly been initialized from the IDBFS persistence. Any asynchronous operation from StorageFolder awaits for the filesystem's initialization before continuing.

Note that you can view the content of the **IndexedDB** in the Application tab of your browser, in the **Storage / IndexedDB** section.

## Support for `StorageFile.GetFileFromApplicationUriAsync`

Uno Platform supports the ability to get package files using the [`StorageFile.GetFileFromApplicationUriAsync`](https://docs.microsoft.com/en-us/uwp/api/windows.storage.storagefile.getfilefromapplicationuriasync).

Support per platform may vary:
- On non-WebAssembly targets, the file is available directly as it is a part of the installed package.
- On WebAssembly, the requested file is part of the application package on the remote server and is downloaded on demand to avoid increasing the initial application payload size. After it is requested for the first time, the file is then stored in the browser IndexedDB.

Here's how to use it:

```csharp
var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///MyPackageFile.xml"));
var content = await FileIO.ReadTextAsync(file);
```
Given than in the project there's the following declaration:
```xml
<ItemGroup>
    <Content Include="MyPackageFile.xml" />
</ItemGroup>
```

### Support for Library provided assets
Since Uno Platform 4.6, the `GetFileFromApplicationUriAsync` method supports reading assets provided by `ProjectReference` or `PackageReference` libraries, using the following syntax:

Given a library or package named `MyLibray01`, the following can be used to read assets:
```csharp
var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx://MyLibray01/MyPackageFile.xml"));
var content = await FileIO.ReadTextAsync(file);
```

Uno Platform also provides the ability to determine if an asset or resource exists in the application package by using `StorageFileHelper.ExistsInPackage`:
```csharp
var fileExists = await StorageFileHelper.ExistsInPackage("Assets/Fonts/uno-fluentui-assets.ttf");
```
> [!IMPORTANT]
> `StorageFileHelper.ExistsInPackage` is only available for Uno Platform based targets, but not for WinAppSDK.
## Support for `RandomAccessStreamReference.CreateFromUri`

Uno Platform supports the creation of a `RandomAccessStreamReference` from an `Uri` (`RandomAccessStreamReference.CreateFromUri`), but note that on WASM downloading a file from a server often causes issues with [CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS). 
Make sure the server that hosts the file is configured accordingly.

## Support for `CachedFileManager`

For all targets except for UWP/WinUI and WebAssembly, the `CachedFileManager` does not provide any functionality and its methods immediately return. This allows us to easily write code that requires deferring updates on UWP and sharing it across all targets.

In the case of WebAssembly, the behavior of `CachedFileManager` depends on whether the app uses the **File System Access API** or **Download picker**. This is described in detail in [file pickers documentation](windows-storage-pickers.md#webassembly).
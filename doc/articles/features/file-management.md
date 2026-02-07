---
uid: Uno.Features.FileManagement
---

# File Management

File management allows shared reading and writing of files across all Uno Platform targets. This includes the ability to read files from the application package, as well as the ability to read and write files from the file system.

> [!TIP]
> This article covers Uno-specific information for file management. For a full description of the feature and instructions on using it, see [Files, folders, and libraries](https://learn.microsoft.com/windows/uwp/files/).

## Supported features

| Feature             | WinUI     | Android | iOS     | Web (WASM) | macOS   | Linux (Skia) | WPF (Skia) |
|---------------------|-----------|---------|---------|------------|---------|--------------|------------|
| `StorageFile`       | ✔         | ✔       | ✔       | ✔          | ✔       | ✔            | ✔          |
| `StorageFolder`     | ✔         | ✔       | ✔       | ✔          | ✔       | ✔            | ✔          |
| `CachedFileManager` | ✔         | partial | partial | partial    | partial | partial      | partial    |
| `StorageFileHelper` | ✔         | ✔       | ✔       | ✔          | ✔       | ✔            | ✔          |

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

Uno Platform supports the ability to get package files using the [`StorageFile.GetFileFromApplicationUriAsync(Uri)`](https://learn.microsoft.com/uwp/api/windows.storage.storagefile.getfilefromapplicationuriasync) method.

Support per platform may vary:

- On WebAssembly targets, the requested file is part of the application package on the remote server and is downloaded on demand to avoid increasing the initial application payload size. After it is requested for the first time, the file is then stored in the browser IndexedDB.
- Otherwise, the file is available directly as it is a part of the installed package.

### General usage instructions

Ensure that a declaration exists in your project file like the following:

```xml
<ItemGroup>
    <Content Include="MyPackageFile.xml" />
</ItemGroup>
```

A URI with the `ms-appx:///` scheme can then be used to read a file's content:

```csharp
var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///MyPackageFile.xml"));
var content = await FileIO.ReadTextAsync(file);
```

### Support for Library provided assets

Since Uno Platform 4.6, the `GetFileFromApplicationUriAsync` method supports reading assets provided by `ProjectReference` or `PackageReference` libraries, using the following syntax:

Given a library or package named `MyLibrary01`, the following format can be used to read assets:

```csharp
var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///MyLibrary01/MyPackageFile.xml"));
var content = await FileIO.ReadTextAsync(file);
```

Uno Platform also provides the ability to determine if an asset or resource exists in the application package by using `StorageFileHelper.ExistsInPackage`:

```csharp
var fileExists = await StorageFileHelper.ExistsInPackage("Assets/Fonts/uno-fluentui-assets.ttf");
```

### Loading JSON data files

A common scenario is loading JSON data files packaged with your application. **Always use `StorageFile.GetFileFromApplicationUriAsync` instead of `System.IO.File` APIs** for cross-platform compatibility.

**Example:**

```csharp
public async Task<string> LoadJsonFileAsync(string resourcePath)
{
    // Use StorageFile API for cross-platform support
    var uri = new Uri(resourcePath);
    var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(uri);
    return await Windows.Storage.FileIO.ReadTextAsync(file);
}

// Usage:
var json = await LoadJsonFileAsync("ms-appx:///AppData/Recipes.json");
var recipes = JsonSerializer.Deserialize<List<Recipe>>(json);
```

**Why this matters:**
- `System.IO.File` APIs work with physical file paths and don't understand `ms-appx://` URIs
- On WebAssembly, files are downloaded on-demand and stored in IndexedDB, not the file system
- On mobile platforms, the app package structure differs from desktop
- `StorageFile.GetFileFromApplicationUriAsync` handles all platform-specific details automatically

**Project setup:**

Ensure your JSON files are marked as `Content` in your project file:

```xml
<ItemGroup>
    <Content Include="AppData\**\*.json" />
</ItemGroup>
```

## Support for `RandomAccessStreamReference.CreateFromUri`

Uno Platform supports the creation of a `RandomAccessStreamReference` from an `Uri` (`RandomAccessStreamReference.CreateFromUri`), but note that on WASM downloading a file from a server often causes issues with [CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS).
Make sure the server that hosts the file is configured accordingly.

## Support for `CachedFileManager`

For all targets except WinUI and WebAssembly, the `CachedFileManager` does not provide any functionality, and its methods immediately return. This allows us to easily write code that requires deferring updates on Windows but is shared across all targets.

In the case of WebAssembly, the behavior of `CachedFileManager` depends on whether the app uses the **File System Access API** or **Download picker**. This is described extensively within the [documentation](xref:Uno.Features.WSPickers#webassembly) for storage pickers.

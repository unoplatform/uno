# Windows.Storage

Uno supports some of the APIs from the `Windows.Storage` namespace, such as `Windows.Storage.StorageFile` and `Windows.Storage.StorageFolder` for all platforms.

Both `Windows.Storage` and `System.IO` APIs are available, with some platform specifics defined below. In general, it is best to use `Windows.Storage` APIs when available, as their async nature allows for transparent interactions with the underlying file system implementations.

Note that only `BasicProperties` are barely supported for now. 
`FileAttributes` and all "advanced properties" (`StorageItemContentProperties`) related to the content of the file, including thumbnail, are not supported.

## WebAssembly support

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

## Support for StorageFile.GetFileFromApplicationUriAsync

Uno supports the ability to get package files using the [`StorageFile.GetFileFromApplicationUriAsync`](https://docs.microsoft.com/en-us/uwp/api/windows.storage.storagefile.getfilefromapplicationuriasync).

Support per platform may vary:
- On Android, iOS/macOS, the file is available immediately as it is part of the installed package.
- On WebAssembly, the requested file is part of the remote package and is downloaded on demand to avoid increasing the initial application payload size. The file is then stored in the IndexedDB of the browser.

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

## Support for StorageFile.CreateStreamedFileAsync and StorageFile.CreateStreamedFileFromUriAsync

Those methods are not supported yet, however Uno supports to create a `RandomAccessStreamReference` from an `Uri` (`RandomAccessStreamReference.CreateFromUri`), but note that on WASM downloading a file from a random server usually causes some issues with [CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS). 
Make sure to configure the server that hosts the file accordingly.


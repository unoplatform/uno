# Windows.Storage.Pickers

![Android JumpList sample](../Assets/features/filepickers/fileopenpicker.png)

File pickers allow the user to pick a folder or a file on the local file system so that the application can work with it. The following table shows which file picker experiences are available across Uno Platform targets. For detailed information see the next sections.

Legend
  - ✅  Supported
  - ⏸️ Partially supported (see below for more details)
  - 🚫 Not supported
  
| Picker         | UWP | WebAssembly | Android | iOS | macOS | WPF | GTK |
|----------------|-----|-------------|---------|-----|-------|-----|-----|
| FileOpenPicker | ✅   | ✅      (1)     | ✅       | ✅   | ✅     | ✅   | 🚫  |
| FileSavePicker | ✅   | ✅  (1)         | ✅       | ✅   | ✅     | ✅   | 🚫  |
| FolderPicker   | ✅   | ✅           | ✅       | ⏸️ (2)|✅     | 🚫  | 🚫  |

(1) - Multiple implementations supported - see WebAssembly section below
(2) - See iOS section below

## Examples

### FolderPicker

``` c#
var folderPicker = new FolderPicker();
folderPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
folderPicker.FileTypeFilter.Add("*");
StorageFolder pickedFolder = await folderPicker.PickSingleFolderAsync();
if (pickedFolder != null)
{
    // Folder was picked you can now use it
    var files = await pickedFolder.GetFilesAsync();
}
else
{
    // No folder was picked or the dialog was cancelled.
}
```

**Notes**: While the `SuggestedStartLocation` has currently no effect in Uno Platform targets, and `FileTypeFilter` has no effect for `FolderPicker`, they both must be set, otherwise the dialog crashes on UWP.

### FileOpenPicker

#### Picking a single file

``` c#
var fileOpenPicker = new FileOpenPicker();
fileOpenPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
fileOpenPicker.FileTypeFilter.Add(".txt");
fileOpenPicker.FileTypeFilter.Add(".csv");
StorageFile pickedFile = await fileOpenPicker.PickSingleFileAsync();
if (pickedFile != null)
{
    // File was picked, you can now use it
    var text = await FileIO.ReadTextAsync(pickedFile);
}
else
{
    // No file was picked or the dialog was cancelled.
}
```

**Notes**: While the `SuggestedStartLocation` has currently no effect in Uno Platform targets, it must be set, otherwise the dialog crashes on UWP. `FileTypes` must include at least one item. You can add extensions in the format `.extension`, with the exception of `*` (asterisk) which allows picking any type of file.

#### Picking multiple files

``` c#
var fileOpenPicker = new FileOpenPicker();
fileOpenPicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
fileOpenPicker.FileTypeFilter.Add(".jpg");
fileOpenPicker.FileTypeFilter.Add(".png");
var pickedFiles = await fileOpenPicker.PickMultipleFilesAsync();
if (pickedFiles.Count > 0)
{
    // At least one file was picked, you can use them
    foreach (var file in pickedFiles)
    {
        global::System.Diagnostics.Debug(file.Name);   
    }
}
else
{
    // No file was picked or the dialog was cancelled.
}
```

**Notes**: While the `SuggestedStartLocation` has currently no effect in Uno Platform targets, it must be set, otherwise the dialog crashes on UWP. `FileTypes` must include at least one item. You can add extensions in the format `.extension`, with the exception of `*` (asterisk) which allows picking any type of file.

### FileSavePicker

``` c#
var fileSavePicker = new FileSavePicker();
fileSavePicker.SuggestedStartLocation = PickerLocationId.ComputerFolder;
fileSavePicker.SuggestedFileName = "myfile.txt";
fileSavePicker.FileTypeChoices.Add("Plain Text", new List<string>() { ".txt", ".text" });
StorageFile saveFile = await fileSavePicker.PickSaveFileAsync();
if (saveFile != null)
{
    // Save file was picked, you can now write in it
    await FileIO.WriteTextAsync(saveFile, "Hello, world!");
}
else
{
    // No file was picked or the dialog was cancelled.
}
```

**Notes**: While the `SuggestedStartLocation` has no effect, it must be set for UWP. You must declare at least one item for `FileTypeChoices`. Each has a description and one or more extensions.

## Picker configuration

File pickers have various configuration options that customize the experience (see the <a href="https://docs.microsoft.com/en-us/uwp/api/windows.storage.pickers.fileopenpicker" target="_blank">UWP documentation</a> for full list of properties). Not all options are supported on all target platforms, in which case these are ignored.

To set which file type extensions you want to allow, use the `FileTypeFilter` property on `FileOpenPicker` and `FolderPicker`, and the `FileTypeChoices` property on `FileSavePicker`. Extensions must be in the format ".xyz" (starting with a dot). For `FileOpenPicker` and `FolderPicker` you can also include "*" (star) entry, which represents the fact that any file extension is allowed.

Some systems use `MIME` types to specify the file type. Uno includes a list of common predefined mappings (see list in <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/MIME_types/Common_types" target="_blank">MDN Docs</a>). If a MIME type you require is missing, you can provide it by adding it to the `Uno.WinRTFeatureConfiguration.FileTypes.FileTypeToMimeMapping` dictionary:

``` c#
Uno.WinRTFeatureConfiguration.FileTypes.FileTypeToMimeMapping.Add(".myextension", "some/mimetype");
```

For iOS and macOS, `UTType` is utilized for the same purpose. Here you can provide a custom mapping using `Uno.WinRTFeatureConfiguration.FileTypes.FileTypeToUTTypeMapping` dictionary:

``` c#
Uno.WinRTFeatureConfiguration.FileTypes.FileTypeToUTTypeMapping.Add(".myextension", "my.custom.UTType");
```

Custom Uniform Type Identifiers must be declared in the `info.plist` of your iOS and macOS application. See a full example of this [in Apple documentation](https://developer.apple.com/library/archive/documentation/FileManagement/Conceptual/understanding_utis/understand_utis_declare/understand_utis_declare.html).

## WebAssembly

There are two implementations of file pickers available in WebAssembly - **File System Access API pickers** and **download/upload pickers**.
  
### File System Access API pickers

The most powerful picker implementation on WebAssembly uses the <a href="https://wicg.github.io/file-system-access/" target="_blank">**File System Access API**</a>. This is not yet widely implemented across all browsers. See the following support tables for each picker:

- <a href="https://caniuse.com/?search=showDirectoryPicker" target="_blank">`FolderPicker`</a>
- <a href="https://caniuse.com/?search=showOpenFilePicker" target="_blank">`FileOpenPicker`</a>
- <a href="https://caniuse.com/?search=showSaveFilePicker" target="_blank">`FileSavePicker`</a>

`FolderPicker` is only supported for this type of pickers.

File System Access API pickers allow direct access to the picked files and folders. This means that any modifications the user does to the files are persisted on the target file system. 

However, writing to the target file system is limited, so when a write-stream is opened for a file, Uno Platform creates a copy of the file in temporary storage and your changes are applied to this temporary file instead. When your file stream is then flushed, closed, or disposed of, the changes are written to the source file and the temporary file is discarded.

### Download/upload pickers

In case the **File System Access API** is not available in the browser, Uno Platform also offers a fallback to "download" and "upload" experiences.

For the upload picker, the browser triggers a file picker dialog and Uno Platform then copies the selected files into temporary storage of the app. The `StorageFile` instance you receive is private for your application and the changes are not reflected in the original file. To save the changes, you need to trigger the "download picker".

For the download picker, the experience requires the use of [`CachedFileManager`](https://docs.microsoft.com/en-us/uwp/api/Windows.Storage.CachedFileManager):

```c#
var savePicker = new FileSavePicker():
savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
savePicker.FileTypeChoices.Add("Text file", new List<string>() { ".txt" });
savePicker.SuggestedFileName = "New Document";
// for download picker, no dialog is actually triggered here
// and a temporary file is returned immediately.
var file = await savePicker.PickSaveFileAsync();
CachedFileManager.DeferUpdates(file);
// write the file
await FileIO.WriteTextAsync(file, "Hello, world!");
// this starts the download process of the browser
await CachedFileManager.CompleteUpdatesAsync(file);
```

### Checking the source of opened file

To know how the file needs to be handled, you need to check the type of pickers it comes from. To do this, access the `Provider` property of the file:

```c#
if (file.Provider.Id == "jsfileaccessapi")
{
    // File was picked using File System Access pickers.
}

if (file.Provider.Id == "computer")
{
    // File is a temporary file created using Upload picker.
}
```

The local files have provider ID of `computer`, which matches the UWP behavior. `jsfileaccessapi` is used for files coming from the File System Access API.

### Choosing supported type of pickers

By default, Uno Platform attempts to use File System Access API and falls back to download/upload pickers if not available. To control this behavior, you can use `WinRTFeatureConfiguration`:

```c#
#if __WASM__
Uno.WinRTFeatureConfiguration.Storage.Pickers.WasmConfiguration = 
    WasmPickerConfiguration.FileSystemAccessApiWithFallback;
#endif
```

The allowed values for the configuration are:

- `FileSystemAccessApiWithFallback` - defaults to File System Access API, but falls back to download/upload pickers if not available
- `FileSystemAccessApi` - uses File System Access API only. If not avaialable, pickers will throw `NotSupportedException`
- `DownloadUpload` - uses download/upload pickers only.

## Android

Files picked from file pickers on Android are provided by the *Storage Access Framework API*. Due to its limitations, it is not possible to write to existing file in-place. Instead, Uno Platform creates a copy of the file in temporary storage and your changes are applied to this temporary file instead. When your file stream is then flushed, closed, or disposed of, the changes are written to the source file and the temporary file is discarded.

## iOS

iOS does not offer a built-in `FileSavePicker` experience. Luckily it is possible to implement this functionality for example using a combination of a `FolderPicker` and `ContentDialog`.

To provide your own custom implementation, create a class that implements the `IFileSavePickerExtension` which is only available on iOS. This class must have a `public` constructor with a `object` parameter. This will actually be an instance of `FileSavePicker` when invoked later. Then implement the `PickSaveFileAsync` method:

```c#
#if __IOS__
namespace Custom.Pickers
{
    public class CustomFileSavePickerExtension : IFileSavePickerExtension
    {
        private readonly FileSavePicker _fileSavePicker;
    
        public CustomFileSavePickerExtension(object owner)
        {
            _fileSavePicker = (FileSavePicker)picker;
        }
        
        public async Task<StorageFile> PickSaveFileAsync(CancellationToken token)
        {
            // ... your own implementation
        }
    }
}
#endif
```

When done, register this extension in `App.xaml.cs`:

```c#
public App()
{
#if __IOS__
    ApiExtensibility.Register(
        typeof(Uno.Ehttps://twitter.com/thebookisclosed/status/1375006215189753860?s=19xtensions.Storage.Pickers.IFileSavePickerExtension), 
        picker => new CustomFileSavePickerExtension(picker));
#endif
}
```

As this is quite complex, you can find a working implementation of a folder-based save file picker in [Uno.Samples repository](https://github.com/unoplatform/Uno.Samples/tree/master/UI/FileSavePickeriOS). You can modify and adjust this implementation as you see fit for your specific use case.

---
uid: Uno.Features.ApplicationData
---

# Application Data and Settings

<img src="../Assets/features/applicationdata/appdata.jpg" alt="Application Data and Preferences" style="width: 400px;" />

To store persistent application data and user settings, you can utilize the `Windows.Storage.ApplicationData` class in Uno Platform.

Legend

- ✔  Supported

| Picker             | WinUI      | WebAssembly | Android | iOS/Mac Catalyst   | macOS | Skia Desktop |
|--------------------|------------|-------------|---------|--------------------|-------|--------------|
| `LocalFolder`      | ✔         | ✔          | ✔       | ✔                 | ✔     | ✔            |
| `RoamingFolder`    | ✔         | ✔          | ✔       | ✔                 | ✔     | ✔            |
| `LocalCacheFolder` | ✔         | ✔          | ✔       | ✔                 | ✔     | ✔            |
| `TemporaryFolder`  | ✔         | ✔          | ✔       | ✔                 | ✔     | ✔            |
| `LocalSettings`    | ✔         | ✔          | ✔       | ✔                 | ✔     | ✔            |
| `RoamingSettings`  | ✔         | ✔          | ✔       | ✔                 | ✔     | ✔            |

Please note that `RoamingFolder` and `RoamingSettings` are not roamed automatically across devices, they only provide a logical separation between data that you intend to roam and that you intend to keep local.

## Storing application data

There are several folders where persistent application data can be stored:

- `LocalFolder/RoamingFolder` - general-use application files
- `TemporaryFolder` - files with limited lifetime
- `LocalCacheFolder` - cached files retrieved from external services

In the case of `TemporaryFolder` and `LocalCacheFolder` it is crucial to remember that the user or operating system may purge files stored in these locations to reclaim storage space. To store persistent files prefer the `LocalFolder` or `RoamingFolder`.

The following example shows how you can create a file in `LocalFolder` and then read the contents back:

```csharp
StorageFolder folder = ApplicationData.Current.LocalFolder;

// Create a file in the root of LocalFolder.
StorageFile file = await folder.CreateFileAsync("file.txt", CreationCollisionOption.ReplaceExisting);

// Write text in the newly created file.
await FileIO.WriteTextAsync(file, "Hello, Uno Platform!");

// Read the text from file.
string text = await FileIO.ReadTextAsync(file);
```

## Storing settings

The `LocalSettings` and `RoamingSettings` properties provide access to simple key-value containers that allow storage of lightweight user and application preferences. The values stored in settings should be simple serializable types. To store more complex data structures, it is preferred to serialize them first into a string (for example using a JSON serializer).

```csharp
ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

// Save a setting.
localSettings.Values["name"] = "Martin";

// Read a setting.
string value = (string)localSettings.Values["name"];
```

## Initialization considerations

All the `StorageFile`/`StorageFolder` APIs need to be used inside the instance constructor of the `App.xaml.cs`.

Any earlier use, for example in a static constructor or in `Program.cs`, will lead to a fatal failure.

## Data location on Skia Desktop

In the case of Skia Desktop targets, the data are stored in application- and user-specific locations on the hard drive. The default path to the various folders depends on the runtime operating system.

### Windows

- `LocalFolder` - `C:\Users\UserName>\AppData\Local\<Publisher>\<ApplicationName>\LocalState`
- `RoamingFolder` - `C:\Users\<UserName>\AppData\Local\<Publisher>\<ApplicationName>\RoamingState`
- `LocalCaheFolder` - `C:\Users\<UserName>\AppData\Local\<Publisher>\<ApplicationName>\LocalCache`
- `TemporaryFolder` - `C:\Users\<UserName>\AppData\Local\Temp\<Publisher>\<ApplicationName>\TempState`
- `LocalSettings` - `C:\Users\<UserName>\AppData\Local\<Publisher>\<ApplicationName>\Settings\Local.dat`
- `RoamingSettings` - `C:\Users\<UserName>\AppData\Local\<Publisher>\<ApplicationName>\Settings\Roaming.dat`

### Unix-based systems

- `LocalFolder` - `/home/<UserName>/.local/share/<Publisher>/<ApplicationName>/LocalState`
- `RoamingFolder` - `/home/<UserName>/.local/share/<Publisher>/<ApplicationName>/RoamingState`
- `TemporaryFolder` - `/tmp/<Publisher>/<ApplicationName>/TempState`
- `LocalCache` - `/home/<UserName>/.cache/<Publisher>/<ApplicationName>/LocalCache`
- `LocalSettings` - `/home/<UserName>/.local/share/<Publisher>/<ApplicationName>/Settings/Local.dat`
- `RoamingSettings` - `/home/<UserName>/.local/share/<Publisher>/<ApplicationName>/Settings/Roaming.dat`

Where `<UserName>` is the name of the currently logged-in user and `<Publisher>` and `<ApplicationName>` are values coming from the `<Identity>` node of the `Package.appxmanifest` (note that the publisher value is prefixed by `CN=` in the manifest, but this is excluded from the folder name).

The default paths above can be overridden using the following feature flags:

- `WinRTFeatureConfiguration.ApplicationData.TemporaryFolderPathOverride` - affects `TemporaryFolder` location
- `WinRTFeatureConfiguration.ApplicationData.LocalCacheFolderPathOverride` - affects `LocalCacheFolder` location
- `WinRTFeatureConfiguration.ApplicationData.ApplicationDataPathOverride` - affects `LocalFolder`, `RoamingFolder`, `LocalCaheFolder`, `LocalSettings` and `RoamingSettings`

These properties need to be set before the application is initialized. The best place for this is `Program.cs`, before the `UnoPlatformHostBuilder` instance is created.

If you intend to support both Windows and Unix-based systems for the Desktop target, make the path conditional utilizing `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)`.

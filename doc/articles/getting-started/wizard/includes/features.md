#### Sample content

Adds sample pages that show basic usage patterns. With Uno.Extensions navigation, as in the recommended preset, the main page gets a text box and a button that navigates to a second page, which displays the entered text. This is not included in either the blank or recommended presets.

```dotnetcli
dotnet new unoapp -preset recommended --sample
```

#### Toolkit

Installs the [Uno.Toolkit](https://github.com/unoplatform/uno.toolkit.ui) package in the project, this package adds a set of custom controls, behaviors, extensions and other utilities to Uno Platform projects that are not offered out-of-the-box by WinUI.  

This includes [`Card`](https://github.com/unoplatform/uno.toolkit.ui/blob/main/src/Uno.Toolkit.UI/Controls/Card/Card.cs), [`TabBar`](https://github.com/unoplatform/uno.toolkit.ui/blob/main/src/Uno.Toolkit.UI/Controls/TabBar/TabBar.cs), [`NavigationBar`](https://github.com/unoplatform/uno.toolkit.ui/blob/main/src/Uno.Toolkit.UI/Controls/NavigationBar/NavigationBar.cs) and others.

This is included by default in the recommended preset, but not in the blank preset.

```dotnetcli
dotnet new unoapp -toolkit
```

> [!TIP]
> The Uno Toolkit is demonstrated as a live [Uno Toolkit web app](https://gallery.platform.uno/). It is also available as an [iOS](https://apps.apple.com/us/app/uno-gallery/id1380984680) or [Android](https://play.google.com/store/apps/details?id=com.nventive.uno.ui.demo) app.  
> The Gallery app is open-source and is [available on GitHub](https://github.com/unoplatform/uno.gallery).  

#### .NET MAUI Embedding

Adds support for embedding .NET MAUI controls and third party libraries into an application. This is not included in either the blank or recommended presets.

.NET MAUI doesn't support tvOS, so the wizard disables this option when tvOS is selected.

```dotnetcli
dotnet new unoapp -maui
```

#### Server  

Adds an ASP.NET Core Server project to the solution, which hosts the WASM project, and can also be used to create an API and endpoints. It can also be used as the data server and you can also choose to implement the authentication server code in it.

This is not included in either the blank or recommended presets.

```dotnetcli
dotnet new unoapp -server
```

You can read the [server project documentation](xref:Uno.Guides.UsingTheServerProject).

#### PWA Manifest

Includes a PWA ([Progressive Web Apps](https://learn.microsoft.com/microsoft-edge/progressive-web-apps-chromium)) manifest that enables easy installation of the WASM web-target as an app in the running device.

This is included by default in both the blank and recommended presets.

> [!NOTE]
> As this is a WASM feature it will be disabled (Wizard), or ignored (dotnet new), if WASM is not selected as one of the output target platforms.

```dotnetcli
dotnet new unoapp -pwa
```

#### Visual Studio Code debugging

Enables Uno Platform debugging in Visual Studio Code. This is included by default in both the blank and recommended presets.

```dotnetcli
dotnet new unoapp -vscode
```

#### Media Player Element

Bundles the native assets that [`MediaPlayerElement`](xref:Uno.Controls.MediaPlayerElement) needs to play audio and video, through the `MediaPlayerElement` [Uno.Sdk feature](xref:Uno.Features.Uno.Sdk). This is not included in either the blank or recommended presets.

```dotnetcli
dotnet new unoapp -mpe
```

#### Renderer

Selects how the UI is drawn on all targets except **WinAppSDK**, including **Desktop (Windows, macOS, Linux)**, **WebAssembly (WASM)**, **Android**, and **iOS**. Starting with Uno Platform 7.0, the native renderer has been removed.

- **Skia** is the default, in both the blank and recommended presets. It provides a **consistent, pixel-perfect rendering pipeline** by drawing the entire UI using the Skia graphics library (via SkiaSharp). Learn more about [Uno's Skia rendering](xref:uno.features.renderer.skia).
- **WebGPU** is experimental. It draws the UI through WebGPU. Selected on its own, the app has no SkiaSharp and no fallback. Learn more about [Uno's WebGPU rendering](xref:uno.features.renderer.webgpu).

Selecting both uses WebGPU first, with Skia as the fallback.

```dotnetcli
dotnet new unoapp -renderer skia
```

```dotnetcli
dotnet new unoapp -renderer skia -renderer webgpu
```

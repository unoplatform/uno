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

```dotnetcli
dotnet new unoapp -maui
```

#### Server  

Adds an ASP.NET Core Server project to the solution, which hosts the WASM project, and can also be used to create an API and endpoints. It can also be used as the data server and you can also choose to implement the authentication server code in it.

This is included by default in the recommended preset, but not in the blank preset.

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

#### WASM Multi-Threading

Enables multi-threading in the WASM project.  
This option is only available if WASM is selected as one of the output target platforms. This is not enabled in either blank or recommended presets.

```dotnetcli
dotnet new unoapp -wasm-multi-threading
```

#### Renderer

##### Skia

Skia is the [default rendering engine](xref:uno.features.renderer.skia) for Uno Platform as of `Uno.Sdk` 6.0 or later, across all targets except **WinAppSDK**, including **Desktop (Windows, macOS, Linux)**, **WebAssembly (WASM)**, **Android**, and **iOS**.
It provides a **consistent, pixel-perfect rendering pipeline** by drawing the entire UI using the Skia graphics library (via SkiaSharp). This is the default renderer in the **Blank** and **Recommended** presets.

Learn more about [Uno's Skia rendering](xref:uno.features.renderer.skia).

```dotnetcli
dotnet new unoapp -renderer skia
```

##### Native

The Native renderer uses the **platform's native UI components** and rendering systems (e.g., **WinUI** for Windows App SDK, **UIKit** on iOS, **Android Views** on Android). This approach offers better integration with platform-specific behaviors, accessibility tools, and a more native look and feel.

```dotnetcli
dotnet new unoapp -renderer native
```

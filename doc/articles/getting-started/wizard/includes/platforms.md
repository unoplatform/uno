This setting lets you choose which platforms the generated app will target.

Uno Platform currently supports targeting the following operating systems:

- Mobile
  - Android
  - iOS
- Web
  - WebAssembly (wasm)
- Desktop
  - Skia Desktop (Linux X11, Linux Framebuffer, Windows, macOS)
  - Windows App SDK
- TV
  - tvOS ([Apple TV](xref:Uno.Features.AppleTv))

> [!NOTE]
> For most platforms, the name of the command line argument is just the platform name in lowercase (e.g., `android`, `ios` or `tvos`). Use `wasm` for WebAssembly, `desktop` for Skia Desktop, and `windows` for Windows App SDK.

By default, when you create a new Uno Platform app, it will target the following platforms: Android, iOS, WebAssembly, and Skia Desktop.

The following command only selects the Windows platform:

```dotnetcli
dotnet new unoapp -platforms windows
```

To select multiple platforms, the `platforms` argument should be supplied multiple times. In the following example, the generated app will target Windows, Android and iOS:

```dotnetcli
dotnet new unoapp -platforms windows -platforms android -platforms ios
```

#### Android Devices

When Android is selected, these options add support for more Android form factors to the Android head. None of them is included in either the blank or recommended presets.

- **Android TV**: configures the Android head for Android TV. A single APK targets both phones and TVs. See [Support for Android TV](xref:Uno.Features.AndroidTv).
- **Android Auto**: adds Android Auto and Android Automotive OS support, through the AndroidX Car App Library. See [Support for Android Auto](xref:Uno.Features.AndroidAuto).
- **Wear OS**: adds Wear OS support to the Android head. A single APK targets both phones and watches. See [Support for Wear OS](xref:Uno.Features.AndroidWear).

On the command line, these options use their full names and take `true`:

```dotnetcli
dotnet new unoapp --includeAndroidTV true --includeAndroidAuto true --includeAndroidWear true -platforms android
```

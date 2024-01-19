This setting lets you choose which platforms the generated app will target.

Uno Platform currently supports targeting the following operating systems:

- Mobile
  - Android
  - iOS
- Web
  - WebAssembly (wasm)
- Desktop
  - Mac (Mac Catalyst)
  - Windows
  - GTK
  - WPF
  - Linux FrameBuffer (linux-fb)

> [!NOTE]
> For most platforms the name of the command line argument is just the platform name (eg windows or ios). However, for WebAssembly, Mac and Linux Framebuffer, use the abbreviation in braces in the above list (ie wasm, maccatalyst or linux-fb).

By default, when you create a new Uno Platform app, it will target the following platforms: Windows, iOS, Android, Mac Catalyst, Gtk and Wasm.

The following command only selects the Windows platform:

```dotnetcli
dotnet new unoapp -platforms windows
```

To select multiple platforms, the `platforms` argument should be supplied multiple times. In the following example, the generated app will target Windows, Android and iOS:

```dotnetcli
dotnet new unoapp -platforms windows -platforms android -platforms ios
```

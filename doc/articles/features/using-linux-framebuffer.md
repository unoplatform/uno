---
uid: Uno.Skia.Linux.Framebuffer
---

# Using the Linux Framebuffer and `libinput`

Uno supports the [Linux Framebuffer](https://www.kernel.org/doc/html/latest/fb/framebuffer.html) and [libinput](https://github.com/wayland-project/libinput) as a target, in the case where your target device does not provide a Window Manager.

There are restrictions for the support for the Framebuffer:
- The `TextBox` control is not supported. If you need text input, you will need to implement an on-screen keyboard manually using the Keyboard and Pointer events that Uno provides (in the `CoreWindow` class).
- The mouse is supported through pointer events, but Uno does not show the pointer for your app. You'll need to display one using the pointer events provided by Uno (also in the `CoreWindow` class).
- It is only supported on Linux where `/dev/fbXX` is available.

## Get started with the Framebuffer
- Follow the [getting started guide](xref:Uno.GetStarted.vscode)

Create a new app using
```
dotnet new unoapp -o MyApp
```

You'll get a set of projects, including one named `MyApp.Skia.Linux.FrameBuffer`.

You can build this app by navigating to the `MyApp.Skia.Linux.FrameBuffer` and type the following:

```
dotnet run
```

The app will start and display on the first available framebuffer device. To change the active framebuffer, set the device name in the `FRAMEBUFFER` environment variable.

`dotnet run` uses the `Debug` configuration, which will show logging information in the current terminal and may overwrite the UI content.

To read the logging information, either:
- Launch the application from a different terminal (through SSH, for instance)
- Launch the app using `dotnet run > logging.txt 2>&1`, then launch `tail -f logging.txt` in another terminal.

Once the application is running, you can exit the application with:
- `Ctrl+C`
- `F12`, a key configuration found in the `Program.cs` file of your project which invokes `Application.Current.Exit()`

## Creating a standalone app
You can create a standalone publication folder using the following:

```
dotnet publish -c Release -r linux-x64 --self-contained true
```

> [!NOTE]
> When using the `Release` configuration, logging is disabled for performance considerations. You can restore logging in the `App.xaml.cs` file.

Documentation on other hardware targets are [available here](https://github.com/dotnet/core/blob/main/release-notes/6.0/supported-os.md).

## DPI Scaling support
Whenever possible, the `FrameBufferHost` will try to detect the actual DPI scale to use when rendering the UI, based on the physical information provided by the FrameBuffer driver. If the value cannot be determined, a scale of `1.0` is used.

The automatic scaling can be overridden in two ways:
- Set a value using `FrameBufferHost.DisplayScale`
- Set a value through the `UNO_DISPLAY_SCALE_OVERRIDE` environment variable. This value has precedence over the value specified in `FrameBufferHost.DisplayScale`

## Additional dependencies

If your device is significantly trimmed down for installed packages, you'll need to install :
- `libfontconfig`
- `libfreetype`
- `libinput`

---
uid: Uno.Skia.Linux.Framebuffer
---

# Using the Linux FrameBuffer and `libinput`

Uno Platform supports the [Linux FrameBuffer](https://www.kernel.org/doc/html/latest/fb/framebuffer.html) and [libinput](https://wayland.freedesktop.org/libinput/doc/latest/what-is-libinput.html) as a target, in the case where your target device does not provide a Window Manager.

There are some restrictions for the support for the FrameBuffer:

- The mouse is supported through pointer events, but Uno Platform does not show the pointer for your app. You'll need to display one using the pointer events provided by Uno Platform (also in the `CoreWindow` class).
- It is only supported on Linux where `/dev/fbXX` is available.

## Get started with the FrameBuffer

- Follow the [getting started guide](xref:Uno.GetStarted.vscode)

Create a new app using:

```dotnetcli
dotnet new unoapp -o MyApp
```

In the `Platforms/Desktop/Program.cs` file, the `UnoPlatformHostBuilder` can be configured to use the Framebuffer support, in case X11 support is detected first:

```csharp
var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .UseX11()
    .UseLinuxFrameBuffer()
    .UseMacOS()
    .UseWindows()
    .Build();
```

Each platform support is evaluated in order for availability and definition in builder. X11 is chosen when the `DISPLAY` variable is present.

## Running the app

You can build and run this app by navigating to the `MyApp` and type the following:

```dotnetcli
dotnet run -f net9.0-desktop
```

The app will start and display on the first available framebuffer device. To change the active framebuffer, set the device name in the `FRAMEBUFFER` environment variable.

By default, the `Debug` configuration is used, which will show logging information in the current terminal and may overwrite the UI content.

To read the logging information, either:

- Launch the application from a different terminal (through SSH, for instance)
- Launch the app using `dotnet run -f net9.0-desktop > logging.txt 2>&1`, then launch `tail -f logging.txt` in another terminal.

Once the application is running, you can exit the application with:

- `Ctrl+C`
- `F12`, a key configuration found in the `Program.cs` file of your project which invokes `Application.Current.Exit()`

## Creating a standalone app

You can create a standalone publication folder using the following:

```dotnetcli
dotnet publish -c Release -f net9.0-desktop -r linux-x64 --self-contained true
```

> [!NOTE]
> When using the `Release` configuration, logging is disabled for performance considerations. You can restore logging in the `App.xaml.cs` file.

Documentation on other hardware targets is [available here](https://github.com/dotnet/core/blob/main/release-notes/8.0/supported-os.md).

## DPI Scaling support

Whenever possible, the `FrameBufferHost` will try to detect the actual DPI scale to use when rendering the UI, based on the physical information provided by the FrameBuffer driver. If the value cannot be determined, a scale of `1.0` is used.

The automatic scaling can be overridden in two ways:

- Set a value using `FrameBufferHost.DisplayScale`.
- Set a value through the `UNO_DISPLAY_SCALE_OVERRIDE` environment variable. This value has precedence over the value specified in `FrameBufferHost.DisplayScale`.

## Additional dependencies

If your device is significantly trimmed down for installed packages, you'll need to install:

- `libfontconfig`
- `libfreetype`
- `libinput`

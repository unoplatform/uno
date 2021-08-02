# Using the Linux Framebuffer and libinput

Uno supports the [Linux Framebuffer](https://www.kernel.org/doc/html/latest/fb/framebuffer.html) and [libinput](https://github.com/wayland-project/libinput) as a target, in the case where your target device does not provide a Window Manager.

There are restrictions for the support for the Framebuffer:
- The `TextBox` control is not supported. If you need text input, you will need to implement an on-screen keyboard manually using the Keyboard and Pointer events that Uno provides (in the `CoreWindow` class).
- The mouse is supported through pointer events, but Uno does not show the pointer for your app. You'll need to display one using the pointer events provided by Uno (also in the `CoreWindow` class).
- It is only supported on Linux where `/dev/fbXX` is available.

## Get started with the Framebuffer
- Follow the [getting started guide for linux](../get-started-with-linux.md)
- Install the [`dotnet new` templates](../get-started-dotnet-new.md)

Create a new app using 
```
dotnet new unoapp -o MyApp
```

You'll get a set of projects, including one named `MyApp.Skia.Linux.FrameBuffer`.

You can build this app by navigating to the `MyApp..Skia.Linux.FrameBuffer` and type the following:

```
dotnet run
```

You can create a standalone publication folder using the following:

```
dotnet publish -c Release -r linux-x64 --self-contained true
```

The app will start and display on the first available framebuffer device.

Documentation on other hardware targets are [available here](https://github.com/dotnet/core/blob/main/release-notes/5.0/5.0-supported-os.md).

## Additional dependencies

If your device is significantly trimmed down for installed packages, you'll need to install :
- `libfontconfig`
- `libfreetype`
- `libinput`

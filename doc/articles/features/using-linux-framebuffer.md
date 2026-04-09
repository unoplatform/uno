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
dotnet run -f net10.0-desktop
```

The app will start and display on the first available framebuffer device. To change the active framebuffer, set the device name in the `FRAMEBUFFER` environment variable.

By default, the `Debug` configuration is used, which will show logging information in the current terminal and may overwrite the UI content.

To read the logging information, either:

- Launch the application from a different terminal (through SSH, for instance)
- Launch the app using `dotnet run -f net10.0-desktop > logging.txt 2>&1`, then launch `tail -f logging.txt` in another terminal.

Once the application is running, you can exit the application with:

- `Ctrl+C`
- `F12`, a key configuration found in the `Program.cs` file of your project which invokes `Application.Current.Exit()`

## Creating a standalone app

You can create a standalone publication folder using the following:

```dotnetcli
dotnet publish -c Release -f net10.0-desktop -r linux-x64 --self-contained true
```

> [!NOTE]
> When using the `Release` configuration, logging is disabled for performance considerations. You can restore logging in the `App.xaml.cs` file.

Documentation on other hardware targets is [available here](https://github.com/dotnet/core/blob/main/release-notes/8.0/supported-os.md).

## Configuration Options

The `UseLinuxFrameBuffer` method accepts an optional configuration callback that provides access to the `FramebufferHostBuilder`. This builder exposes options for rendering, input, display orientation, and keyboard layout.

```csharp
var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .UseLinuxFrameBuffer(fb => fb
        .EnableMouseCursor(5, Color.FromArgb(255, 255, 255, 255))
        .Orientation(DisplayOrientations.Portrait)
        .UseKMSDRM()
        .XkbKeymap(new FramebufferHostBuilder.XKBKeymapParams(layout: "us"))
        .ReverseMouseWheel()
    )
    .Build();
```

### Mouse Cursor

By default, the mouse cursor is shown only after the first mouse event is received from libinput. If only touch events are received (e.g. on a touchscreen), no cursor is displayed.

You can override this behavior explicitly:

- **`EnableMouseCursor(float radius, Color color)`** — Always show the cursor, rendered as a small filled circle with the specified radius (in pixels) and color.
- **`DisableMouseCursor()`** — Never show the cursor, even when mouse events are received.

```csharp
// Show a white cursor circle with radius 5
fb.EnableMouseCursor(5, Color.FromArgb(255, 255, 255, 255));

// Or hide the cursor entirely
fb.DisableMouseCursor();
```

### Mouse Wheel Direction

By default, the framebuffer host inverts the raw libinput scroll values so that scrolling matches the conventional direction used by desktop environments.

- **`ReverseMouseWheel(bool reverse = true)`** — Disables the default inversion and uses the raw libinput scroll values directly. This is useful on devices where the natural scrolling direction from libinput already matches the expected behavior.

```csharp
fb.ReverseMouseWheel();
```

### Display Orientation

- **`Orientation(DisplayOrientations orientation)`** — Sets the display orientation. The default is `DisplayOrientations.Landscape`. This rotates how pointer coordinates and rendering are mapped to the framebuffer.

Available values: `Landscape`, `Portrait`, `LandscapeFlipped`, `PortraitFlipped`.

```csharp
fb.Orientation(DisplayOrientations.Portrait);
```

### Hardware-Accelerated Rendering (KMS/DRM)

By default, the framebuffer host attempts to create an OpenGL ES context via KMS/DRM for hardware-accelerated rendering. If that fails, it falls back to software rendering automatically.

- **`UseKMSDRM(string? cardPath, DRMFourCCColorFormat? gbmSurfaceColorFormat, DRMConnectorChooserDelegate? connectorChooser)`** — Explicitly enables hardware-accelerated rendering via KMS/DRM.
  - `cardPath` — Path to the DRM device file (e.g. `/dev/dri/card0`). If `null`, the first available `/dev/dri/cardX` device is used.
  - `gbmSurfaceColorFormat` — The FourCC color format for the GBM surface. Defaults to ARGB8888. See the [DRM FourCC header](https://github.com/torvalds/linux/blob/master/include/uapi/drm/drm_fourcc.h) for valid values.
  - `connectorChooser` — A delegate that receives the list of available DRM connectors and returns the index of the one to use, or -1. If not provided, the first connector is used.

- **`DisableKMSDRM()`** — Forces software rendering, skipping any attempt to use KMS/DRM.

```csharp
// Explicitly enable DRM with a specific card
fb.UseKMSDRM(cardPath: "/dev/dri/card1");

// Or force software rendering
fb.DisableKMSDRM();

// Choose a specific connector
fb.UseKMSDRM(connectorChooser: connectors =>
{
    for (int i = 0; i < connectors.Count; i++)
    {
        if (connectors[i].connectorStringRepresentation.Contains("HDMI"))
            return i;
    }
    return 0;
});
```

### Keyboard Layout (XKB Keymap)

- **`XkbKeymap(XKBKeymapParams keymapParams)`** — Configures the RMLVO parameters passed to `libxkbcommon` for keyboard keymap creation. If not called, the system default keymap is used.

The `XKBKeymapParams` record accepts the following optional parameters: `rules`, `model`, `layout`, `variant`, and `options`. For details on RMLVO, see the [libxkbcommon documentation](https://xkbcommon.org/doc/current/xkb-intro.html#RMLVO-intro).

```csharp
// Use a French AZERTY layout
fb.XkbKeymap(new FramebufferHostBuilder.XKBKeymapParams(layout: "fr"));

// Use a US layout with the Dvorak variant
fb.XkbKeymap(new FramebufferHostBuilder.XKBKeymapParams(layout: "us", variant: "dvorak"));
```

## DPI Scaling Support

Whenever possible, the `FrameBufferHost` will try to detect the actual DPI scale to use when rendering the UI, based on the physical information provided by the FrameBuffer driver. If the value cannot be determined, a scale of `1.0` is used.

The automatic scaling can be overridden in two ways:

- Set a value using `FrameBufferHost.DisplayScale`.
- Set a value through the `UNO_DISPLAY_SCALE_OVERRIDE` environment variable. This value has precedence over the value specified in `FrameBufferHost.DisplayScale`.

## Additional Dependencies

If your device is significantly trimmed down for installed packages, you'll need to install:

- `libfontconfig`
- `libfreetype`
- `libinput`

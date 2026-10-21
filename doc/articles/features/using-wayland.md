---
uid: Uno.Skia.Linux.Wayland
---

# Using the Wayland host

Uno Platform can run Skia Desktop apps as native [Wayland](https://wayland.freedesktop.org/) clients, without going through XWayland. A native Wayland window gets the scale the compositor picks for it, including fractional scales such as 125% or 150%, so text and images stay sharp on HiDPI displays where an XWayland window would be rendered at 100% and scaled up.

The Wayland host is opt-in. An app that doesn't register it keeps running through X11, including under XWayland.

## How to use the Wayland host

In the `Platforms/Desktop/Program.cs` file, add `UseWayland()` to the `UnoPlatformHostBuilder`, **before** `UseX11()`:

```csharp
var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .UseWayland()
    .UseX11()
    .UseLinuxFrameBuffer()
    .UseMacOS()
    .UseWin32()
    .Build();

host.Run();
```

The builder evaluates the hosts in the order they were registered. The Wayland host is selected when all of the following are true:

- The app runs on Linux and the `WAYLAND_DISPLAY` environment variable is set.
- The host can connect to the compositor.
- The compositor advertises `wl_compositor`, `wl_shm` and `xdg_wm_base`.

Otherwise, the next registered host (typically X11) is used. To run an app through XWayland in a Wayland session, unset `WAYLAND_DISPLAY` for that app, or don't register `UseWayland()`.

The app id of the window (used by the compositor to match the app's `.desktop` file) is the package name, as `WM_CLASS` is on X11.

### Rendering backend

The host renders with OpenGL ES through EGL when it can create a context for the window, and falls back to software rendering into shared-memory buffers otherwise. You can force one backend, or disable some, from the host builder:

```csharp
    .UseWayland(b => b.ForceRenderingBackend(WaylandRenderingBackend.Software))
```

For more information, see [Rendering Backend Selection](xref:Uno.Skia.Desktop#rendering-backend-selection).

### Native dependencies

The host loads the following libraries at runtime: `libwayland-client.so.0`, `libxkbcommon.so.0`, and, for OpenGL ES rendering, `libwayland-egl.so.1` and `libEGL.so.1`. On compositors without the cursor-shape protocol, cursors are loaded from the XCursor theme through `libwayland-cursor.so.0`; without it, the cursor does not change. Install these dependencies using your distribution's package manager. An EGL implementation (such as Mesa) is needed for accelerated rendering.

To use the system HarfBuzz library for text shaping, configure `UseSystemHarfBuzz(true)`:

```csharp
    .UseWayland(b => b.UseSystemHarfBuzz(true))
```

This option requires `libharfbuzz.so.0` to be installed and available to the dynamic linker. With the default `UseSystemHarfBuzz(false)`, the app uses the HarfBuzzSharp native library supplied by its desktop head.

### Supported protocols

| Protocol | Required | Used for |
|----------|----------|----------|
| `wl_compositor`, `wl_shm`, `xdg_wm_base` | Yes | Windows and software rendering |
| `wl_seat` | No | Pointer, keyboard and touch input |
| `wl_output` | No | Output information, integer scaling and changes as windows move between outputs |
| `wp_viewporter`, `wp_fractional_scale_v1` | No | Fractional scaling. Without both protocols, surfaces at version 6 or later use the integer scale from `wl_surface.preferred_buffer_scale`; older surfaces use the largest `wl_output.scale` among the outputs entered by the window. |
| `zxdg_decoration_manager_v1` | No | Negotiating server-side decorations; client-side decorations are used otherwise |
| `wl_subcompositor` | No | Client-side decoration surfaces |
| `wl_data_device_manager` | No | Clipboard and drag and drop |
| `zwp_text_input_manager_v3` | No | Compositor IME sessions |
| `zxdg_exporter_v2` | No | Exporting a parent window for desktop portals |
| `xdg_activation_v1` | No | Compositor-mediated activation |
| `xdg_toplevel_icon_manager_v1` | No | Window icons |
| `wp_cursor_shape_manager_v1` | No | Pointer cursors drawn by the compositor. Without it, cursors come from the XCursor theme. |

### Window management and title bars

The host supports multiple windows, resizing, minimize/maximize/restore, full-screen presenters and window titles. The compositor controls placement and activation: apps cannot choose a global screen position or assume that an activation request will receive focus.

When the compositor grants server-side decorations, it draws the window frame. Otherwise, Uno Platform draws client-side decorations with move, resize and caption buttons. `Window.ExtendsContentIntoTitleBar`, `Window.SetTitleBar` and `InputNonClientPointerSource` caption and passthrough regions support custom title bars. Non-client region rectangles use physical pixels; update them when layout or `XamlRoot.RasterizationScale` changes.

### Clipboard, drag and drop, and text input

Clipboard and incoming drag and drop support text, HTML, images and local file lists through the core data-device protocol. Copy/move negotiation follows the operations supported by both applications. Wayland ties clipboard ownership to a focused input serial, so set clipboard content in response to user input in the focused window. A background client cannot take the clipboard arbitrarily: content set while the window has no keyboard focus is not published, and a warning is logged.

Drags started in the app are handed to the compositor as soon as they start, so they can be dropped on other applications, including windows that overlap the app's own window. The dragged content is offered as local file lists (also readable as text), images, HTML and text, and the drag visual is shown as the compositor's drag icon. The operation completed by the target is reported back as the drop result. Content made only of app-private formats, or a drag without a pointer or touch grab, stays an in-app drag, as does any drag when the compositor's data device is older than version 3.

Keyboard input supports XKB layouts, dead keys and compose sequences. Compositors that advertise `text-input-v3` also provide IME preedit, commit, surrounding text and deletion for `TextBox` and `RichEditBox`. Configure the desktop's input-method service for its Wayland session; without the protocol, keyboard and compose input remain available but compositor IME composition is unavailable.

### Desktop portals and settings

Open, save and folder pickers use the XDG Desktop Portal over the session D-Bus. Install `xdg-desktop-portal` and a backend appropriate to your desktop, and run the app in a session with a working D-Bus connection. When the compositor supports `xdg-foreign-v2`, the picker is parented to the exported Wayland window for the duration of the request. Without it, the portal can show a dialog without a parent window.

System theme and text scale use the desktop settings portal. Window activation and icons use the compositor's optional activation and top-level icon protocols.

### Diagnostics and environment variables

Set these diagnostic switches before starting the app:

| Variable | Value | Effect |
|----------|-------|--------|
| `UNO_WAYLAND_DISABLE_CURSOR_SHAPE` | `1` | Disables the cursor-shape protocol to exercise the XCursor theme fallback. |
| `UNO_WAYLAND_FORCE_CSD` | `1` | Forces client-side decorations to exercise Uno Platform's window frame. |

These are the only `UNO_WAYLAND_*` variables read by the runtime. The SamplesApp desktop head also reads `UNO_WAYLAND_RENDERER` (`OpenGLES` or `Software`) to select a backend and `UNO_WAYLAND_LIFECYCLE_SMOKE=1` to run its window lifecycle smoke check. Other apps select a backend through the host builder.

The runtime also reads the following session settings:

| Variable | Used for |
|----------|----------|
| `WAYLAND_DISPLAY` | Host selection and the Wayland display connection. |
| `XCURSOR_THEME` | Cursor theme when the cursor-shape protocol is unavailable or disabled. |
| `XCURSOR_SIZE` | Cursor size in logical pixels for that theme (default 24). |
| `LC_ALL`, `LC_CTYPE`, `LANG` | Compose locale, using the first non-empty value in this order, or `C` when none is set. |
| `XDG_ACTIVATION_TOKEN` | Startup activation; the token is consumed and cleared after reading. |

Enable Debug logging for the `Uno.UI.Runtime.Wayland.WaylandDiagnostics` category to trace window lifecycle and protocol activity.

### Limitations

- `WebView2` and `MediaPlayerElement` embed X11 windows, so they are not supported by the Wayland host.
- Input uses the first seat advertised by the compositor. Output tracking follows the outputs entered by the window; the compositor determines its scale.
- Wayland does not expose global window coordinates. Window placement and focus remain under compositor control.
- Portal dialogs and IME require the corresponding desktop services; a headless compositor alone does not provide them.

## Supported features by platform

| Feature | Windows | Android | iOS | Web (WASM) | macOS | Linux (Skia) | Win 7 (Skia) |
|---------|---------|---------|-----|------------|-------|--------------|--------------|
| Wayland host | ✖ | ✖ | ✖ | ✖ | ✖ | ✔ | ✖ |

## See the Wayland host in action

The SamplesApp desktop head registers `UseWayland()` before `UseX11()`. Run it from a Wayland session to use the Wayland host. To force a rendering backend, set the `UNO_WAYLAND_RENDERER` environment variable to `OpenGLES` or `Software`.

Search the SamplesApp for `Window_DesktopScenarios` to exercise resizing, presenters, multiple windows and custom title bars; `Clipboard_DesktopTransfers` for clipboard formats, file portals and drop negotiation; and `TextBox_DesktopIme` for Unicode composition and focus transfer between editors. The existing `TextBox_IME_Composition` page displays composition events.

The CI headless Weston run uses software rendering at scale 1. It checks host selection, window lifecycle and runtime tests. Accelerated EGL presentation, physical input, IME services and portal backends require a desktop session for validation.

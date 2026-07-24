---
uid: Uno.Skia.Headless
---

# Using the Headless (offscreen) host

Uno Platform provides a **headless** Skia host that runs an app **without a display**. The full window
lifecycle stays intact and the two-phase render cycle keeps running, but there is no native window, no
input device, and no OS chrome. It is cross-platform (Windows, Linux, macOS) and runs anywhere the
managed Skia stack does.

Typical uses:

- CI / automated UI testing
- Server-side or offscreen rendering

Windows produce no on-screen output. To read pixels, use the standard WinUI `RenderTargetBitmap`
(see [Reading pixels](#reading-pixels)).

## Get started

Reference the `Uno.WinUI.Runtime.Skia.Headless` package from your desktop head, then select the host with
`UseHeadless()` on the `UnoPlatformHostBuilder`:

```csharp
using Uno.UI.Hosting;

var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .UseHeadless()
    .Build();

host.Run();
```

> [!NOTE]
> The headless host reports itself as supported on every operating system, so it is always selected when
> present. Use it on its own (for example in a dedicated test/offscreen head) rather than chained with
> `UseX11()`/`UseWin32()`/`UseMacOS()`, which it would otherwise shadow.

## Running the app

`host.Run()` blocks until the app calls `Application.Current.Exit()`. Closing the app's `Window` does not
end the process (as on WinUI desktop), and there is no native window or OS signal to end it either — so an
explicit `Exit()` is how a headless run terminates.

## Configuration options

`UseHeadless` accepts an optional callback that configures the `HeadlessHostBuilder`:

```csharp
var host = UnoPlatformHostBuilder.Create()
    .App(() => new App())
    .UseHeadless(o => o
        .WithSize(1280, 720)   // initial raw pixel size (default 1024 x 640)
        .WithDpi(144))         // or .WithScale(1.5f); default scale 1.0
    .Build();
```

| Method | Description |
|--------|-------------|
| `WithSize(int width, int height)` | The initial raw pixel size applied to every window (default `1024 x 640`). |
| `WithScale(float scale)` | The rasterization scale, a.k.a. `RawPixelsPerViewPixel` (default `1.0`). |
| `WithDpi(float logicalDpi)` | Sets the scale from a logical DPI value (`scale = dpi / 96`). |
| `ConfigureWindow(Func<HeadlessWindowContext, HeadlessWindowOptions>)` | Per-window configuration, invoked for each window as it is created. |

### Scale vs. size

The scale (`WithScale`/`WithDpi`) has no public WinUI equivalent, so it is set here. The **size** uses the
standard WinUI API instead — see below.

## Window size

Every window starts at the builder default (`WithSize`). To size a window individually, or change it at
runtime, use the standard WinUI `AppWindow.Resize`, which the headless host honors:

```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
{
    var window = new Window();
    window.AppWindow.Resize(new SizeInt32 { Width = 1280, Height = 720 });
    window.Content = new MainPage();
    window.Activate();
}
```

## Multiple windows

Multiple windows are supported. Use `ConfigureWindow` to give each window its own scale, and size each one
with `AppWindow.Resize`:

```csharp
.UseHeadless(o => o.ConfigureWindow(ctx => new HeadlessWindowOptions
{
    Scale = ctx.Index == 0 ? 1f : 2f,
}))
```

`WithScale`/`WithDpi` act as the defaults for windows that `ConfigureWindow` does not specify; `WithSize`
sets the initial size for all of them.

## Reading pixels

The host has no pixel-output hook. Read pixels on demand with the standard WinUI `RenderTargetBitmap`:

```csharp
var rtb = new RenderTargetBitmap();
await rtb.RenderAsync(window.Content);          // or any element/subtree
byte[] pixels = (await rtb.GetPixelsAsync()).ToArray();   // BGRA8, rtb.PixelWidth × rtb.PixelHeight
```

The render cycle keeps ticking so `RenderTargetBitmap` (and composition animations) behave like a real
target, even though the window's own paint walk is skipped.

## Simulating input

The headless host wires up no pointer or keyboard input source (there is no input device). To drive
synthetic input in automated tests, use `InputInjector` — it feeds events straight into the managed input
pipeline (the same hit-testing and `PointerPressed`/`PointerMoved`/… routing real input drives), so it
works without any host input interface:

```csharp
var injector = InputInjector.TryCreate();
injector?.InjectPointerInput(/* … */);
```

## DPI scaling

The scale defaults to `1.0` and can be set with `WithScale`/`WithDpi`. It is reported through
`DisplayInformation` (`RawPixelsPerViewPixel`, `LogicalDpi`) so layout and `RenderTargetBitmap` capture at
the configured scale.

## Additional dependencies

On trimmed-down environments (e.g. minimal CI containers), the managed Skia stack needs the usual native
libraries to render text:

- `libfontconfig`
- `libfreetype`

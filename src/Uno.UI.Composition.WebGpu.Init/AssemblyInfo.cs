using System.Runtime.CompilerServices;

// The renderer takes the device through IWebGpuDeviceContext, which the contexts here implement; the init half never
// references the renderer.

// The hosts call the internal WebGpuContext.Create* helpers to build a WebGpu swapchain context for the WebGpu kind.
[assembly: InternalsVisibleTo("Uno.UI.Runtime.X11")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Win32")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.MacOS")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.WebAssembly.Browser")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Android")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.AppleUIKit")]

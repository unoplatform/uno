using System.Runtime.CompilerServices;

// The WebGPU renderer consumes this init half's internal types (OwnedResources, the swapchain/browser contexts,
// the raw wgpu bindings) to build its device-bound renderer. One-way: the init half never references the renderer.

// The hosts call the internal WebGpuContext.Create* helpers to build a WebGpu swapchain context for the WebGpu kind.
[assembly: InternalsVisibleTo("Uno.UI.Runtime.X11")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Win32")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.MacOS")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.BrowserWasm")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Android")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.AppleUIKit")]

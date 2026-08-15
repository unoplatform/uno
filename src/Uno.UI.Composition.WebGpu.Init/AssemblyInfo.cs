using System.Runtime.CompilerServices;

// The WebGPU renderer consumes this init half's internal types (OwnedResources, the swapchain/browser contexts,
// the raw wgpu bindings) to build its device-bound renderer. One-way: the init half never references the renderer.
[assembly: InternalsVisibleTo("Uno.UI.Composition.WebGpu")]

// The hosts call the internal WebGpuContext.Create* helpers to build a WebGpu swapchain context for the WebGpu kind.
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.X11")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Win32")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.MacOS")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.WebAssembly.Browser")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.Android")]
[assembly: InternalsVisibleTo("Uno.UI.Runtime.Skia.AppleUIKit")]

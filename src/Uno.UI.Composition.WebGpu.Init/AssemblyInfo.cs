using System.Runtime.CompilerServices;

// The WebGPU renderer consumes this init half's internal types (OwnedResources, the swapchain/browser contexts,
// the raw wgpu bindings) to build its device-bound renderer. One-way: the init half never references the renderer.
[assembly: InternalsVisibleTo("Uno.UI.Composition.WebGpu")]

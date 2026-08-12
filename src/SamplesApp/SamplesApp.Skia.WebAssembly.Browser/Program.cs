using Uno.UI.Hosting;
using Uno.UI.Composition.Drawing;

// LOCAL-ONLY (do not commit): force the WebGPU-on-WASM head on so the handed-over test build renders via WebGPU
// (BrowserRenderer opts in on UNO_WEBGPU). Must be set before the host builds the renderer. An existing value wins.
if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("UNO_WEBGPU")))
{
	System.Environment.SetEnvironmentVariable("UNO_WEBGPU", "1");
}

// Composition root: when the WebGPU head is active, register the WebGPU context/window factory (GPU-API half,
// independent of the renderer) + the WebGPU render backend provider. The host references neither.
if (System.Environment.GetEnvironmentVariable("UNO_WEBGPU") is "1" or "true" or "neutral" or "swapchain")
{
	global::Uno.UI.Composition.WebGpu.WebGpuContextFactory.Register();
	GraphicsRegistry.Register(new IGraphicsProvider[] { new global::Uno.UI.Composition.WebGpu.WebGpuGraphicsProvider() });
}

var host = UnoPlatformHostBuilder.Create()
	.App(() => new SamplesApp.App())
	.UseWebAssembly()
	.Build();

await host.RunAsync();

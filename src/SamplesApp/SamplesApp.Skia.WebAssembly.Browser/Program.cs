using Uno.UI.Hosting;
using Uno.UI.Composition.Drawing;

var builder = UnoPlatformHostBuilder.Create()
	.App(() => new SamplesApp.App())
	.UseWebAssembly();

// Opt into the WebGPU render backend (over the managed, SkiaSharp-free geometry engine) when requested. The host
// selects the render path from what's registered here — a declared backend drives the neutral pipeline; otherwise
// the head uses the default Skia WebGL/software renderer. Requires publishing with -p:UnoWebGpuWasm=true so that
// Dawn/emdawnwebgpu is linked.
if (System.Environment.GetEnvironmentVariable("UNO_WEBGPU") is "1" or "true" or "neutral" or "swapchain")
{
	builder.GraphicsBackend(new global::Uno.UI.Composition.WebGpu.WebGpuGraphicsProvider());
	builder.GeometryFactory(new ManagedGeometryFactory());
}

await builder.Build().RunAsync();

using Uno.UI.Hosting;

var builder = UnoPlatformHostBuilder.Create()
	.App(() => new SamplesApp.App())
	.UseWebAssembly();

// Register the drawing backend + content seams (Skia by default; WebGPU + managed seams for a SkiaSharp-free build).
// Shared with every SamplesApp head. The WebGPU render path additionally requires publishing with
// -p:UnoWebGpuWasm=true so that Dawn/emdawnwebgpu is linked.
SamplesApp.DrawingBackendConfiguration.Configure(builder);

await builder.Build().RunAsync();

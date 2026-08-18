using Uno.UI.Hosting;

var builder = UnoPlatformHostBuilder.Create()
	.App(() => new SamplesApp.App())
	.UseAppleUIKit();

// Register the drawing backend + content seams (Skia by default; WebGPU + managed seams for a SkiaSharp-free build).
SamplesApp.DrawingBackendConfiguration.Configure(builder);

var host = builder.Build();

host.Run();

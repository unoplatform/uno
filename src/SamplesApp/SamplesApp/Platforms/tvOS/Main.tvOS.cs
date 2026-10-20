using Uno.UI.Hosting;

var builder = UnoPlatformHostBuilder.Create()
	.App(() => new SamplesApp.App())
	.UseAppleUIKit();

// Register the drawing backend + content seams, as every other head does. Without this the builder has
// no renderer, font provider, image decoder or geometry engine and Build() throws.
SamplesApp.DrawingBackendConfiguration.Configure(builder);

var host = builder.Build();

host.Run();

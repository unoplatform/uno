using Uno.UI.Hosting;

var host = UnoPlatformHostBuilder.Create()
	.App(() => new SamplesApp.App())
	.UseWebAssembly()
	.Build();

await host.RunAsync();

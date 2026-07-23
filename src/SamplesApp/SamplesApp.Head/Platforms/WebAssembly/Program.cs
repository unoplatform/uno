using Uno.UI.Hosting;

SamplesApp.App.ConfigureLogging();

var host = UnoPlatformHostBuilder.Create()
	.App(() => new SamplesApp.App())
	.UseWebAssembly()
	.Build();

await host.RunAsync();

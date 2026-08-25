using Uno;
using Uno.UI.Hosting;

WinRTFeatureConfiguration.AppNotifications.UseServiceWorkerOnWebAssembly = true;

var host = UnoPlatformHostBuilder.Create()
	.App(() => new SamplesApp.App())
	.UseWebAssembly()
	.Build();

await host.RunAsync();

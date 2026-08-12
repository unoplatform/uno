using Uno.UI.Hosting;

var host = UnoPlatformHostBuilder.Create()
	.App(() => new SamplesApp.App())
	.UseAppleUIKit()
	.Build();

host.Run();

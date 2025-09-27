using SamplesApp;
using Uno.UI.Hosting;

var host = UnoPlatformHostBuilder.Create()
	.App(() => new App())
	.UseAppleUIKit()
	.Build();

host.Run();

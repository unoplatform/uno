using Uno.UI.Hosting;

var host = UnoPlatformHostBuilder.Create()
	.UseAppleUIKit()
	.Build();

host.Run();

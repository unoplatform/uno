#if __IOS__ && !__MACCATALYST__ && !TESTFLIGHT && !DEBUG
// requires Xamarin Test Cloud Agent
Xamarin.Calabash.Start();
#endif

using SamplesApp;
using Uno.UI.Hosting;

var host = UnoPlatformHostBuilder.Create()
	.App(() => new App())
	.UseAppleUIKit(builder =>
	{
		builder.UseUIApplicationDelegate<App>();
	})
	.Build();

host.Run();

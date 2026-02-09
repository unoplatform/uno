using Uno.UI.Hosting;

var host = UnoPlatformHostBuilder.Create()
	.App(() => new SamplesApp.App())
	.AfterInit(() => global::Uno.Foundation.Extensibility.ApiExtensibility.Register(typeof(Microsoft.UI.Xaml.Documents.ISpellCheckingService), o => new global::Uno.WinUI.SpellChecking.SpellCheckingService(o)))
	.UseWebAssembly()
	.Build();

await host.RunAsync();

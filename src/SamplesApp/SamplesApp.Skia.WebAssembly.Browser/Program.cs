using Uno.UI.Runtime.Skia.WebAssembly.Browser;

var host = new WebAssemblyBrowserHost(() => new SamplesApp.App());
await host.Run();

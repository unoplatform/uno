using System;
using System.Text.RegularExpressions;
using Uno.UI.Runtime.Skia.WebAssembly.Browser;

namespace Uno.UI.Hosting;

internal partial class WebAssemblyHostBuilder : IPlatformHostBuilder
{
	public WebAssemblyHostBuilder()
	{
	}

	public bool IsSupported => true;

	public UnoPlatformHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder)
		=> new WebAssemblyBrowserHost(appBuilder);
}

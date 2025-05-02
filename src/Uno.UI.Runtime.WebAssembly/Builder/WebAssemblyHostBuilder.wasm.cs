#nullable enable

using System;
using Uno.UI.Hosting.WebAssembly;

namespace Uno.UI.Hosting;

internal partial class WebAssemblyHostBuilder : IPlatformHostBuilder
{
	public WebAssemblyHostBuilder()
	{
	}

	public bool IsSupported => true;

	public UnoPlatformHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
		=> new WebAssemblyBrowserHost(appBuilder);
}

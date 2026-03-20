using System;
using System.Text.RegularExpressions;
using Uno.UI.Runtime.Skia.WebAssembly.Browser;

namespace Uno.UI.Hosting;

public class WebAssemblyHostBuilder : IPlatformHostBuilder
{
	private bool _forceSoftwareRendering;

	internal WebAssemblyHostBuilder()
	{
	}

	/// <summary>
	/// Force the use of software rendering instead of attempting to use WebGL.
	/// </summary>
	public WebAssemblyHostBuilder ForceSoftwareRendering()
	{
		_forceSoftwareRendering = true;
		return this;
	}

	bool IPlatformHostBuilder.IsSupported => true;

	UnoPlatformHost IPlatformHostBuilder.Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
		=> new WebAssemblyBrowserHost(appBuilder, _forceSoftwareRendering);
}

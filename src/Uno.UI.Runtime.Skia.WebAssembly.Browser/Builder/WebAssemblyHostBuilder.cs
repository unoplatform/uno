using System;
using System.Text.RegularExpressions;
using Uno.UI.Runtime.Skia.WebAssembly.Browser;

namespace Uno.UI.Hosting;

public partial class WebAssemblyHostBuilder : IPlatformHostBuilder
{
	private bool _forceSoftwareRendering;

	public WebAssemblyHostBuilder()
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

	public bool IsSupported => true;

	public UnoPlatformHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType)
		=> new WebAssemblyBrowserHost(appBuilder, _forceSoftwareRendering);
}

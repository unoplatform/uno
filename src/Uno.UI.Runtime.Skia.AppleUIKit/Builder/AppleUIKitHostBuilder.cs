using System;
using System.Text.RegularExpressions;
using Uno.UI.Runtime.Skia.AppleUIKit;

namespace Uno.UI.Hosting;

internal partial class AppleUIKitHostBuilder : IPlatformHostBuilder
{
	public AppleUIKitHostBuilder()
	{
	}

	public bool IsSupported => true;

	public UnoPlatformHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder)
		=> new AppleUIKitHost(appBuilder);
}

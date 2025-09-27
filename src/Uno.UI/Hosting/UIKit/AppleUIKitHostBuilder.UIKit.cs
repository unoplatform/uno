#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Uno.UI.Helpers;
using Uno.UI.Hosting.UIKit;

namespace Uno.UI.Hosting;

internal partial class AppleUIKitHostBuilder : IPlatformHostBuilder
{
	public AppleUIKitHostBuilder()
	{
	}

	public bool IsSupported => DeviceTargetHelper.IsUIKit();

	public UnoPlatformHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType) =>
		new AppleUIKitHost(appType);
}

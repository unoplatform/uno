#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Uno.UI.Helpers;

namespace Uno.UI.Hosting;

internal partial class AppleUIKitHostBuilder : IPlatformHostBuilder
{
	private Type? _uiApplicationDelegateOverride;

	public AppleUIKitHostBuilder()
	{
	}

	public bool IsSupported => DeviceTargetHelper.IsUIKit();

	public UnoPlatformHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType) =>
		new AppleUIKitHost(appBuilder, _uiApplicationDelegateOverride);
}

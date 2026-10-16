using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Uno.UI.Helpers;
using Uno.UI.Runtime.AppleUIKit;

namespace Uno.UI.Hosting;

internal partial class AppleUIKitHostBuilder : IPlatformHostBuilder, IAppleUIKitHostBuilder
{
	private Type? _uiApplicationDelegateOverride;

	public AppleUIKitHostBuilder()
	{
	}

	public bool IsSupported => DeviceTargetHelper.IsUIKit();

	public UnoPlatformHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder, Type appType) =>
		new AppleUIKitHost(appBuilder, _uiApplicationDelegateOverride);

	public IAppleUIKitHostBuilder UseUIApplicationDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
		where T : UnoUIApplicationDelegate
	{
		_uiApplicationDelegateOverride = typeof(T);

		return this;
	}
}

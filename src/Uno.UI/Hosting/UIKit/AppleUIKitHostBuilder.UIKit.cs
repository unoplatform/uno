#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Uno.UI.Helpers;

#if !UIKIT_SKIA
using Uno.UI.Hosting.UIKit;
#endif

namespace Uno.UI.Hosting;

internal partial class AppleUIKitHostBuilder : IPlatformHostBuilder, IAppleUIKitHostBuilder
{
	private Type? _uiApplicationDelegateOverride;

	public AppleUIKitHostBuilder()
	{
	}

	public bool IsSupported => DeviceTargetHelper.IsUIKit();

	public UnoPlatformHost Create(Func<Microsoft.UI.Xaml.Application> appBuilder) =>
		new AppleUIKitHost(appBuilder, _uiApplicationDelegateOverride);

	public IAppleUIKitHostBuilder UseUIApplicationDelegate<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>()
#if UIKIT_SKIA
		where T : UnoUIApplicationDelegate
#else
		where T : Microsoft.UI.Xaml.Application
#endif
	{
		_uiApplicationDelegateOverride = typeof(T);

		return this;
	}
}

#nullable enable

using System;
using Uno.Foundation.Extensibility;
using Windows.System.Profile.Internal;

namespace Windows.System.Profile;

public static partial class AnalyticsInfo
{
	private static readonly Lazy<IAnalyticsInfoExtension?> _analyticsInfoExtension = new Lazy<IAnalyticsInfoExtension?>(
		() =>
		{
			if (ApiExtensibility.CreateInstance<IAnalyticsInfoExtension>(
				typeof(AnalyticsInfo),
				out var analyticsInfoExtension))
			{
				return analyticsInfoExtension;
			}
			return null;
		});

	private static UnoDeviceForm GetDeviceForm() => _analyticsInfoExtension.Value?.GetDeviceForm() ?? UnoDeviceForm.Unknown;
}

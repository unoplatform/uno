#nullable enable

using System;
using Uno.Foundation.Extensibility;

namespace Windows.System.Profile
{
	public static partial class AnalyticsInfo
	{
		private static Lazy<IAnalyticsInfoExtension?> _analyticsInfoExtension = new Lazy<IAnalyticsInfoExtension?>(
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

	internal interface IAnalyticsInfoExtension
	{
		UnoDeviceForm GetDeviceForm();
	}
}

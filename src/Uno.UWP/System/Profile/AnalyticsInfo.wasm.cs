using System;
using Uno.Foundation;
using Windows.System.Profile.Internal;

using NativeMethods = __Windows.__System.Profile.AnalyticsInfo.NativeMethods;

namespace Windows.System.Profile
{
	public static partial class AnalyticsInfo
	{
		private static UnoDeviceForm GetDeviceForm()
		{
			var typeString = NativeMethods.GetDeviceType();

			if (Enum.TryParse(typeString, true, out UnoDeviceForm deviceForm))
			{
				return deviceForm;
			}
			return UnoDeviceForm.Unknown;
		}
	}
}

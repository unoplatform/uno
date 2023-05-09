using System;
using Uno.Foundation;
using Windows.System.Profile.Internal;

#if NET7_0_OR_GREATER
using NativeMethods = __Windows.__System.Profile.AnalyticsInfo.NativeMethods;
#endif

namespace Windows.System.Profile
{
	public static partial class AnalyticsInfo
	{
		private const string JsType = "Windows.System.Profile.AnalyticsInfo";

		private static UnoDeviceForm GetDeviceForm()
		{
			var typeString = WebAssemblyRuntime.InvokeJS($"{JsType}.getDeviceType()");
			if (Enum.TryParse(typeString, true, out UnoDeviceForm deviceForm))
			{
				return deviceForm;
			}
			return UnoDeviceForm.Unknown;
		}
	}
}

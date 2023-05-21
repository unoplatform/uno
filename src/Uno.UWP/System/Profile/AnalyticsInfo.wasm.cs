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
#if !NET7_0_OR_GREATER
		private const string JsType = "Windows.System.Profile.AnalyticsInfo";
#endif

		private static UnoDeviceForm GetDeviceForm()
		{
			var typeString =
#if NET7_0_OR_GREATER
				NativeMethods.GetDeviceType();
#else
				WebAssemblyRuntime.InvokeJS($"{JsType}.getDeviceType()");
#endif

			if (Enum.TryParse(typeString, true, out UnoDeviceForm deviceForm))
			{
				return deviceForm;
			}
			return UnoDeviceForm.Unknown;
		}
	}
}

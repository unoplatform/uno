#if __SKIA__

namespace Windows.System.Profile
{
	public static partial class AnalyticsInfo
	{
		private static UnoDeviceForm GetDeviceForm() => UnoDeviceForm.Unknown;
	}
}
#endif

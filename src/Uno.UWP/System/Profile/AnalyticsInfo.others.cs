#if NET461 || __NETSTD_REFERENCE__
using Uno.System.Profile;

namespace Windows.System.Profile
{
	public partial class AnalyticsInfo
    {
		private static UnoDeviceForm GetDeviceForm() => UnoDeviceForm.Desktop;
	}
}
#endif

#if NET461 || __NETSTD_REFERENCE__
using Windows.System.Profile.Internal;

namespace Windows.System.Profile;

public partial class AnalyticsInfo
{
	private static UnoDeviceForm GetDeviceForm() => UnoDeviceForm.Desktop;
}
#endif

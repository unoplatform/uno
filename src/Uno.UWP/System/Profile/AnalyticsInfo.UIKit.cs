using UIKit;
using Windows.System.Profile.Internal;

namespace Windows.System.Profile;

public static partial class AnalyticsInfo
{
	private static UnoDeviceForm GetDeviceForm()
	{
		return UIDevice.CurrentDevice.UserInterfaceIdiom switch
		{
			UIUserInterfaceIdiom.Phone => UnoDeviceForm.Mobile,
			UIUserInterfaceIdiom.Pad => UnoDeviceForm.Tablet,
			UIUserInterfaceIdiom.TV => UnoDeviceForm.Television,
			UIUserInterfaceIdiom.CarPlay => UnoDeviceForm.Car,
			_ => UnoDeviceForm.Unknown,
		};
	}
}

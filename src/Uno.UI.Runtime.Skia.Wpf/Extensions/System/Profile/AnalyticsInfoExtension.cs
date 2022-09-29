#nullable disable

using Windows.System.Profile.Internal;

namespace Uno.Extensions.System.Profile
{
	internal class AnalyticsInfoExtension : IAnalyticsInfoExtension
	{
		public UnoDeviceForm GetDeviceForm() => UnoDeviceForm.Desktop;
	}
}

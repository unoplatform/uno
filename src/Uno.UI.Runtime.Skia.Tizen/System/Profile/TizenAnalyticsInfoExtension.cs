using System;
using Tizen.System;
using Windows.System.Profile;

namespace Uno.UI.Runtime.Skia.Tizen.System.Profile
{
	internal class TizenAnalyticsInfoExtension : IAnalyticsInfoExtension
	{
		public TizenAnalyticsInfoExtension(object owner)
		{
		}

		public UnoDeviceForm GetDeviceForm()
		{
			if (Information.TryGetValue<string>("http://tizen.org/feature/profile", out var profile))
			{
				if (profile != null)
				{
					if (profile.Equals("mobile", StringComparison.InvariantCultureIgnoreCase))
					{
						return UnoDeviceForm.Mobile;
					}
					else if (profile.Equals("wearable", StringComparison.InvariantCultureIgnoreCase))
					{
						return UnoDeviceForm.Watch;
					}
					else if (profile.StartsWith("tv", StringComparison.InvariantCultureIgnoreCase))
					{
						return UnoDeviceForm.Television;
					}
				}
			}
			return UnoDeviceForm.Unknown;
		}
	}
}

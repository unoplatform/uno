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
					profile = profile.ToUpperInvariant();
					if (profile.StartsWith("M"))
					{
						return UnoDeviceForm.Mobile;
					}
					else if (profile.StartsWith("W"))
					{
						return UnoDeviceForm.Watch;
					}
					else if (profile.StartsWith("T"))
					{
						return UnoDeviceForm.Television;
					}
				}
			}
			return UnoDeviceForm.Unknown;
		}
	}
}

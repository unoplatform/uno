#if __ANDROID__
using System;
using System.Globalization;
using Android.App;
using Android.Content.Res;
using Android.OS;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;

namespace Windows.System.Profile
{
	public partial class AnalyticsVersionInfo 
	{
		public  string DeviceFamily
		{
			get
			{
				string idiom = "";

				try
				{
					// Threshold in inches for diagonal screensize between what HardwareHelper will consider a tablet or a phone.
					//This is the value given to us by microsoft documentation : https://docs.microsoft.com/en-us/windows/uwp/design/devices/
					double _tabletMinimumSizeThreshold = 7.0;
					
					var displayMetrics = Application.Context.Resources.DisplayMetrics;

					// Turn pixel sizes into inches
					var screenWidth = displayMetrics.WidthPixels / displayMetrics.Xdpi;
					var screenHeight = displayMetrics.HeightPixels / displayMetrics.Ydpi;

					// Use Pythagore to find the diagonal
					var diagonalSize = Math.Sqrt(Math.Pow(screenWidth, 2) + Math.Pow(screenHeight, 2));

					// Is the diagonal larger than the threshold?
					if (diagonalSize >= _tabletMinimumSizeThreshold)
					{
						idiom =  "Tablet";
					}
					else
					{
						idiom = "Phone";
					}
				}
				catch (Exception e)
				{
					e.Log().Error("Could not detect if the device is a tablet or a phone.", e);
				}

				return idiom;
			}
		}

		public  string DeviceFamilyVersion
		{
			get
			{
				return Build.VERSION.Release;
			}
		}
	}
}
#endif

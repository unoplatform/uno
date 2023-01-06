#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Android.Content.PM;

namespace Windows.Graphics.Display
{
    internal static class DisplayOrientationsExtensions
    {
	    public static ScreenOrientation ToScreenOrientation(this DisplayOrientations orientation)
	    {
		    switch (orientation)
		    {
				case DisplayOrientations.Landscape:
				    return ScreenOrientation.Landscape;
				case DisplayOrientations.LandscapeFlipped:
				    return ScreenOrientation.ReverseLandscape;
				case DisplayOrientations.Portrait:
				    return ScreenOrientation.Portrait;
				case DisplayOrientations.PortraitFlipped:
				    return ScreenOrientation.ReversePortrait;
				case DisplayOrientations.Portrait | DisplayOrientations.PortraitFlipped:
				    return ScreenOrientation.SensorPortrait;
				case DisplayOrientations.Landscape | DisplayOrientations.LandscapeFlipped:
				    return ScreenOrientation.SensorLandscape;
				case DisplayOrientations.Portrait | DisplayOrientations.PortraitFlipped | DisplayOrientations.Landscape | DisplayOrientations.LandscapeFlipped:
				    return ScreenOrientation.FullSensor;
				case DisplayOrientations.None:
				default:
					return ScreenOrientation.Unspecified;
			}
	    }
    }
}
#endif

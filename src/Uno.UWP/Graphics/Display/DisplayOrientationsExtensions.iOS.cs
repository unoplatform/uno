#nullable disable

#if __IOS__
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace Windows.Graphics.Display
{
    public static class DisplayOrientationsExtensions
    {
	    public static UIInterfaceOrientationMask ToUIInterfaceOrientationMask(this DisplayOrientations orientation)
	    {
		    UIInterfaceOrientationMask toOrientation = 0;

		    if (orientation == DisplayOrientations.None)
		    {
			    return UIInterfaceOrientationMask.All;
		    }

		    if (orientation.HasFlag(DisplayOrientations.Portrait))
		    {
			    toOrientation |= UIInterfaceOrientationMask.Portrait;
		    }
		    if (orientation.HasFlag(DisplayOrientations.PortraitFlipped))
		    {
			    toOrientation |= UIInterfaceOrientationMask.PortraitUpsideDown;
		    }
		    if (orientation.HasFlag(DisplayOrientations.Landscape))
		    {
			    toOrientation |= UIInterfaceOrientationMask.LandscapeRight;
		    }
		    if (orientation.HasFlag(DisplayOrientations.LandscapeFlipped))
		    {
			    toOrientation |= UIInterfaceOrientationMask.LandscapeLeft;
		    }

		    return toOrientation;
	    }

	    public static UIInterfaceOrientationMask ToUIInterfaceOrientationMask(this UIInterfaceOrientation orientation)
	    {
		    switch (orientation)
		    {
				case UIInterfaceOrientation.Portrait:
				    return UIInterfaceOrientationMask.Portrait;
				case UIInterfaceOrientation.PortraitUpsideDown:
				    return UIInterfaceOrientationMask.PortraitUpsideDown;
				case UIInterfaceOrientation.LandscapeLeft:
				    return UIInterfaceOrientationMask.LandscapeLeft;
				case UIInterfaceOrientation.LandscapeRight:
				    return UIInterfaceOrientationMask.LandscapeRight;
				case UIInterfaceOrientation.Unknown:
				default:
				    return UIInterfaceOrientationMask.Portrait;
		    }
	    }

	    public static UIInterfaceOrientation ToUIInterfaceOrientation(this UIInterfaceOrientationMask orientationMask)
	    {
		    switch (orientationMask)
		    {
				case UIInterfaceOrientationMask.LandscapeLeft:
				    return UIInterfaceOrientation.LandscapeLeft;
				case UIInterfaceOrientationMask.LandscapeRight:
				    return UIInterfaceOrientation.LandscapeRight;
				case UIInterfaceOrientationMask.Portrait:
					return UIInterfaceOrientation.Portrait;
				case UIInterfaceOrientationMask.PortraitUpsideDown:
				    return UIInterfaceOrientation.PortraitUpsideDown;
				default:
				    return UIInterfaceOrientation.Unknown;

		    }
	    }
    }
}
#endif

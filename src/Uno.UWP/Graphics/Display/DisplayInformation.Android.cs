#if __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Uno.UI;

namespace Windows.Graphics.Display
{
    public sealed partial class DisplayInformation
    {
	    static partial void SetOrientationPartial(DisplayOrientations orientations)
	    {
		    var currentActivity = ContextHelper.Current as Activity;
		    if (currentActivity != null)
		    {
			    currentActivity.RequestedOrientation = orientations.ToScreenOrientation();
		    }
	    }
	}
}
#endif
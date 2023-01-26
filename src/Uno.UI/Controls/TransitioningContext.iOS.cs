using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreGraphics;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
#endif

namespace Uno.UI
{
	public class TransitioningContext
	{
		public TransitioningContext()
		{
			IsPushNavigationAnimated = true;
			IsPopNavigationAnimated = true;
		}

		public bool IsPushNavigationAnimated { get; set; }

		public bool IsPopNavigationAnimated { get; set; }
	}
}

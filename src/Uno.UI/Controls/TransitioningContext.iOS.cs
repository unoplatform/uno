using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Foundation;
using UIKit;
using CoreGraphics;

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

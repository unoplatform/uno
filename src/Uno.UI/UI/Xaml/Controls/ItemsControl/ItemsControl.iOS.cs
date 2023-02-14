using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using Uno.UI;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreGraphics;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using nfloat = System.Single;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemsControl : Control
	{
		partial void InitializePartial()
		{

		}

		partial void RequestLayoutPartial()
		{
			SetNeedsLayout();
		}

		partial void RemoveViewPartial(UIView current)
		{
			current.RemoveFromSuperview();
		}
	}
}


using Uno.Extensions;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreGraphics;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public enum ListViewBaseScrollDirection
	{
		Vertical,
		Horizontal,
	}
	//
	// FIXME Merge with ListView and remove the ListViewTypeSelector in favor of using a generic "ListViewItem" and ItemTemplate for its content

}

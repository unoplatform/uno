using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace Windows.UI.Xaml.Controls
{
	public partial class IconElement
    {
		partial void UnregisterSubView()
		{
			if (Subviews.Length > 0)
			{
				Subviews[0].RemoveFromSuperview();
			}
		}

		partial void RegisterSubView(UIView child)
		{
			if (Subviews.Length != 0)
			{
				throw new InvalidOperationException("A Xaml IconElement may not contain more than one child.");
			}

			child.Frame = Bounds;
			child.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
			AddSubview(child);
		}
	}
}

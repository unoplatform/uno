using System;
using AppKit;

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

		partial void RegisterSubView(NSView child)
		{
			if (Subviews.Length != 0)
			{
				throw new InvalidOperationException("A Xaml IconElement may not contain more than one child.");
			}

			child.Frame = Bounds;
			child.AutoresizingMask = NSViewResizingMask.HeightSizable | NSViewResizingMask.WidthSizable;
			AddSubview(child);
		}
	}
}

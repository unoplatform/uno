using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;

namespace Microsoft.UI.Xaml
{
	public partial class ElementStub : FrameworkElement
	{
		private UIView SwapViews(UIView oldView, Func<UIView> newViewProvider)
		{
			var newContent = newViewProvider();

			var currentSuperview = oldView?.Superview;

			if (currentSuperview is not null)
			{
				RaiseMaterializing();

				// Use stub relative insertion, as InsertAt uses a CALayer based
				// insertion which requires to know about the layers of the siblings
				// and the manually added layers. Some of those manual layers 
				// can be added when a background or border is set on a Panel or Border.
				currentSuperview.InsertSubviewAbove(newContent, oldView);

				oldView.RemoveFromSuperview();

				return newContent;
			}

			return null;
		}
	}
}

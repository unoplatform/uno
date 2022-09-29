using System;
using System.Collections.Generic;
using System.Linq;
using Android.Views;
using Uno.Extensions;
using Uno.UI;

namespace Windows.UI.Xaml.Media
{
	public partial class VisualTreeHelper
	{
		internal static void SwapViews(UIElement oldView, UIElement newView)
		{
			var parentViewGroup = oldView?.Parent as ViewGroup;
			var currentPosition = parentViewGroup?.GetChildren().IndexOf(oldView);

			if (parentViewGroup != null && currentPosition != null && currentPosition.Value != -1)
			{
				parentViewGroup.RemoveViewAt(currentPosition.Value);

				var unoViewGroup = parentViewGroup as UnoViewGroup;

				if (unoViewGroup != null)
				{
					var newContentAsFrameworkElement = newView as IFrameworkElement;
					if (newContentAsFrameworkElement != null)
					{
						newContentAsFrameworkElement.TemplatedParent = (unoViewGroup as IFrameworkElement)?.TemplatedParent;
					}
					unoViewGroup.AddView(newView, currentPosition.Value);
				}
				else
				{
					parentViewGroup.AddView(newView, currentPosition.Value);
				}
			}
		}
	}
}

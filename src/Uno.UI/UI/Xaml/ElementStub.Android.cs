using Android.Views;
using Uno.Extensions;
using Windows.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;

namespace Windows.UI.Xaml
{
	public partial class ElementStub : FrameworkElement
	{
		private View SwapViews(View oldView, Func<View> newViewProvider)
		{
			var parentViewGroup = oldView?.Parent as ViewGroup;
			var currentPosition = parentViewGroup?.GetChildren().IndexOf(oldView);

			if (currentPosition != null && currentPosition.Value != -1)
			{
				var newView = newViewProvider();
				parentViewGroup.RemoveViewAt(currentPosition.Value);

				RaiseMaterializing();

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

				return newView;
			}

			return null;
		}
	}
}

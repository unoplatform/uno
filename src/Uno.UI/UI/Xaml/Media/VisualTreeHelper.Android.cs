#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Android.Views;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.UI;
using Uno.Foundation.Logging;

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
				if (typeof(VisualTreeHelper).Log().IsEnabled(LogLevel.Trace))
				{
					typeof(VisualTreeHelper).Log().Trace($"Swapping {oldView} from {currentPosition.Value}");
				}

				RemoveViewAt(parentViewGroup, currentPosition.Value);

				var unoViewGroup = parentViewGroup as UnoViewGroup;

				if (unoViewGroup != null)
				{
					var newContentAsFrameworkElement = newView as IFrameworkElement;
					if (newContentAsFrameworkElement != null)
					{
						newContentAsFrameworkElement.TemplatedParent = (unoViewGroup as IFrameworkElement)?.TemplatedParent;
					}
				}

				InsertViewAt(parentViewGroup, currentPosition.Value, newView);
			}
			else
			{
				if (typeof(VisualTreeHelper).Log().IsEnabled(LogLevel.Trace))
				{
					typeof(VisualTreeHelper).Log().Trace($"Unable to remove {oldView} parentViewGroup:{parentViewGroup} currentPosition:{currentPosition}");
				}
			}

			void RemoveViewAt(ViewGroup parent, int index)
			{
				if (parent is Panel panel)
				{
					panel.Children.RemoveAt(currentPosition.Value);
				}
				else
				{
					parent.RemoveViewAt(currentPosition.Value);
				}
			}

			void InsertViewAt(ViewGroup parent, int index, ViewGroup view)
			{
				if (view is Panel panel && view is UIElement viewAsUIElement)
				{
					panel.Children.Insert(index, viewAsUIElement);
				}
				else
				{
					parent.AddView(view, index);
				}
			}
		}
	}
}

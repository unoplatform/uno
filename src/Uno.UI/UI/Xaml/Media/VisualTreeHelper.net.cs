#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;

namespace Windows.UI.Xaml.Media
{
	public partial class VisualTreeHelper
	{
		internal static void SwapViews(UIElement oldView, UIElement newView)
		{
			if ((oldView as FrameworkElement)?.Parent is FrameworkElement parentElement)
			{
				var currentPosition = parentElement.GetChildren().IndexOf(oldView);

				if (currentPosition != -1)
				{
					parentElement.RemoveChild(oldView);

					parentElement.AddChild(newView, currentPosition);
				}
			}
		}
	}
}

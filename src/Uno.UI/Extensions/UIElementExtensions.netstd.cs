using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Uno.UI.Extensions
{
	public static class UIElementExtensions
	{

		/// <summary>
		/// Returns the root of the view's local visual tree.
		/// </summary>
		public static FrameworkElement GetTopLevelParent(this UIElement view)
		{
			var current = view as FrameworkElement;
			while (current != null)
			{
				var parent = current.Parent as FrameworkElement;
				if (parent == null)
				{
					return current;
				}
				current = parent;
			}

			return null;
		}

	}
}

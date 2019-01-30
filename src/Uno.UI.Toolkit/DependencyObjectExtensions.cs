using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Toolkit.Extensions
{
	internal static class DependencyObjectExtensions
	{
#if !XAMARIN && !__WASM__
		internal static T FindFirstParent<T>(this DependencyObject obj) where T : DependencyObject
		{
			var parent = VisualTreeHelper.GetParent(obj);

			if (parent == null)
			{
				return null;
			}
			else if (parent is T t)
			{
				return t;
			}
			else
			{
				return FindFirstParent<T>(parent);
			}
		}
#endif
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Sample.Views.Helper
{
    public static class DependencyObjectExtensions
    {
		public static IEnumerable<DependencyObject> GetChildren(this DependencyObject obj)
		{
			var count = VisualTreeHelper.GetChildrenCount(obj);

			for (int i = 0; i < count; i++)
			{
				yield return VisualTreeHelper.GetChild(obj, i);
			}
		}


		public static T FindFirstChild<T>(this DependencyObject element, int? childLevelLimit = null, bool includeCurrent = true)
			where T :
#if HAS_UNO
			class,
#endif
			DependencyObject
		{
			return element.FindFirstChild<T>(x => true, childLevelLimit, includeCurrent);
		}

		public static T FindFirstChild<T>(this DependencyObject element, Func<T, bool> selector, int? childLevelLimit = null, bool includeCurrent = true)
			where T :
#if HAS_UNO
			class,
#endif
			DependencyObject
		{
			return InnerFindFirstChild(new[] { element }.Trim(), selector, childLevelLimit, includeCurrent);
		}

		private static T InnerFindFirstChild<T>(IEnumerable<DependencyObject> elements, Func<T, bool> selector, int? childLevelLimit, bool includeCurrentLevel)
			where T :
#if HAS_UNO
			class,
#endif
			DependencyObject
		{
			if (elements.None() || (childLevelLimit.HasValue && childLevelLimit <= 0))
			{
				return null;
			}
			else if (includeCurrentLevel)
			{
				return elements.OfType<T>().FirstOrDefault(selector)
					?? InnerFindFirstChild(elements.SelectMany(GetChildren), selector, childLevelLimit.HasValue ? childLevelLimit - 1 : null, true);
			}
			else
			{
				return InnerFindFirstChild(elements.SelectMany(GetChildren), selector, childLevelLimit.HasValue ? childLevelLimit - 1 : null, true);
			}
		}

		public static T FindFirstParent<T>(this DependencyObject element, bool includeCurrent = true)
			where T :
#if HAS_UNO
			class,
#endif
			DependencyObject
		{
			return element.GetParentHierarchy(includeCurrent).OfType<T>().FirstOrDefault();
		}

		public static T FindFirstParent<T>(this DependencyObject element, Func<T, bool> selector, bool includeCurrent = true)
			where T :
#if HAS_UNO
			class,
#endif
			DependencyObject
		{
			return element.GetParentHierarchy(includeCurrent).OfType<T>().FirstOrDefault(selector);
		}


		public static IEnumerable<DependencyObject> GetParentHierarchy(this DependencyObject element, bool includeCurrent = true)
		{
			if (includeCurrent)
			{
				yield return element;
			}

			for (var parent = (element as FrameworkElement).SelectOrDefault(e => e.Parent) ?? VisualTreeHelper.GetParent(element);
				 parent != null;
				 parent = VisualTreeHelper.GetParent(parent))
			{
				yield return parent;
			}
		}
	}
}

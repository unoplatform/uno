using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Disposables;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI
{
	public static partial class ViewExtensions
	{
		/// <summary>
		/// (Mock) Gets an enumerator containing all the children of a View group
		/// </summary>
		/// <param name="group"></param>
		/// <returns></returns>
		internal static IEnumerable<object> GetChildren(this object group)
		{
			return Array.Empty<object>();
		}

		internal static FrameworkElement GetTopLevelParent(this UIElement view) => throw new NotImplementedException();

		internal static T FindFirstChild<T>(this FrameworkElement root) where T : FrameworkElement
		{
			return root.GetDescendants().OfType<T>().FirstOrDefault();
		}

		private static IEnumerable<FrameworkElement> GetDescendants(this FrameworkElement root)
		{
			foreach (var child in root._children)
			{
				yield return child as FrameworkElement;

				foreach (var descendant in (child as FrameworkElement).GetDescendants())
				{
					yield return descendant;
				}
			}
		}
	}
}

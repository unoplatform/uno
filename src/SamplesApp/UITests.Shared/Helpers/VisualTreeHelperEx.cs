using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace UITests.Helpers
{
	/// <summary>
	/// Contains various methods to debug the visual tree
	/// </summary>
	internal static class VisualTreeHelperEx
	{
		/// <summary>
		/// Prints the visual tree starting from the <paramref name="reference"/> node.
		/// </summary>
		/// <param name="reference"></param>
		/// <returns></returns>
		public static string TreeGraph(this DependencyObject reference) => TreeGraph(reference, DebugVTNode);

		/// <summary>
		/// Prints the visual tree starting from the <paramref name="reference"/> node with custom way to <paramref name="describe"/> each node.
		/// </summary>
		/// <param name="reference"></param>
		/// <param name="describe"></param>
		/// <returns></returns>
		public static string TreeGraph(this DependencyObject reference, Func<DependencyObject, string> describe)
		{
			var buffer = new StringBuilder();
			Walk(reference);
			return buffer.ToString();

			void Walk(DependencyObject o, int depth = 0)
			{
				Print(o, depth);
				foreach (var child in GetChildren(o))
				{
					Walk(child, depth + 1);
				}
			}
			void Print(DependencyObject o, int depth)
			{
				buffer
					.Append(new string(' ', depth * 4))
					.Append(describe(o))
					.AppendLine();
			}
		}

		private static string DebugVTNode(DependencyObject x)
		{
			var xname = (x as FrameworkElement)?.Name;

			return new StringBuilder()
				.Append(x.GetType().Name)
				.Append(xname != null ? $"#{xname}" : string.Empty)
				.ToString();
		}

		public static T GetFirstAncestor<T>(this DependencyObject reference) => GetAncestors(reference)
			.OfType<T>()
			.FirstOrDefault();

		public static T GetFirstAncestor<T>(this DependencyObject reference, Func<T, bool> predicate) => GetAncestors(reference)
			.OfType<T>()
			.FirstOrDefault(predicate);

		public static T GetFirstDescendant<T>(DependencyObject reference) => GetDescendants(reference)
			.OfType<T>()
			.FirstOrDefault();

		public static T GetFirstDescendant<T>(DependencyObject reference, Func<T, bool> predicate) => GetDescendants(reference)
			.OfType<T>()
			.FirstOrDefault(predicate);

		public static IEnumerable<DependencyObject> GetAncestors(this DependencyObject o)
		{
			if (o is null) yield break;
			while (VisualTreeHelper.GetParent(o) is DependencyObject parent)
			{
				yield return o = parent;
			}
		}

		public static IEnumerable<DependencyObject> GetDescendants(DependencyObject reference)
		{
			foreach (var child in GetChildren(reference))
			{
				yield return child;
				foreach (var grandchild in GetDescendants(child))
				{
					yield return grandchild;
				}
			}
		}

		public static IEnumerable<DependencyObject> GetChildren(DependencyObject reference)
		{
			return Enumerable
				.Range(0, VisualTreeHelper.GetChildrenCount(reference))
				.Select(x => VisualTreeHelper.GetChild(reference, x));
		}
	}
}

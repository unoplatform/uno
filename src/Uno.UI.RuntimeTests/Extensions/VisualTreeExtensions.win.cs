#if !HAS_UNO
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using _View = Microsoft.UI.Xaml.DependencyObject;

namespace Uno.UI.Extensions; // same as in ViewExtensions.visual-tree.cs

internal static class VisualTreeExtensions // non-uno counterpart to src\Uno.UI\Extensions\ViewExtensions.visual-tree.cs
{
#if !WINAPPSDK
	/// <summary>
	/// Returns the first descendant of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <remarks>The crawling is done depth first.</remarks>
	public static T? FindFirstDescendant<T>(this _View? reference) => reference.EnumerateDescendants()
		.OfType<T>()
		.FirstOrDefault();

	/// <summary>
	/// Returns the first descendant of a specified type that satisfies the <paramref name="predicate"/>.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="predicate">A function to test each node for a condition.</param>
	/// <remarks>The crawling is done depth first.</remarks>
	public static T? FindFirstDescendant<T>(this _View? reference, Func<T, bool> predicate) => reference.EnumerateDescendants()
		.OfType<T>()
		.FirstOrDefault(predicate);

	/// <summary>
	/// Returns the first descendant of a specified type that satisfies the <paramref name="predicate"/> whose ancestors (up to <paramref name="reference"/>) and itself satisfy the <paramref name="hierarchyPredicate"/>.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="hierarchyPredicate">A function to test each ancestor for a condition.</param>
	/// <param name="predicate">A function to test each descendant for a condition.</param>
	/// <remarks>The crawling is done depth first.</remarks>
	public static T? FindFirstDescendant<T>(this _View? reference, Func<_View, bool> hierarchyPredicate, Func<T, bool> predicate) => reference.EnumerateDescendants(hierarchyPredicate)
		.OfType<T>()
		.FirstOrDefault(predicate);

	/// <summary>
	/// Returns all the visual descendants of a node.
	/// </summary>
	/// <param name="reference">Any node of the visual tree</param>
	public static IEnumerable<_View> EnumerateDescendants(this _View? reference) => reference.EnumerateDescendants(x => true);

	/// <summary>
	/// Returns all the visual descendants of a node that satisfies the <paramref name="hierarchyPredicate"/>.
	/// </summary>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="hierarchyPredicate"></param>
	/// <returns></returns>
	public static IEnumerable<_View> EnumerateDescendants(this _View? reference, Func<_View, bool> hierarchyPredicate)
	{
		foreach (var child in reference.EnumerateChildren().Where(hierarchyPredicate))
		{
			yield return child;
			foreach (var grandchild in child.EnumerateDescendants(hierarchyPredicate))
			{
				yield return grandchild;
			}
		}
	}
#endif

	private static IEnumerable<_View> EnumerateChildren(this _View? reference)
	{
		return Enumerable
			.Range(0, VisualTreeHelper.GetChildrenCount(reference))
			.Select(x => VisualTreeHelper.GetChild(reference, x));
	}
}
#endif

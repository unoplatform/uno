#nullable enable

// note: This file is selectively included on windows build in: src\Uno.UI.Toolkit\Uno.UI.Toolkit.Windows.csproj
// Any other source file that we depends here should also be made available there.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.Foundation;

#if __IOS__
using UIKit;
using _View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using _View = AppKit.NSView;
#elif __ANDROID__
using _View = Android.Views.View;
#else
using _View = Windows.UI.Xaml.DependencyObject;
#endif

using static System.Reflection.BindingFlags;
using static Uno.UI.Extensions.PrettyPrint;

namespace Uno.UI.Extensions;

#if WINAPPSDK || WINDOWS_UWP
internal
#else
public
#endif

static partial class ViewExtensions
{
	/// <summary>
	/// Produces a text representation of the visual tree.
	/// </summary>
	/// <param name="reference">Any node of the visual tree</param>
	public static string TreeGraph(this _View reference) => TreeGraph(reference, DescribeVTNode);

	public static string TreeGraph(this _View reference, Func<object, IEnumerable<string>> describeProperties) =>
		TreeGraph(reference, x => DescribeVTNode(x, describeProperties));

	/// <summary>
	/// Produces a text representation of the visual tree, using the provided method of description.
	/// </summary>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="describe">A function to describe a visual tree node in a single line.</param>
	/// <returns></returns>
	public static string TreeGraph(this _View reference, Func<_View, string> describe)
	{
		var buffer = new StringBuilder();
		Walk(reference);
		return buffer.ToString();

		void Walk(_View o, int depth = 0)
		{
			Print(o, depth);
			foreach (var child in EnumerateChildren(o))
			{
				Walk(child, depth + 1);
			}
		}
		void Print(_View o, int depth)
		{
			buffer
				.Append(new string(' ', depth * 4))
				.Append(describe(o))
				.AppendLine();
		}
	}

	private static string DescribeVTNode(object x)
	{
		return DescribeVTNode(x, GetDetails);

		static IEnumerable<string> GetDetails(object x)
		{
			if (x is ListViewItem lvi)
			{
				yield return $"Index={(ItemsControl.ItemsControlFromItemContainer(lvi)?.IndexFromContainer(lvi) ?? -1)}";
			}
#if __ANDROID__
			if (x is Android.Views.View v)
			{
				yield return $"LTRB={v.Left},{v.Top},{v.Right},{v.Bottom}";
			}
#elif __IOS__
			if (x is _View view && view.Superview is { })
			{
				var abs = view.Superview.ConvertPointToView(view.Frame.Location, toView: null);
				yield return $"Abs=[Rect {view.Frame.Width:0.#}x{view.Frame.Height:0.#}@{abs.X:0.#},{abs.Y:0.#}]";
			}
#endif
			if (x is FrameworkElement fe)
			{
				yield return $"Actual={fe.ActualWidth}x{fe.ActualHeight}";
				yield return $"HV={fe.HorizontalAlignment}/{fe.VerticalAlignment}";
			}
			if (x is ScrollViewer sv)
			{
				yield return $"Offset=({sv.HorizontalOffset},{sv.VerticalOffset}), Viewport=({sv.ViewportHeight},{sv.ViewportWidth}), Extent=({sv.ExtentHeight},{sv.ExtentWidth})";
			}
			if (TryGetDpValue<CornerRadius>(x, "CornerRadius", out var cr)) yield return $"CornerRadius={FormatCornerRadius(cr)}";
			if (TryGetDpValue<Thickness>(x, "Margin", out var margin)) yield return $"Margin={FormatThickness(margin)}";
			if (TryGetDpValue<Thickness>(x, "Padding", out var padding)) yield return $"Padding={FormatThickness(padding)}";

			if (TryGetDpValue<double>(x, "Opacity", out var opacity)) yield return $"Opacity={opacity}";
			if (TryGetDpValue<Visibility>(x, "Visibility", out var visibility)) yield return $"Visibility={visibility}";
		}
	}

	internal static string DescribeVTNode(object x, Func<object, IEnumerable<string>> describeProperties)
	{
		if (x is null) return "<null>";

		return new StringBuilder()
			.Append(x.GetType().Name)
			.Append((x as FrameworkElement)?.Name is string { Length: > 0 } xname ? $"#{xname}" : string.Empty)
			//.Append($"@{x.GetHashCode():X8}")
			.Append(GetPropertiesDescriptionSafe())
			.ToString();

		string? GetPropertiesDescriptionSafe()
		{
			try
			{
				return string.Join(", ", describeProperties(x)) is { Length: > 0 } propertiesDescription
					? $" // {propertiesDescription}"
					: null;
			}
			catch (Exception e)
			{
				return $"// threw {e.GetType().Name}: {EscapeMultiline(e.Message, escapeTabs: true)}";
			}
		}
	}

	internal static bool TryGetDpValue<T>(object owner, string property, [NotNullWhen(true)] out T? value)
	{
		if (owner is DependencyObject @do &&
			owner.GetType()
				.GetProperty($"{property}Property", Public | Static | FlattenHierarchy)
				?.GetValue(null, null) is DependencyProperty dp)
		{
			value = (T)@do.GetValue(dp);
			return true;
		}

		value = default;
		return false;
	}

	/// <summary>
	/// Returns the first ancestor of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of ancestor to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <remarks>First Counting from the <paramref name="reference"/> and not from the root of tree.</remarks>
	public static T? FindFirstAncestor<T>(this _View? reference) => EnumerateAncestors(reference)
		.OfType<T>()
		.FirstOrDefault();

	/// <summary>
	/// Returns the first ancestor of a specified type that satisfies the <paramref name="predicate"/>.
	/// </summary>
	/// <typeparam name="T">The type of ancestor to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="predicate">A function to test each node for a condition.</param>
	/// <remarks>First Counting from the <paramref name="reference"/> and not from the root of tree.</remarks>
	public static T? FindFirstAncestor<T>(this _View? reference, Func<T, bool> predicate) => EnumerateAncestors(reference)
		.OfType<T>()
		.FirstOrDefault(predicate);

	/// <summary>
	/// Returns the first descendant of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	public static T? FindFirstDescendant<T>(this _View? reference) => EnumerateDescendants(reference)
		.OfType<T>()
		.FirstOrDefault();

	/// <summary>
	/// Returns the first descendant of a specified type that satisfies the <paramref name="predicate"/>.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="predicate">A function to test each node for a condition.</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	public static T? FindFirstDescendant<T>(this _View? reference, Func<T, bool> predicate) => EnumerateDescendants(reference)
		.OfType<T>()
		.FirstOrDefault(predicate);

	/// <summary>
	/// Returns the first descendant of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="name">x:Name of the node</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	public static T? FindFirstDescendant<T>(this _View? reference, string name) where T : FrameworkElement => EnumerateDescendants(reference)
		.OfType<T>()
		.FirstOrDefault(x => x.Name == name);

	/// <summary>
	/// Returns the first descendant of a specified type that satisfies the <paramref name="predicate"/> whose ancestors (up to <paramref name="reference"/>) and itself satisfy the <paramref name="hierarchyPredicate"/>.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="hierarchyPredicate">A function to test each ancestor for a condition.</param>
	/// <param name="predicate">A function to test each descendant for a condition.</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	public static T? FindFirstDescendant<T>(this _View? reference, Func<_View, bool> hierarchyPredicate, Func<T, bool> predicate) => EnumerateDescendants(reference, hierarchyPredicate)
		.OfType<T>()
		.FirstOrDefault(predicate);

	/// <summary>
	/// Returns all the visual descendants of a node.
	/// </summary>
	/// <param name="reference">Any node of the visual tree</param>
	public static IEnumerable<_View> EnumerateDescendants(this _View? reference) => EnumerateDescendants(reference, x => true);

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

	// note: methods for retrieving children/ancestors exist with varying signatures.
	// re-implementing them with unified & more inclusive signature for convenience.
#if __IOS__ || __MACOS__
	internal static IEnumerable<_View> EnumerateAncestors(this _View? o)
	{
		if (o is null) yield break;
		while (o.Superview is _View parent)
		{
			yield return o = parent;
		}
	}

	internal static IEnumerable<_View> EnumerateChildren(this _View? o)
	{
		if (o is null) return Enumerable.Empty<_View>();
		return o.Subviews;
	}
#elif __ANDROID__
	internal static IEnumerable<_View> EnumerateAncestors(this _View? o)
	{
		if (o is null) yield break;

		while (o.Parent is _View parent)
		{
			yield return o = parent;
		}
	}

	internal static IEnumerable<_View> EnumerateChildren(this _View? reference)
	{
		if (reference is Android.Views.ViewGroup vg)
		{
			return Enumerable
				.Range(0, vg.ChildCount)
				.Select(vg.GetChildAt)
				.Where(x => x != null).Cast<_View>();
		}

		return Enumerable.Empty<_View>();
	}
#else
	internal static IEnumerable<_View> EnumerateAncestors(this _View? o)
	{
		if (o is null) yield break;
		while (VisualTreeHelper.GetParent(o) is { } parent)
		{
			yield return o = parent;
		}
	}

	internal static IEnumerable<_View> EnumerateChildren(this _View? reference)
	{
		return Enumerable
			.Range(0, VisualTreeHelper.GetChildrenCount(reference))
			.Select(x => VisualTreeHelper.GetChild(reference, x));
	}
#endif
}

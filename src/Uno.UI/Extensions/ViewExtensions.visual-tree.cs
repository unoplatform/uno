#nullable enable

// note: This file is selectively included on windows build in: src\Uno.UI.Toolkit\Uno.UI.Toolkit.Windows.csproj
// Any other source file that we depends here should also be made available there.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Foundation;
using Uno.Extensions;

#if __APPLE_UIKIT__
using UIKit;
using _View = UIKit.UIView;
#elif __ANDROID__
using _View = Android.Views.View;
#else
using _View = Microsoft.UI.Xaml.DependencyObject;
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

	public static string TreeGraph(
		this object reference,
		Func<object, IEnumerable<string>> describeProperties,
		Func<object, IEnumerable<object>, IEnumerable<object>>? getMembers = null
	) => TreeGraph(reference, x => DescribeVTNode(x, describeProperties), getMembers);

	/// <summary>
	/// Produces a text representation of the visual tree, using the provided method of description.
	/// </summary>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="describe">A function to describe a visual tree node in a single line.</param>
	/// <returns></returns>
	public static string TreeGraph(
		this object reference,
		Func<object, string> describe,
		Func<object, IEnumerable<object>, IEnumerable<object>>? getMembers = null
	)
	{
		var buffer = new StringBuilder();
		Walk(reference);
		return buffer.ToString();

		void Walk(object o, int depth = 0)
		{
			Print(o, depth);

			var children = (o as _View)?.EnumerateChildren().Cast<object>() ?? Array.Empty<object>();
			children = getMembers?.Invoke(o, children) ?? children;
			foreach (var child in children)
			{
				Walk(child, depth + 1);
			}
		}
		void Print(object o, int depth)
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
#if true // positioning properties/hints
			if (x is FrameworkElement { Parent: Grid } fe3)
			{
				int c = Grid.GetColumn(fe3), cspan = Grid.GetColumnSpan(fe3),
					r = Grid.GetRow(fe3), rspan = Grid.GetRowSpan(fe3);

				yield return $"R{FormatRange(r, rspan)}C{FormatRange(c, cspan)}";
				string FormatRange(int x, int span) => span > 1 ? $"{x}-{x + span - 1}" : $"{x}";
			}
			if (x is Grid g)
			{
				if (g.ColumnDefinitions is { Count: > 0 } columns)
				{
					yield return $"Columns={string.Join(',', columns.Select(FormatGridDefinition))}";
				}
				if (g.RowDefinitions is { Count: > 0 } rows)
				{
					yield return $"Rows={string.Join(',', rows.Select(FormatGridDefinition))}";
				}
			}
			if (x is ListViewItem lvi)
			{
				yield return $"Index={(ItemsControl.ItemsControlFromItemContainer(lvi)?.IndexFromContainer(lvi) ?? -1)}";
			}
#endif
#if false // binding: templated-parent and data-context
			if (x is UIElement uie)
			{
#if HAS_UNO
				yield return $"TP={FormatType(uie.GetTemplatedParent())}";
#endif
				yield return $"DC={FormatType(uie.DataContext)}";
			}
#endif
#if false // native layout coordinates
#if __ANDROID__
			if (x is Android.Views.View v)
			{
				yield return $"LTRB={v.Left},{v.Top},{v.Right},{v.Bottom}";
			}
#elif __APPLE_UIKIT__
			if (x is _View view && view.Superview is { })
			{
				var abs = view.Superview.ConvertPointToView(view.Frame.Location, toView: null);
				yield return $"Abs=[Rect {view.Frame.Width:0.#}x{view.Frame.Height:0.#}@{abs.X:0.#},{abs.Y:0.#}]";
			}
#endif
#endif
#if true // framework layout properties
			if (x is FrameworkElement fe)
			{
				if (fe.Parent is FrameworkElement parent)
				{
					yield return $"XY={FormatPoint(fe.TransformToVisual(parent).TransformPoint(default))}";
				}
				yield return $"Actual={FormatSize(fe.ActualWidth, fe.ActualHeight)}";
#if HAS_UNO
				//yield return $"Available={FormatSize(fe.m_previousAvailableSize)}";
#endif
				//yield return $"Desired={FormatSize(fe.DesiredSize)}";
				//yield return $"Render={FormatSize(fe.RenderSize)}";
#if HAS_UNO
				//if ($"{(fe.IsMeasureDirty ? "M" : null)}{(fe.IsMeasureDirtyPath ? "m" : null)}{(fe.IsArrangeDirty ? "A" : null)}{(fe.IsArrangeDirtyPath ? "a" : null)}" is { Length: > 0 } dirty)
				//	yield return $"Dirty={dirty}";
#endif
				yield return $"Constraints=[{fe.MinWidth:0.#},{fe.Width:0.#},{fe.MaxWidth:0.#}]x[{fe.MinHeight:0.#},{fe.Height:0.#},{fe.MaxHeight:0.#}]";
				yield return $"HV={fe.HorizontalAlignment}/{fe.VerticalAlignment}";
			}
			if (x is ScrollViewer sv)
			{
				yield return $"Offset={FormatPoint(sv.HorizontalOffset, sv.VerticalOffset)}";
				yield return $"Viewport={FormatSize(sv.ViewportWidth, sv.ViewportHeight)}";
				yield return $"Extent={FormatSize(sv.ExtentWidth, sv.ExtentHeight)}";
			}
			//if (TryGetDpValue<CornerRadius>(x, "CornerRadius", out var cr)) yield return $"CornerRadius={FormatCornerRadius(cr)}";
			if (TryGetDpValue<Thickness>(x, "Margin", out var margin)) yield return $"Margin={FormatThickness(margin)}";
			if (TryGetDpValue<Thickness>(x, "Padding", out var padding)) yield return $"Padding={FormatThickness(padding)}";
#endif

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
				GetProperty(owner.GetType(), $"{property}Property")
				?.GetValue(null, null) is DependencyProperty dp)
		{
			value = (T)@do.GetValue(dp);
			return true;
		}

		value = default;
		return false;

		[UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "`Uno.UI.SourceGenerators/BindableTypeProviders` / `BindableMetadata.g.cs` ensures the property exists.")]
		[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "`Uno.UI.SourceGenerators/BindableTypeProviders` / `BindableMetadata.g.cs` ensures the property exists.")]
		static PropertyInfo? GetProperty(Type type, string propertyName)
			=> type.GetProperty(propertyName, Public | Static | FlattenHierarchy);
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
	/// Returns the first ancestor of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of ancestor to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <remarks>First Counting from the <paramref name="reference"/> and not from the root of tree.</remarks>
	/// <exception cref="Exception">If the specified node could not be found.</exception>
	public static T? FindFirstAncestorOrThrow<T>(this _View? reference) => EnumerateAncestors(reference)
		.OfType<T>()
		.FirstOrDefault() ??
		throw new Exception($"Unable to find element: {typeof(T).Name}");


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
	/// Returns the first descendant of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	/// <exception cref="Exception">If the specified node could not be found.</exception>
	public static T FindFirstDescendantOrThrow<T>(this _View? reference) where T : FrameworkElement => EnumerateDescendants(reference)
		.OfType<T>()
		.FirstOrDefault() ??
		throw new Exception($"Unable to find element: {typeof(T).Name}");

	/// <summary>
	/// Returns the first descendant of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="name">x:Name of the node</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	/// <exception cref="Exception">If the specified node could not be found.</exception>
	public static T FindFirstDescendantOrThrow<T>(this _View? reference, string name) where T : FrameworkElement => EnumerateDescendants(reference)
		.OfType<T>()
		.FirstOrDefault(x => x.Name == name) ??
		throw new Exception($"Unable to find element: {typeof(T).Name}#{name}");


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

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static _View? AsNativeView(this object o) => o as _View;

	// note: methods for retrieving children/ancestors exist with varying signatures.
	// re-implementing them with unified & more inclusive signature for convenience.
#if __APPLE_UIKIT__
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

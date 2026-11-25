#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using static System.FormattableString;
using static System.Reflection.BindingFlags;
using static Uno.UI.Extensions.PrettyPrint;

namespace Uno.UI.Extensions;

public static partial class VisualTreeExtensions
{
	/// <summary>
	/// Produces a text representation of the visual tree below <paramref name="reference"/>: one node per line, indented by depth,
	/// with its type, x:Name and a default set of layout details.
	/// </summary>
	/// <param name="reference">Any node of the visual tree</param>
	public static string TreeGraph(this DependencyObject reference) => TreeGraph(reference, DescribeVTNode);

	/// <summary>
	/// Produces a text representation of the visual tree below <paramref name="reference"/>: one node per line, indented by depth,
	/// with its type, x:Name and the details <paramref name="describeProperties"/> returns for it.
	/// </summary>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="describeProperties">A function returning the details to show for a node, e.g. <c>Margin=4</c>.</param>
	public static string TreeGraph(this DependencyObject reference, Func<object, IEnumerable<string>> describeProperties) =>
		TreeGraph(reference, x => DescribeVTNode(x, describeProperties));

	/// <summary>
	/// Produces a text representation of the tree below <paramref name="reference"/>: one node per line, indented by depth,
	/// with its type, x:Name and the details <paramref name="describeProperties"/> returns for it.
	/// </summary>
	/// <param name="reference">The root node</param>
	/// <param name="describeProperties">A function returning the details to show for a node, e.g. <c>Margin=4</c>.</param>
	/// <param name="getMembers">A function returning the nodes to visit under a node, given its visual children; <see langword="null"/> to visit the visual children.</param>
	public static string TreeGraph(
		this object reference,
		Func<object, IEnumerable<string>> describeProperties,
		Func<object, IEnumerable<object>, IEnumerable<object>>? getMembers = null
	) => TreeGraph(reference, x => DescribeVTNode(x, describeProperties), getMembers);

	/// <summary>
	/// Produces a text representation of the tree below <paramref name="reference"/>: one node per line, indented by depth,
	/// using the provided method of description.
	/// </summary>
	/// <param name="reference">The root node</param>
	/// <param name="describe">A function to describe a visual tree node in a single line.</param>
	/// <param name="getMembers">A function returning the nodes to visit under a node, given its visual children; <see langword="null"/> to visit the visual children.</param>
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

			var children = (o as DependencyObject)?.EnumerateChildren().Cast<object>() ?? Array.Empty<object>();
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
				yield return Invariant($"Constraints=[{fe.MinWidth:0.#},{fe.Width:0.#},{fe.MaxWidth:0.#}]x[{fe.MinHeight:0.#},{fe.Height:0.#},{fe.MaxHeight:0.#}]");
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

			if (TryGetDpValue<double>(x, "Opacity", out var opacity)) yield return Invariant($"Opacity={opacity}");
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

	/// <summary>
	/// Gets the value of the dependency property named <paramref name="property"/> on <paramref name="owner"/>,
	/// found by reflection through its static <c>{property}Property</c> member.
	/// </summary>
	/// <typeparam name="T">The expected type of the value.</typeparam>
	/// <param name="owner">The object to read the property from.</param>
	/// <param name="property">The name of the property, without the <c>Property</c> suffix.</param>
	/// <param name="value">The value, when found.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="owner"/> is a <see cref="DependencyObject"/> with such a property and its value is a non-null <typeparamref name="T"/>;
	/// otherwise <see langword="false"/>.
	/// </returns>
	/// <remarks>Meant for diagnostics: in a trimmed app, the <c>{property}Property</c> member may have been removed, in which case this returns <see langword="false"/>.</remarks>
	public static bool TryGetDpValue<T>(object owner, string property, [NotNullWhen(true)] out T? value)
	{
		if (owner is DependencyObject @do &&
				GetProperty(owner.GetType(), $"{property}Property")
				?.GetValue(null, null) is DependencyProperty dp &&
				@do.GetValue(dp) is T t)
		{
			value = t;
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
	/// <remarks>Searches upward from <paramref name="reference"/>, not down from the root.</remarks>
	public static T? FindFirstAncestor<T>(this DependencyObject? reference) => EnumerateVisualTreeAncestors(reference)
		.OfType<T>()
		.FirstOrDefault();

	/// <summary>
	/// Returns the first ancestor of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of ancestor to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <remarks>Searches upward from <paramref name="reference"/>, not down from the root.</remarks>
	/// <exception cref="InvalidOperationException">If no such ancestor exists.</exception>
	public static T FindFirstAncestorOrThrow<T>(this DependencyObject? reference) where T : class => EnumerateVisualTreeAncestors(reference)
		.OfType<T>()
		.FirstOrDefault() ??
		throw new InvalidOperationException($"Unable to find element: {typeof(T).Name}");

	/// <summary>
	/// Returns the first ancestor of a specified type that satisfies the <paramref name="predicate"/>.
	/// </summary>
	/// <typeparam name="T">The type of ancestor to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="predicate">A function to test each node for a condition.</param>
	/// <remarks>Searches upward from <paramref name="reference"/>, not down from the root.</remarks>
	public static T? FindFirstAncestor<T>(this DependencyObject? reference, Func<T, bool> predicate) => EnumerateVisualTreeAncestors(reference)
		.OfType<T>()
		.FirstOrDefault(predicate);

	/// <summary>
	/// Returns the first descendant of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	public static T? FindFirstDescendant<T>(this DependencyObject? reference) => EnumerateDescendants(reference)
		.OfType<T>()
		.FirstOrDefault();

	/// <summary>
	/// Returns the first descendant of a specified type that satisfies the <paramref name="predicate"/>.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="predicate">A function to test each node for a condition.</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	public static T? FindFirstDescendant<T>(this DependencyObject? reference, Func<T, bool> predicate) => EnumerateDescendants(reference)
		.OfType<T>()
		.FirstOrDefault(predicate);

	/// <summary>
	/// Returns the first descendant of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="name">x:Name of the node</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	public static T? FindFirstDescendant<T>(this DependencyObject? reference, string name) where T : FrameworkElement => EnumerateDescendants(reference)
		.OfType<T>()
		.FirstOrDefault(x => x.Name == name);

	/// <summary>
	/// Returns the first descendant of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	/// <exception cref="InvalidOperationException">If no such descendant exists.</exception>
	public static T FindFirstDescendantOrThrow<T>(this DependencyObject? reference) where T : class => EnumerateDescendants(reference)
		.OfType<T>()
		.FirstOrDefault() ??
		throw new InvalidOperationException($"Unable to find element: {typeof(T).Name}");

	/// <summary>
	/// Returns the first descendant of a specified type.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="name">x:Name of the node</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	/// <exception cref="InvalidOperationException">If no such descendant exists.</exception>
	public static T FindFirstDescendantOrThrow<T>(this DependencyObject? reference, string name) where T : FrameworkElement => EnumerateDescendants(reference)
		.OfType<T>()
		.FirstOrDefault(x => x.Name == name) ??
		throw new InvalidOperationException($"Unable to find element: {typeof(T).Name}#{name}");

	/// <summary>
	/// Returns the first descendant of a specified type that satisfies the <paramref name="predicate"/> whose ancestors (up to <paramref name="reference"/>) and itself satisfy the <paramref name="hierarchyPredicate"/>.
	/// </summary>
	/// <typeparam name="T">The type of descendant to find.</typeparam>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="hierarchyPredicate">A function to test each ancestor for a condition.</param>
	/// <param name="predicate">A function to test each descendant for a condition.</param>
	/// <remarks>The nodes are visited in depth-first order.</remarks>
	public static T? FindFirstDescendant<T>(this DependencyObject? reference, Func<DependencyObject, bool> hierarchyPredicate, Func<T, bool> predicate) => EnumerateDescendants(reference, hierarchyPredicate)
		.OfType<T>()
		.FirstOrDefault(predicate);

	/// <summary>
	/// Returns all the visual descendants of a node, in depth-first order.
	/// </summary>
	/// <param name="reference">Any node of the visual tree</param>
	/// <seealso cref="EnumerateRawDescendants(UIElement, int?)"/>
	public static IEnumerable<DependencyObject> EnumerateDescendants(this DependencyObject? reference) => EnumerateDescendants(reference, x => true);

	/// <summary>
	/// Returns the visual descendants of a node that satisfy the <paramref name="hierarchyPredicate"/>, in depth-first order.
	/// </summary>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="hierarchyPredicate">A function to test each descendant; one that fails it is skipped together with its own descendants.</param>
	/// <seealso cref="EnumerateRawDescendants(UIElement, int?)"/>
	public static IEnumerable<DependencyObject> EnumerateDescendants(this DependencyObject? reference, Func<DependencyObject, bool> hierarchyPredicate)
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

	/// <summary>
	/// Returns the descendants of an element in depth-first order.
	/// </summary>
	/// <param name="view">The element to start from; it is not returned.</param>
	/// <param name="maxDepth">How deep to go: 0 returns the direct children only, n returns n + 1 levels; <see langword="null"/> for no limit.</param>
	/// <remarks>
	/// Unlike <see cref="EnumerateDescendants(DependencyObject)"/>, this walks the element's raw child list,
	/// so it also returns the placeholders that <c>x:Load</c> and <c>x:DeferLoadStrategy</c> leave for elements not loaded yet (<c>ElementStub</c>).
	/// This only makes a difference on Uno Platform targets: on WinAppSDK both methods walk <see cref="VisualTreeHelper"/> and, without <paramref name="maxDepth"/>, return the same elements in the same order.
	/// </remarks>
	public static IEnumerable<UIElement> EnumerateRawDescendants(this UIElement view, int? maxDepth = null)
	{
#if HAS_UNO
		return VisualTreeHelper.EnumerateRawDescendants(view, maxDepth);
#else
		return Walk(view, maxDepth);

		static IEnumerable<UIElement> Walk(UIElement view, int? maxDepth)
		{
			foreach (var child in view.EnumerateChildren().OfType<UIElement>())
			{
				yield return child;

				if (maxDepth is null or > 0)
				{
					foreach (var descendant in Walk(child, maxDepth - 1))
					{
						yield return descendant;
					}
				}
			}
		}
#endif
	}

	/// <summary>
	/// Returns the ancestors of a node, from its parent up to the root, following <see cref="VisualTreeHelper.GetParent"/>.
	/// </summary>
	/// <param name="reference">Any node of the visual tree; it is not returned.</param>
	public static IEnumerable<DependencyObject> EnumerateVisualTreeAncestors(this DependencyObject? reference)
	{
		if (reference is null) yield break;
		while (VisualTreeHelper.GetParent(reference) is { } parent)
		{
			yield return reference = parent;
		}
	}

	internal static IEnumerable<DependencyObject> EnumerateChildren(this DependencyObject? reference)
	{
		if (reference is null)
		{
			yield break;
		}

		var count = VisualTreeHelper.GetChildrenCount(reference);
		for (var i = 0; i < count; i++)
		{
			yield return VisualTreeHelper.GetChild(reference, i);
		}
	}
}

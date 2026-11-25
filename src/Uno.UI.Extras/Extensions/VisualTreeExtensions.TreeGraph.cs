#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

using static System.FormattableString;
using static Uno.UI.Extensions.PrettyPrint;

namespace Uno.UI.Extensions;

public static partial class VisualTreeExtensions
{
	/// <summary>
	/// Produces a text representation of the visual tree below <paramref name="reference"/>: one node per line, indented by depth,
	/// with its type, x:Name and layout details (<see cref="DescribeLayout(object)"/>).
	/// </summary>
	/// <param name="reference">Any node of the visual tree</param>
	public static string TreeGraph(this DependencyObject reference) => TreeGraph(reference, DescribeLayout);

	/// <summary>
	/// Produces a text representation of the visual tree below <paramref name="reference"/>: one node per line, indented by depth,
	/// with its type, x:Name and the details <paramref name="describeProperties"/> returns for it.
	/// </summary>
	/// <param name="reference">Any node of the visual tree</param>
	/// <param name="describeProperties">A function returning the details to show for a node, e.g. <c>Margin=4</c>; see <see cref="DescribeLayout(object)"/> and <see cref="DescribeVisual(object)"/>.</param>
	public static string TreeGraph(this DependencyObject reference, Func<object, IEnumerable<string>> describeProperties) =>
		TreeGraph(reference, x => DescribeVTNode(x, describeProperties));

	/// <summary>
	/// Produces a text representation of the tree below <paramref name="reference"/>: one node per line, indented by depth,
	/// with its type, x:Name and the details <paramref name="describeProperties"/> returns for it.
	/// </summary>
	/// <param name="reference">The root node</param>
	/// <param name="describeProperties">A function returning the details to show for a node, e.g. <c>Margin=4</c>; see <see cref="DescribeLayout(object)"/> and <see cref="DescribeVisual(object)"/>.</param>
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

	/// <summary>
	/// Describes a node's layout, for <c>TreeGraph</c>'s <c>describeProperties</c>: its grid cell, position in its parent and actual size;
	/// its size constraints, alignment, margin and padding when set; its scroll state; its opacity and visibility when not the default.
	/// </summary>
	/// <remarks>This is what <see cref="TreeGraph(DependencyObject)"/> shows by default.</remarks>
	/// <seealso cref="DescribeVisual(object)"/>
	public static IEnumerable<string> DescribeLayout(object x)
	{
		if (x is FrameworkElement { Parent: Grid } cell)
		{
			int c = Grid.GetColumn(cell), cspan = Grid.GetColumnSpan(cell),
				r = Grid.GetRow(cell), rspan = Grid.GetRowSpan(cell);

			if (r != 0 || c != 0 || rspan > 1 || cspan > 1)
			{
				yield return $"R{FormatRange(r, rspan)}C{FormatRange(c, cspan)}";
			}

			static string FormatRange(int x, int span) => span > 1 ? $"{x}-{x + span - 1}" : $"{x}";
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
#if false // binding: templated-parent and data-context
		if (x is UIElement uie)
		{
#if HAS_UNO
			yield return $"TP={FormatType(uie.GetTemplatedParent())}";
#endif
			yield return $"DC={FormatType(uie.DataContext)}";
		}
#endif
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
			if (fe.MinWidth != 0 || !double.IsNaN(fe.Width) || !double.IsPositiveInfinity(fe.MaxWidth) ||
				fe.MinHeight != 0 || !double.IsNaN(fe.Height) || !double.IsPositiveInfinity(fe.MaxHeight))
			{
				yield return Invariant($"Constraints=[{fe.MinWidth:0.#},{fe.Width:0.#},{fe.MaxWidth:0.#}]x[{fe.MinHeight:0.#},{fe.Height:0.#},{fe.MaxHeight:0.#}]");
			}
			if (fe.HorizontalAlignment != HorizontalAlignment.Stretch || fe.VerticalAlignment != VerticalAlignment.Stretch)
			{
				yield return $"HV={fe.HorizontalAlignment}/{fe.VerticalAlignment}";
			}
		}
		if (x is ScrollViewer sv)
		{
			yield return $"Offset={FormatPoint(sv.HorizontalOffset, sv.VerticalOffset)}";
			yield return $"Viewport={FormatSize(sv.ViewportWidth, sv.ViewportHeight)}";
			yield return $"Extent={FormatSize(sv.ExtentWidth, sv.ExtentHeight)}";
		}
		//if (TryGetDpValue<CornerRadius>(x, "CornerRadius", out var cr)) yield return $"CornerRadius={FormatCornerRadius(cr)}";
		foreach (var detail in DescribeSpacing(x))
		{
			yield return detail;
		}
		foreach (var detail in DescribeOpacityAndVisibility(x))
		{
			yield return detail;
		}
	}

	/// <summary>
	/// Describes a node's appearance, for <c>TreeGraph</c>'s <c>describeProperties</c>: its actual size; its border, background, margin and padding when set;
	/// its opacity and visibility when not the default; and for a control, the current state of each of its visual state groups.
	/// </summary>
	/// <seealso cref="DescribeLayout(object)"/>
	public static IEnumerable<string> DescribeVisual(object x)
	{
		if (x is FrameworkElement fe) yield return $"Actual={FormatSize(fe.ActualWidth, fe.ActualHeight)}";

		if (TryGetDpValue<Thickness>(x, "BorderThickness", out var borderThickness) && !borderThickness.Equals(default(Thickness))) yield return $"BorderThickness={FormatThickness(borderThickness)}";
		if (TryGetDpValue<Brush>(x, "BorderBrush", out var borderBrush)) yield return $"BorderBrush={FormatBrush(borderBrush)}";
		if (TryGetDpValue<Brush>(x, "Background", out var background)) yield return $"Background={FormatBrush(background)}";

		foreach (var detail in DescribeSpacing(x)) yield return detail;
		foreach (var detail in DescribeOpacityAndVisibility(x)) yield return detail;

		if (x is Control control && GetCurrentVisualStates(control) is { } states) yield return $"VisualStates={states}";
	}

	private static IEnumerable<string> DescribeSpacing(object x)
	{
		if (TryGetDpValue<Thickness>(x, "Margin", out var margin) && !margin.Equals(default(Thickness)))
		{
			yield return $"Margin={FormatThickness(margin)}";
		}
		if (TryGetDpValue<Thickness>(x, "Padding", out var padding) && !padding.Equals(default(Thickness)))
		{
			yield return $"Padding={FormatThickness(padding)}";
		}
	}

	private static IEnumerable<string> DescribeOpacityAndVisibility(object x)
	{
		if (TryGetDpValue<double>(x, "Opacity", out var opacity) && opacity != 1)
		{
			yield return Invariant($"Opacity={opacity}");
		}
		if (TryGetDpValue<Visibility>(x, "Visibility", out var visibility) && visibility == Visibility.Collapsed)
		{
			yield return "Collapsed";
		}
	}

	// "CommonStates=PointerOver|FocusStates=Unfocused", read from the visual state groups of the control's template root.
	private static string? GetCurrentVisualStates(Control control)
	{
		return VisualTreeHelper.GetChildrenCount(control) > 0
			&& VisualTreeHelper.GetChild(control, 0) is FrameworkElement root
			&& VisualStateManager.GetVisualStateGroups(root) is { Count: > 0 } groups
			? string.Join("|", groups.Select(group => $"{group.Name}={group.CurrentState?.Name}"))
			: null;
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
}

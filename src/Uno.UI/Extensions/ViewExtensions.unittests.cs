using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.UI.Xaml;

namespace Uno.UI
{
	public static partial class ViewExtensions
	{
		/// <summary>
		/// Gets an enumerator containing all the children of a View group
		/// </summary>
		/// <param name="group"></param>
		/// <returns></returns>
		public static List<UIElement> GetChildren(this UIElement group) => (group as FrameworkElement)?._children ?? new List<UIElement>();

		internal static TResult FindLastChild<TParam, TResult>(this UIElement group, TParam param, Func<UIElement, TParam, TResult> selector, out bool hasAnyChildren)
			where TResult : class
		{
			hasAnyChildren = false;
			var children = group.GetChildren();
			for (int i = children.Count - 1; i >= 0; i--)
			{
				hasAnyChildren = true;
				var result = selector(children[i], param);
				if (result is not null)
				{
					return result;
				}
			}

			return null;
		}

		public static FrameworkElement GetTopLevelParent(this UIElement view) => throw new NotImplementedException();

		public static T FindFirstChild<T>(this FrameworkElement root) where T : FrameworkElement
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


		/// <summary>
		/// Displays the visual tree in the vicinity of <paramref name="element"/> for diagnostic purposes.
		/// </summary>
		/// <param name="element">The view to display tree for.</param>
		/// <param name="fromHeight">How many levels above <paramref name="element"/> should be included in the displayed subtree.</param>
		/// <returns>A formatted string representing the visual tree around <paramref name="element"/>.</returns>
		public static string ShowLocalVisualTree(this UIElement element, int fromHeight = 1000)
		{
			var root = element as FrameworkElement;
			for (int i = 0; i < fromHeight; i++)
			{
				if (root.Parent is FrameworkElement parent)
				{
					root = parent;
				}
				else
				{
					break;
				}
			}

			return ShowDescendants(root, viewOfInterest: element);
		}

		/// <summary>
		/// Displays all the visual descendants of <paramref name="element"/> for diagnostic purposes. 
		/// </summary>
		public static string ShowDescendants(this UIElement element, StringBuilder sb = null, string spacing = "", UIElement viewOfInterest = null)
		{
			sb = sb ?? new StringBuilder();

			Inner(element, spacing);
			return sb.ToString();

			void Inner(UIElement elem, string s)
			{
				AppendView(elem, s);
				s += "  ";
				foreach (var child in elem.GetChildren())
				{
					Inner(child, s);
				}
			}

			void AppendView(UIElement innerView, string s)
			{
				var name = (innerView as FrameworkElement)?.Name;
				var namePart = string.IsNullOrEmpty(name) ? "" : $"-'{name}'";

				var fe = innerView as FrameworkElement;
				var u = innerView as UIElement;

				sb
					.Append(s)
					.Append(innerView == viewOfInterest ? "*>" : ">")
					.Append(innerView.ToString() + namePart)
					.Append($"-({fe.ActualWidth}x{fe.ActualHeight})@({fe.LayoutSlot.Left},{fe.LayoutSlot.Top})")
					.Append($"  {innerView.Visibility}")
					.Append(fe != null ? $" HA={fe.HorizontalAlignment},VA={fe.VerticalAlignment}" : "")
					.Append(fe != null && fe.Margin != default ? $" Margin={fe.Margin}" : "")
					.Append(fe != null && fe.TryGetBorderThickness(out var b) && b != default ? $" Border={b}" : "")
					.Append(fe != null && fe.TryGetPadding(out var p) && p != default ? $" Padding={p}" : "")
					.Append(u != null ? $" DesiredSize={u.DesiredSize}" : "")
					.Append(u != null && u.NeedsClipToSlot ? "CLIPPED_TO_SLOT" : "")
					.Append(u != null && u.RenderTransform != null ? $"RENDER_TRANSFORM({u.RenderTransform.MatrixCore})" : "")
					.Append(u?.Clip != null ? $" Clip={u.Clip.Rect}" : "")
					.AppendLine();
			}
		}
	}
}

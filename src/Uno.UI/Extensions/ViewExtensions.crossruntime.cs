#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.Extensions;
using Uno.UI.Extensions;

namespace Uno.UI
{
	public static partial class ViewExtensions
	{
		internal static TResult? FindLastChild<TParam, TResult>(this UIElement group, TParam param, Func<UIElement, TParam, TResult?> selector, out bool hasAnyChildren)
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

#if !__NETSTD_REFERENCE__
		/// <summary>
		/// Find the first child of a specific type.
		/// </summary>
		/// <typeparam name="T">Expected type of the searched child</typeparam>
		/// <param name="view"></param>
		/// <param name="childLevelLimit">Defines the max depth, null if not limit (Should never be used)</param>
		/// <param name="includeCurrent">Indicates if the current view should also be tested or not.</param>
		/// <returns></returns>
		internal static T? FindFirstChild<T>(this UIElement view, int? childLevelLimit = null, bool includeCurrent = true)
			where T : UIElement
		{
			return view.FindFirstChild<T>(null, childLevelLimit, includeCurrent);
		}

		/// <summary>
		/// Find the first child of a specific type.
		/// </summary>
		/// <typeparam name="T">Expected type of the searched child</typeparam>
		/// <param name="view"></param>
		/// <param name="selector">Additional selector for the child</param>
		/// <param name="childLevelLimit">Defines the max depth, null if not limit (Should never be used)</param>
		/// <param name="includeCurrent">Indicates if the current view should also be tested or not.</param>
		/// <returns></returns>
		internal static T? FindFirstChild<T>(this UIElement view, Func<T, bool>? selector, int? childLevelLimit = null, bool includeCurrent = true)
			where T : UIElement
		{
			Func<UIElement, bool> childSelector;
			if (selector == null)
			{
				childSelector = child => child is T;
			}
			else
			{
				childSelector = child => child is T t && selector(t);
			}

			if (includeCurrent && childSelector(view))
			{
				return view as T;
			}

			var maxDepth = childLevelLimit ?? int.MaxValue;

			return (T?)view.EnumerateAllChildren(childSelector, maxDepth).FirstOrDefault();
		}

		internal static string ShowDescendants(this UIElement view, StringBuilder? sb = null, string spacing = "", UIElement? viewOfInterest = null)
		{
			sb = sb ?? new StringBuilder();
			AppendView(view);
			spacing += "  ";
			foreach (var child in view._children)
			{
				ShowDescendants(child, sb, spacing, viewOfInterest);
			}

			return sb.ToString();

			StringBuilder AppendView(UIElement innerView)
			{
				var name = (innerView as IFrameworkElement)?.Name;
				var namePart = string.IsNullOrEmpty(name) ? "" : $"-'{name}'";

				var uiElement = innerView as UIElement;
				var desiredSize = uiElement?.DesiredSize.ToString("F1") ?? "<native/unk>";
				var fe = innerView as IFrameworkElement;
				var layoutSlot = innerView.LayoutSlot;

				return sb
					.Append(spacing)
					.Append(innerView == viewOfInterest ? "*>" : ">")
					.Append(innerView + namePart)
					.Append($"-({layoutSlot.Width:F1}x{layoutSlot.Height:F1})@({layoutSlot.X:F1},{layoutSlot.Y:F1})")
					.Append($" d:{desiredSize}")
					.Append(fe != null ? $" HA={fe.HorizontalAlignment},VA={fe.VerticalAlignment}" : "")
					.Append(fe != null && fe.Margin != default ? $" Margin={fe.Margin}" : "")
					.Append(fe != null && fe.TryGetBorderThickness(out var b) && b != default ? $" Border={b}" : "")
					.Append(fe != null && fe.TryGetPadding(out var p) && p != default ? $" Padding={p}" : "")
					.Append(fe != null && fe.Visibility != Visibility.Visible ? "Collapsed " : "")
					.Append(fe != null && fe.Opacity != 1 ? $"Opacity={fe.Opacity} " : "")
					.Append(uiElement?.Clip != null ? $" Clip={uiElement.Clip.Rect}" : "")
					.Append(uiElement?.NeedsClipToSlot ?? false ? " CLIPPED_TO_SLOT" : "")
					.Append(uiElement?.GetElementSpecificDetails())
					.Append(uiElement?.GetElementGridOrCanvasDetails())
					.Append(uiElement?.RenderTransform.GetTransformDetails())
					.Append(uiElement?.GetLayoutDetails())
					.AppendLine();
			}
		}


		internal static string ShowLocalVisualTree(this UIElement element, int fromHeight = 0)
		{
			var root = element;

			for (int i = 0; i < fromHeight; i++)
			{
				var parent = root.GetParent() as UIElement;
				if (parent != null)
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
#endif
	}
}

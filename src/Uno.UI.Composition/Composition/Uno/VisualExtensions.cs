#nullable enable

using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Microsoft.UI.Composition;

internal static class VisualExtensions
{
	internal static Vector3 GetTotalOffset(this Visual visual)
	{
		if (visual.IsTranslationEnabled && visual.Properties.TryGetVector3("Translation", out var translation) == CompositionGetValueStatus.Succeeded)
		{
			return visual.Offset + translation;
		}

		return visual.Offset;
	}

#if DEBUG
	internal static string ShowLocalCompositionTree(this Visual visual, int fromHeight = 1000)
	{
		var root = visual;

		for (int i = 0; i < fromHeight; i++)
		{
			var parent = root.Parent;
			if (parent != null)
			{
				root = parent;
			}
			else
			{
				break;
			}
		}

		return ShowDescendants(root, viewOfInterest: visual);
	}

	internal static string ShowDescendants(this Visual view, StringBuilder? sb = null, string spacing = "", Visual? viewOfInterest = null)
	{
		sb = sb ?? new StringBuilder();
		AppendView(view);
		spacing += "  ";

		foreach (var child in (view as ContainerVisual)?.Children.ToArray() ?? Array.Empty<ContainerVisual>())
		{
			ShowDescendants(child, sb, spacing, viewOfInterest);
		}

		return sb.ToString();

		static string GetViewBox(Visual v)
		{
			if (v is ShapeVisual sv)
			{
				return sv.ViewBox?.GetRect().ToString() ?? "null";
			}

			return "N/A";
		}

		static string GetClip(Visual v)
		{
			var clip = v.Clip;
			if (clip is null)
			{
				return "null";
			}
			else if (clip is RectangleClip clipRect)
			{
				return $"Left: {clipRect.Left}, Right: {clipRect.Right}, Top: {clipRect.Top}, Bottom: {clipRect.Bottom}";
			}

			return clip.GetType().ToString();
		}

		StringBuilder AppendView(Visual innerView)
		{
			var name = innerView.Comment;
			var namePart = string.IsNullOrEmpty(name) ? "" : $"-'{name}'";

			var size = innerView.Size;
			var offset = innerView.GetTotalOffset();
			var opacity = innerView.Opacity;
			var viewBox = GetViewBox(innerView);
			var clip = GetClip(innerView);
			return sb
				.Append(spacing)
				.Append(innerView == viewOfInterest ? "*>" : ">")
				.Append(innerView + namePart)
				.Append($" size:{size}")
				.Append($" offset:{offset}")
				.Append($" opacity:{opacity}")
				.Append($" viewBox:{viewBox}")
				.Append($" clip:{clip}")
				.AppendLine();
		}
	}
#endif
}

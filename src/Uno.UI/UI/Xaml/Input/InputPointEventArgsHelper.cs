#nullable enable

using System.Numerics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Extensions;
using Windows.Foundation;

namespace Uno.UI.Xaml.Input;

internal static class InputPointEventArgsHelper
{
	/// <summary>
	/// Converts a point in window coordinates to the coordinates of <paramref name="relativeTo"/>, with the rules of
	/// WinUI's CInputPointEventArgs::ConvertGlobalPointToRelativePoint (InputPointEventArgs.cpp).
	/// </summary>
	internal static Point ConvertGlobalPointToRelativePoint(Point globalPoint, UIElement? relativeTo)
	{
		// A popup has no visuals of its own: out of the live tree, its child stands for it.
		if (relativeTo is Popup { IsInLiveTree: false } popup)
		{
			relativeTo = popup.Child;
		}

		if (relativeTo is null)
		{
			return globalPoint;
		}

		// An element out of the live tree gets (0, 0) rather than an error.
		if (!relativeTo.IsInLiveTree)
		{
			return default;
		}

		return Matrix3x2.Invert(UIElement.GetTransform(relativeTo, null), out var inverse)
			? inverse.Transform(globalPoint)
			: default;
	}
}

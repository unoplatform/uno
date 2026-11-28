#nullable enable

using System;

namespace Uno.UI.Runtime.Win32;

// Standalone so the pure math can be linked into Uno.UI.UnitTests without the provider's HWND/P-Invoke dependencies.
internal static class Win32UiaBoundsMath
{
	// Logical bounds come from float transforms (853.6 DIP is 853.59997), so the scaled edges land just below
	// a whole pixel and clients truncating to an integer RECT report it one pixel off. Snap each edge instead.
	internal static (double Left, double Top, double Width, double Height) ToPhysicalScreenRect(
		(double X, double Y, double Width, double Height) logicalRect,
		double scale,
		(int X, int Y) clientOrigin)
	{
		var left = Math.Round(clientOrigin.X + logicalRect.X * scale, MidpointRounding.AwayFromZero);
		var top = Math.Round(clientOrigin.Y + logicalRect.Y * scale, MidpointRounding.AwayFromZero);
		var right = Math.Round(clientOrigin.X + (logicalRect.X + logicalRect.Width) * scale, MidpointRounding.AwayFromZero);
		var bottom = Math.Round(clientOrigin.Y + (logicalRect.Y + logicalRect.Height) * scale, MidpointRounding.AwayFromZero);

		return (left, top, right - left, bottom - top);
	}
}

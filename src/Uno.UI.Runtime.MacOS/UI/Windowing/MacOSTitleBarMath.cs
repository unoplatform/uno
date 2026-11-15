using System;
using Microsoft.UI.Input;

namespace Uno.UI.Runtime.MacOS;

// keep in sync with NonClientPointerAction in UNOWindow.h
internal enum MacOSNonClientPointerAction
{
	Client = 0,
	Drag = 1,
}

/// <summary>
/// The pure part of the macOS title bar support, split out so it is unit-testable without a native window.
/// </summary>
internal static class MacOSTitleBarMath
{
	/// <summary>
	/// Computes the AppWindowTitleBar insets reserved by the traffic lights.
	/// </summary>
	/// <param name="buttonsMinX">Left edge of the traffic lights, in points from the window's left edge.</param>
	/// <param name="buttonsMaxX">Right edge of the traffic lights, in points from the window's left edge.</param>
	/// <param name="windowWidth">Window width, in points.</param>
	/// <param name="scale">The window's rasterization scale.</param>
	/// <returns>The insets in physical pixels, the unit WinUI reports them in.</returns>
	public static (int Left, int Right) GetInsets(double buttonsMinX, double buttonsMaxX, double windowWidth, double scale)
	{
		if (buttonsMaxX <= buttonsMinX)
		{
			return (0, 0);
		}

		// The reserved area keeps the same spacing after the buttons as between the window edge and the buttons.
		// Right-to-left system layouts put the traffic lights on the right.
		return buttonsMinX + buttonsMaxX <= windowWidth
			? (ToPhysical(buttonsMinX + buttonsMaxX, scale), 0)
			: (0, ToPhysical(2 * windowWidth - buttonsMinX - buttonsMaxX, scale));
	}

	/// <summary>
	/// Maps the non-client region under the pointer to what macOS does with a left-button press there.
	/// </summary>
	/// <remarks>
	/// There is no system menu on macOS, so the Icon region drags like the rest of the title bar.
	/// The caption-button and border kinds have no macOS equivalent over the content and stay client.
	/// </remarks>
	public static MacOSNonClientPointerAction GetPointerAction(NonClientRegionKind? region)
		=> region is NonClientRegionKind.Caption or NonClientRegionKind.Icon
			? MacOSNonClientPointerAction.Drag
			: MacOSNonClientPointerAction.Client;

	private static int ToPhysical(double points, double scale) => (int)Math.Ceiling(points * scale);
}

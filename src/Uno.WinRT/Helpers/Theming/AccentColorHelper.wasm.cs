#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using Windows.UI;

using NativeMethods = __Uno.Helpers.Theming.AccentColorHelper.NativeMethods;

namespace Uno.Helpers.Theming;

internal static partial class AccentColorHelper
{
	private static partial AccentColorPalette? GetPlatformAccentColorPalette()
	{
		var hex = NativeMethods.GetAccentColor();
		if (hex is null || hex.Length != 7 || hex[0] != '#')
		{
			return null;
		}

		var r = Convert.ToByte(hex.Substring(1, 2), 16);
		var g = Convert.ToByte(hex.Substring(3, 2), 16);
		var b = Convert.ToByte(hex.Substring(5, 2), 16);
		var accent = Color.FromArgb(0xFF, r, g, b);
		return AccentColorPalette.FromAccentColor(accent);
	}

	static partial void ObserveAccentColorChanges()
	{
		NativeMethods.ObserveAccentColor();
	}

	[JSExport]
	public static int DispatchAccentColorChange()
	{
		RefreshAccentColor();
		return 0;
	}
}

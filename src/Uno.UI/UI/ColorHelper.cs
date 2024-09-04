using System.ComponentModel;
using WindowsColor = Windows/*Intentional space for WinUI upgrade tool*/.UI.Color;
using WindowsColorHelper = Windows.UI.ColorHelper;

namespace Microsoft.UI;

public static partial class ColorHelper
{
	/// <summary>
	/// Retrieves the display name of the specified color.
	/// </summary>
	/// <param name="color">The color to get the name for.</param>
	/// <returns>The localized display name of the color.</returns>
	public static string ToDisplayName(WindowsColor color) => WindowsColorHelper.ToDisplayName(color);

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static WindowsColor FromARGB(byte a, byte r, byte g, byte b)
		=> WindowsColor.FromArgb(a, r, g, b);

	/// <summary>
	/// Generates a Color structure, based on discrete Byte values for ARGB components. C# and Microsoft Visual Basic code should use Color.FromArgb instead.
	/// </summary>
	/// <param name="a">The A (transparency) component of the desired color. Range is 0-255.</param>
	/// <param name="r">The R component of the desired color. Range is 0-255.</param>
	/// <param name="g">The G component of the desired color. Range is 0-255.</param>
	/// <param name="b">The B component of the desired color. Range is 0-255.</param>
	/// <returns>The generated Color value.</returns>
	public static WindowsColor FromArgb(byte a, byte r, byte g, byte b)
		=> WindowsColor.FromArgb(a, r, g, b);
}

#nullable enable

using Silk.NET.OpenGL;

namespace Uno.UI.Runtime.Skia.GTK.Extensions;

internal static class TypeConversionExtensions
{
	public static Pango.Stretch ToGtkFontStretch(this Windows.UI.Text.FontStretch fontStretch) =>
		fontStretch switch
		{
			Windows.UI.Text.FontStretch.UltraCondensed => Pango.Stretch.UltraCondensed,
			Windows.UI.Text.FontStretch.ExtraCondensed => Pango.Stretch.ExtraCondensed,
			Windows.UI.Text.FontStretch.Condensed => Pango.Stretch.Condensed,
			Windows.UI.Text.FontStretch.SemiCondensed => Pango.Stretch.SemiCondensed,
			Windows.UI.Text.FontStretch.SemiExpanded => Pango.Stretch.SemiExpanded,
			Windows.UI.Text.FontStretch.Expanded => Pango.Stretch.Expanded,
			Windows.UI.Text.FontStretch.ExtraExpanded => Pango.Stretch.ExtraExpanded,
			Windows.UI.Text.FontStretch.UltraExpanded => Pango.Stretch.UltraExpanded,
			_ => Pango.Stretch.Normal
		};

	public static Pango.Style ToGtkFontStyle(this Windows.UI.Text.FontStyle fontStyle) =>
		fontStyle switch
		{
			Windows.UI.Text.FontStyle.Normal => Pango.Style.Normal,
			Windows.UI.Text.FontStyle.Italic => Pango.Style.Italic,
			Windows.UI.Text.FontStyle.Oblique => Pango.Style.Oblique,
			_ => Pango.Style.Normal
		};
}

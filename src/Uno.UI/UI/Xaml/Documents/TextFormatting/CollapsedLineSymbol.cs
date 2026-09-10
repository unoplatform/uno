#nullable enable

namespace Microsoft.UI.Xaml.Documents.TextFormatting
{
	/// <summary>
	/// The shaped ellipsis painted at the end of a line collapsed by text trimming.
	/// </summary>
	/// <param name="Font">The font the symbol was shaped with.</param>
	/// <param name="Glyphs">The shaped glyph ids.</param>
	/// <param name="Advances">The per-glyph advances, in pixels.</param>
	/// <param name="Width">The total width of the symbol, in pixels.</param>
	internal record CollapsedLineSymbol(FontDetails Font, ushort[] Glyphs, float[] Advances, float Width);
}

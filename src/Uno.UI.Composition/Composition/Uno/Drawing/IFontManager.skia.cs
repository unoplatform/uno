#nullable enable

using Windows.UI.Text;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Backend font resolver: turns a family/style request (or raw font bytes, or a codepoint) into an
/// <see cref="IFont"/> handle. This is the one Skia-specific concern left in the text layer — the Skia
/// implementation uses <c>SKFontManager</c>/<c>SKTypeface</c>; the managed implementation enumerates system
/// fonts. Obtained from <see cref="IDrawingBackend.FontManager"/>. Byte loading for application/URI fonts stays
/// with the caller (it needs the app's storage APIs); this only turns bytes into a handle.
/// </summary>
public interface IFontManager
{
	/// <summary>Builds a font from raw sfnt bytes (an embedded/URI font), selecting <paramref name="familyNameHint"/> within a collection and positioning any variable axes for the requested style. Returns <c>null</c> if the bytes aren't a usable font.</summary>
	IFont? CreateFont(byte[] data, string? familyNameHint, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize);

	/// <summary>Resolves an installed font family, or <c>null</c> if the family is unknown (caller falls back to the default).</summary>
	IFont? MatchFamily(string familyName, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize);

	/// <summary>Finds a font that can render <paramref name="codepoint"/> when the requested family can't, or <c>null</c> if none is available.</summary>
	IFont? MatchCharacter(int codepoint, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize);

	/// <summary>Returns a guaranteed-usable default font for the requested style.</summary>
	IFont GetDefaultFont(FontWeight weight, FontStretch stretch, FontStyle style, float fontSize);
}

#nullable enable
using System.IO;
using System.Threading.Tasks;
using Windows.UI.Text;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Resolves a fallback font for codepoints the requested family can't render — a family name for a codepoint, then a
/// stream of that family's bytes. Internal to the default providers' codepoint-fallback path; the public seam is
/// <see cref="IFontProvider"/> (implement <see cref="IFontProvider.MatchCharacterAsync"/> to supply custom fallback).
/// </summary>
internal interface IFontFallbackService
{
	/// <summary>Family name of a font that can render <paramref name="codepoint"/>, or <c>null</c> if none.</summary>
	Task<string?> GetFontFamilyForCodepoint(int codepoint);

	/// <summary>A fresh stream of font bytes for the family, or <c>null</c> if unknown. Caller disposes it.</summary>
	Task<Stream?> GetFontStreamForFontFamily(string fontFamily, FontWeight weight, FontStretch stretch, FontStyle style);
}

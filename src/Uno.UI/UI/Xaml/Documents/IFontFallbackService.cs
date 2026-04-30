#nullable enable
using System.IO;
using System.Threading.Tasks;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

/// <summary>
/// Resolves a fallback font for codepoints that the requested font family cannot render.
/// </summary>
/// <remarks>
/// Override the default service by setting <see cref="Uno.UI.FeatureConfiguration.Font.FallbackService"/>.
/// For most cases prefer reusing <see cref="CoverageTableFontFallbackService"/> with a
/// custom coverage table and/or font stream provider rather than implementing this interface from scratch.
/// </remarks>
public interface IFontFallbackService
{
	/// <summary>
	/// Returns the family name of a font that can render <paramref name="codepoint"/>, or <c>null</c>
	/// if no fallback is available. The returned string is later passed back to
	/// <see cref="GetFontStreamForFontFamily"/>.
	/// </summary>
	Task<string?> GetFontFamilyForCodepoint(int codepoint);

	/// <summary>
	/// Returns a fresh stream of font bytes for the given family at the requested style, or
	/// <c>null</c> if the family is unknown to this service. The caller is expected to dispose the
	/// returned stream after consuming it.
	/// </summary>
	Task<Stream?> GetFontStreamForFontFamily(string fontFamily, FontWeight weight, FontStretch stretch, FontStyle style);
}

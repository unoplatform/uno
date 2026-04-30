#nullable enable
using System.Threading.Tasks;
using Windows.UI.Text;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

/// <summary>
/// Resolves a fallback font (and its <see cref="SKTypeface"/>) for codepoints that the
/// requested font family cannot render. Implementations are responsible for caching
/// fetched typefaces and may use any source (network, embedded resource, OS lookup, etc.).
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
	/// <see cref="GetTypefaceForFontFamily"/>.
	/// </summary>
	Task<string?> GetFontFamilyForCodepoint(int codepoint);

	/// <summary>
	/// Returns a typeface for the given family at the requested style, or <c>null</c> if the family
	/// is unknown to this service.
	/// </summary>
	Task<SKTypeface?> GetTypefaceForFontFamily(string fontFamily, FontWeight weight, FontStretch stretch, FontStyle style);
}

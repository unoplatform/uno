#nullable enable
using System.Threading.Tasks;
using Windows.UI.Text;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

internal interface IFontFallbackService
{
	Task<string?> GetFontNameForCodepoint(int codepoint);
	Task<SKTypeface?> GetTypefaceForFontName(string fontName, FontWeight weight, FontStretch stretch, FontStyle style);
}

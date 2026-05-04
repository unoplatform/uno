#nullable enable
using System.IO;
using System.Threading.Tasks;
using Windows.UI.Text;

namespace Microsoft.UI.Xaml.Documents.TextFormatting;

/// <summary>
/// An <see cref="IFontFallbackService"/> that resolves nothing - codepoints unsupported by the
/// requested font family are not substituted by any fallback.
/// </summary>
/// <remarks>
/// Assign <see cref="Instance"/> to <see cref="Uno.UI.FeatureConfiguration.Font.FallbackService"/>
/// to explicitly opt out of font fallback. This is distinct from leaving the property at its
/// <c>null</c> default, which means "use the platform-registered default service."
/// </remarks>
public sealed class EmptyFontFallbackService : IFontFallbackService
{
	public static EmptyFontFallbackService Instance { get; } = new EmptyFontFallbackService();

	private EmptyFontFallbackService() { }

	public Task<string?> GetFontFamilyForCodepoint(int codepoint) => Task.FromResult<string?>(null);

	public Task<Stream?> GetFontStreamForFontFamily(string fontFamily, FontWeight weight, FontStretch stretch, FontStyle style)
		=> Task.FromResult<Stream?>(null);
}

#nullable enable

using Windows.Foundation;
using Microsoft.UI.Xaml.Documents.TextFormatting;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Documents;

internal interface ICustomTextLayout
{
	IParsedText Create(
		Size availableSize,
		Inline[] inlines,
		FontDetails defaultFontDetails,
		UnicodeText.IFontCacheUpdateListener fontListener,
		Brush? defaultForeground,
		TextAlignment? textAlignment,
		out Size size);
}

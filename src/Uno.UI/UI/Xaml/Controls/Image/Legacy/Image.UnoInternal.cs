using Microsoft.UI.Xaml.Automation.Peers;
using Color = Windows.UI.Color;

namespace Microsoft.UI.Xaml.Controls;

partial class Image
{
	private Color? _monochromeColor;

	/// <summary>
	/// When set, the resulting image is tentatively converted to Monochrome.
	/// </summary>
	internal Color? MonochromeColor
	{
		get => _monochromeColor;
		set
		{
			_monochromeColor = value;
			// Force loading the image.
			OnSourceChanged(Source, forceReload: true);
		}
	}
}

using Microsoft.UI.Xaml.Automation.Peers;
using Color = Windows.UI.Color;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a control that displays an image. The image source 
/// is specified by referring to an image file, using several supported 
/// formats. The image source can also be set with a stream.
/// </summary>
public partial class Image
{

	private Color? _monochromeColor;

	/// <summary>
	/// Creates and returns a ImageAutomationPeer object for this Image.
	/// </summary>
	/// <returns>ImageAutomationPeer.</returns>
	protected override AutomationPeer OnCreateAutomationPeer() => new ImageAutomationPeer(this);

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

	/// <summary>
	/// Returns an Empty string as the Description for the Image.
	/// </summary>
	internal
#if __MACOS__ || __IOS__
		new
#endif
		string Description
	{
		// UNO TODO: Description on Image is not implemented
		get => string.Empty;
	}
}

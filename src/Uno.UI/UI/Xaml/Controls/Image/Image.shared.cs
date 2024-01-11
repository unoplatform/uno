using Microsoft.UI.Xaml.Automation.Peers;
using Color = Windows.UI.Color;

namespace Microsoft.UI.Xaml.Controls;

partial class Image
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
	internal string Description
	{
		// UNO TODO: Description on Image is not implemented
		get => string.Empty;
	}
}

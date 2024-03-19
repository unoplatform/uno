using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a control that displays an image. The image source is specified by referring to an image file,
/// using several supported formats. The image source can also be set with a stream. See Remarks for the list 
/// of supported image source formats.
/// </summary>
public partial class Image : FrameworkElement
{
	public Image()
	{
		InitializePlatform();
	}

	partial void InitializePlatform();

	/// <summary>
	/// Creates and returns a ImageAutomationPeer object for this Image.
	/// </summary>
	/// <returns>ImageAutomationPeer.</returns>
	protected override AutomationPeer OnCreateAutomationPeer() => new ImageAutomationPeer(this);
}

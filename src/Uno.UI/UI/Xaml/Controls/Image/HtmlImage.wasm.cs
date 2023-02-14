#nullable enable

namespace Microsoft.UI.Xaml.Controls;

public class HtmlImage : UIElement
{
	public HtmlImage() : base("img")
	{
		// Avoid the "drag effect" which is set by default in browsers
		SetAttribute("draggable", "false");
	}
}

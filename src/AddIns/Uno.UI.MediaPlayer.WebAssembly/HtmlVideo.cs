#nullable enable
using Windows.UI.Xaml;

namespace Uno.UI.Media;

internal class HtmlVideo : UIElement
{
	public HtmlVideo() : base("video")
	{
		SetAttribute("background-color", "transparent");
		SetStyle(
			("width", "100%"),
			("height", "100%"));
	}
}

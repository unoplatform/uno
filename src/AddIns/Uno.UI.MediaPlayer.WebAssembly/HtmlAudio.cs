using Windows.UI.Xaml;

namespace Uno.UI.Media;

internal class HtmlAudio : UIElement
{
	public HtmlAudio() : base("audio")
	{
		SetAttribute("background-color", "transparent");
	}
}

#nullable enable

namespace Windows.UI.Xaml.Controls;

public class HtmlImage : UIElement
{
	public HtmlImage() : base("img")
	{
		// Avoid the "drag effect" which is set by default in browsers
		SetAttribute("draggable", "false");
	}

	// We don't want HtmlImage to be the OriginalSource of PointerRoutedEventArgs.
	// We want the HitTest call to return Image instead of HtmlImage to match WinUI behavior.
	internal override bool IsViewHit() => false;
}

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class ImageIcon
{
	// TODO Uno specific: WinUI's ImageIcon derives from BitmapIcon natively (DeriveFromBitmapIconHelper_base),
	// so CBitmapIcon::ApplyTemplate creates its Image. Uno's ImageIcon derives from IconElement and mirrors it.
	private protected override void ApplyTemplate(out bool addedVisuals)
	{
		addedVisuals = false;

		if (GetChildElementNoRef() is null)
		{
			AddChildElement(new Image());
			addedVisuals = true;
		}
	}
}

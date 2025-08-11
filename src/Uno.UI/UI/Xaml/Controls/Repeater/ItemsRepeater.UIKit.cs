using UIKit;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemsRepeater
{
	public override void AddSubview(UIView view)
	{
		if (view is not UIElement)
		{
			// When selecting text on iOS inside an ItemsRepeater, the system will add a new subview (_UIContainerWindowPortalView)
			// to the ItemsRepeater. This subview is not a UIElement and should be ignored.
			return;
		}

		base.AddSubview(view);
	}

	public override void InsertSubview(UIView view, nint atIndex)
	{
		if (view is not UIElement)
		{
			// When selecting text on iOS inside an ItemsRepeater, the system will add a new subview (_UIContainerWindowPortalView)
			// to the ItemsRepeater. This subview is not a UIElement and should be ignored.
			return;
		}

		base.InsertSubview(view, atIndex);
	}
}

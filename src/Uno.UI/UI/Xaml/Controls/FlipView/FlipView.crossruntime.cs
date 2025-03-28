using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls;

public partial class FlipView : Selector
{
	protected override bool IsItemItsOwnContainerOverride(object item)
	{
		// Require containers be of type IFlipViewItem
		return item is FlipViewItem;
	}
}


using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls;

public partial class FlipView : Selector
{
	protected override bool IsItemItsOwnContainerOverride(object item)
	{
		// Require containers be of type IFlipViewItem
		return item is FlipViewItem;
	}
}


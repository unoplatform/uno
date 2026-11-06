#if UNO_REFERENCE_API
using DirectUI;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	partial class Selector
	{
		private protected override bool ShouldItemsControlManageChildren => ItemsPanelRoot is not (IVirtualizingPanel or ICustomGeneratorItemsHost);

		partial void RefreshPartial()
		{
			if (VirtualizingPanel != null)
			{
				VirtualizingPanel.GetLayouter().Refresh();

				InvalidateMeasure();
			}
		}
	}
}
#endif

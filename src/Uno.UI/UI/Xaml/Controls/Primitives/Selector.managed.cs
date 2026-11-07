#if UNO_REFERENCE_API
using DirectUI;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	partial class Selector
	{
		private protected override bool ShouldItemsControlManageChildren => ItemsPanelRoot is not ICustomGeneratorItemsHost;

		partial void RefreshPartial()
		{
			if (ItemsPanelRoot is ICustomGeneratorItemsHost customItemsHost)
			{
				customItemsHost.Refresh();

				InvalidateMeasure();
			}
		}
	}
}
#endif

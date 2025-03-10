#if UNO_REFERENCE_API || __MACOS__
using Uno.UI.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using System.Collections.Specialized;
using Uno.Extensions.Specialized;
using System.Diagnostics;
using Uno.UI;
using Uno.Disposables;
using Windows.UI.Xaml.Data;
using Uno.UI.DataBinding;
using Windows.Foundation.Collections;

namespace Windows.UI.Xaml.Controls.Primitives
{
	partial class Selector
	{
		private protected override bool ShouldItemsControlManageChildren => !(ItemsPanelRoot is IVirtualizingPanel);

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

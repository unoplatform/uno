#if __APPLE_UIKIT__ || __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace Microsoft.UI.Xaml.Controls
{
	partial class ItemsPresenter : ILayoutOptOut
	{
		public bool ShouldUseMinSize => !(_itemsPanel is NativeListViewBase);
	}
}
#endif

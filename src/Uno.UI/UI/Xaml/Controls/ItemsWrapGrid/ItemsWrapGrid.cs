#if !IS_UNIT_TESTS && !UNO_REFERENCE_API
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemsWrapGrid : Panel, IVirtualizingPanel
	{
		VirtualizingPanelLayout _layout;

		public int FirstVisibleIndex => _layout?.FirstVisibleIndex ?? -1;

		public int LastVisibleIndex => _layout?.LastVisibleIndex ?? -1;

		internal override Orientation? PhysicalOrientation => Orientation;

		public ItemsWrapGrid()
		{
			if (FeatureConfiguration.ListViewBase.DefaultCacheLength.HasValue)
			{
				CacheLength = FeatureConfiguration.ListViewBase.DefaultCacheLength.Value;
			}
		}

		VirtualizingPanelLayout IVirtualizingPanel.GetLayouter()
		{
			if (_layout == null)
			{
				_layout = new ItemsWrapGridLayout();
				_layout.BindToEquivalentProperty(this, nameof(Orientation));
				_layout.BindToEquivalentProperty(this, nameof(AreStickyGroupHeadersEnabled));
				_layout.BindToEquivalentProperty(this, nameof(ItemHeight));
				_layout.BindToEquivalentProperty(this, nameof(ItemWidth));
				_layout.BindToEquivalentProperty(this, nameof(MaximumRowsOrColumns));
				_layout.BindToEquivalentProperty(this, nameof(GroupHeaderPlacement));
				_layout.BindToEquivalentProperty(this, nameof(GroupPadding));
			}
			return _layout;
		}

		// In WinUI, this is actually for ModernCollectionBasePanel
		internal override bool WantsScrollViewerToObscureAvailableSizeBasedOnScrollBarVisibility(Orientation orientation)
		{
			return Orientation == orientation;
		}
	}
}

#endif

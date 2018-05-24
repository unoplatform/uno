#if !NET46 && !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class ItemsWrapGrid : Panel, IVirtualizingPanel
	{
		VirtualizingPanelLayout _layout;

		public int FirstVisibleIndex => _layout?.FirstVisibleIndex ?? -1;

		public int LastVisibleIndex => _layout?.LastVisibleIndex ?? -1;

		public ItemsWrapGrid() { }

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
	}
}

#endif
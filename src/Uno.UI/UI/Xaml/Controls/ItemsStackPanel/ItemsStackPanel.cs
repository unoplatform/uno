#if !NET46 && !NETSTANDARD2_0
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class ItemsStackPanel : Panel, IVirtualizingPanel
	{
		VirtualizingPanelLayout _layout;

		public int FirstVisibleIndex => _layout?.FirstVisibleIndex ?? -1;
		public int LastVisibleIndex => _layout?.LastVisibleIndex ?? -1;

		VirtualizingPanelLayout IVirtualizingPanel.GetLayouter()
		{
			if (_layout == null)
			{
				_layout = new ItemsStackPanelLayout();
				_layout.BindToEquivalentProperty(this, nameof(Orientation));
				_layout.BindToEquivalentProperty(this, nameof(AreStickyGroupHeadersEnabled));
				_layout.BindToEquivalentProperty(this, nameof(GroupHeaderPlacement));
				_layout.BindToEquivalentProperty(this, nameof(GroupPadding));
			}
			return _layout;
		}
	}
}

#endif
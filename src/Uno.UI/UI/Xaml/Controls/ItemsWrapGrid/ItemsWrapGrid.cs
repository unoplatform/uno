#if !NET461 && !NETSTANDARD2_0 && !__MACOS__
using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI;

namespace Windows.UI.Xaml.Controls
{
	public partial class ItemsWrapGrid : Panel, IVirtualizingPanel
	{
		VirtualizingPanelLayout _layout;

		public int FirstVisibleIndex => _layout?.FirstVisibleIndex ?? -1;

		public int LastVisibleIndex => _layout?.LastVisibleIndex ?? -1;

#if XAMARIN_ANDROID
		public int FirstCacheIndex => _layout.XamlParent.NativePanel.ViewCache.FirstCacheIndex;
		public int LastCacheIndex => _layout.XamlParent.NativePanel.ViewCache.LastCacheIndex;
#endif

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
#if XAMARIN_ANDROID
				_layout.BindToEquivalentProperty(this, nameof(CacheLength));
#endif
			}
			return _layout;
		}
	}
}

#endif

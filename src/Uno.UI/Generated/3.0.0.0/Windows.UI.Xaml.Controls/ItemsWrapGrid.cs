#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ItemsWrapGrid : global::Windows.UI.Xaml.Controls.Panel
	{
#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.GroupHeaderPlacement GroupHeaderPlacement
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Primitives.GroupHeaderPlacement)this.GetValue(GroupHeaderPlacementProperty);
			}
			set
			{
				this.SetValue(GroupHeaderPlacementProperty, value);
			}
		}
#endif
#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double ItemHeight
		{
			get
			{
				return (double)this.GetValue(ItemHeightProperty);
			}
			set
			{
				this.SetValue(ItemHeightProperty, value);
			}
		}
#endif
#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Thickness GroupPadding
		{
			get
			{
				return (global::Windows.UI.Xaml.Thickness)this.GetValue(GroupPaddingProperty);
			}
			set
			{
				this.SetValue(GroupPaddingProperty, value);
			}
		}
#endif
#if false || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  double CacheLength
		{
			get
			{
				return (double)this.GetValue(CacheLengthProperty);
			}
			set
			{
				this.SetValue(CacheLengthProperty, value);
			}
		}
#endif
#if false || false || false || false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Orientation Orientation
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Orientation)this.GetValue(OrientationProperty);
			}
			set
			{
				this.SetValue(OrientationProperty, value);
			}
		}
#endif
#if false || false || false || false
		[global::Uno.NotImplemented]
		public  int MaximumRowsOrColumns
		{
			get
			{
				return (int)this.GetValue(MaximumRowsOrColumnsProperty);
			}
			set
			{
				this.SetValue(MaximumRowsOrColumnsProperty, value);
			}
		}
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public  double ItemWidth
		{
			get
			{
				return (double)this.GetValue(ItemWidthProperty);
			}
			set
			{
				this.SetValue(ItemWidthProperty, value);
			}
		}
		#endif
		#if false || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  int FirstCacheIndex
		{
			get
			{
				throw new global::System.NotImplementedException("The member int ItemsWrapGrid.FirstCacheIndex is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  int FirstVisibleIndex
		{
			get
			{
				throw new global::System.NotImplementedException("The member int ItemsWrapGrid.FirstVisibleIndex is not implemented in Uno.");
			}
		}
		#endif
		#if false || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  int LastCacheIndex
		{
			get
			{
				throw new global::System.NotImplementedException("The member int ItemsWrapGrid.LastCacheIndex is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  int LastVisibleIndex
		{
			get
			{
				throw new global::System.NotImplementedException("The member int ItemsWrapGrid.LastVisibleIndex is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.PanelScrollingDirection ScrollingDirection
		{
			get
			{
				throw new global::System.NotImplementedException("The member PanelScrollingDirection ItemsWrapGrid.ScrollingDirection is not implemented in Uno.");
			}
		}
#endif
#if false || false || false || false
		[global::Uno.NotImplemented]
		public  bool AreStickyGroupHeadersEnabled
		{
			get
			{
				return (bool)this.GetValue(AreStickyGroupHeadersEnabledProperty);
			}
			set
			{
				this.SetValue(AreStickyGroupHeadersEnabledProperty, value);
			}
		}
#endif
#if false || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CacheLengthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CacheLength", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsWrapGrid), 
			new FrameworkPropertyMetadata(default(double)));
#endif
#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty GroupHeaderPlacementProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"GroupHeaderPlacement", typeof(global::Windows.UI.Xaml.Controls.Primitives.GroupHeaderPlacement), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsWrapGrid), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.GroupHeaderPlacement)));
#endif
#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty GroupPaddingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"GroupPadding", typeof(global::Windows.UI.Xaml.Thickness), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsWrapGrid), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Thickness)));
#endif
#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ItemHeightProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ItemHeight", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsWrapGrid), 
			new FrameworkPropertyMetadata(default(double)));
#endif
#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ItemWidthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"ItemWidth", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsWrapGrid), 
			new FrameworkPropertyMetadata(default(double)));
#endif
#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MaximumRowsOrColumnsProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MaximumRowsOrColumns", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsWrapGrid), 
			new FrameworkPropertyMetadata(default(int)));
#endif
#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty OrientationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Orientation", typeof(global::Windows.UI.Xaml.Controls.Orientation), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsWrapGrid), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Orientation)));
#endif
#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AreStickyGroupHeadersEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"AreStickyGroupHeadersEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsWrapGrid), 
			new FrameworkPropertyMetadata(default(bool)));
#endif
#if false || false || false || false
		[global::Uno.NotImplemented]
		public ItemsWrapGrid() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsWrapGrid", "ItemsWrapGrid.ItemsWrapGrid()");
		}
#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.ItemsWrapGrid()
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.GroupPadding.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.GroupPadding.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.Orientation.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.Orientation.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.MaximumRowsOrColumns.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.MaximumRowsOrColumns.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.ItemWidth.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.ItemWidth.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.ItemHeight.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.ItemHeight.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.FirstCacheIndex.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.FirstVisibleIndex.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.LastVisibleIndex.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.LastCacheIndex.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.ScrollingDirection.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.GroupHeaderPlacement.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.GroupHeaderPlacement.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.CacheLength.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.CacheLength.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.AreStickyGroupHeadersEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.AreStickyGroupHeadersEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.AreStickyGroupHeadersEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.GroupPaddingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.OrientationProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.MaximumRowsOrColumnsProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.ItemWidthProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.ItemHeightProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.GroupHeaderPlacementProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsWrapGrid.CacheLengthProperty.get
	}
}

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class ItemsStackPanel : global::Windows.UI.Xaml.Controls.Panel
	{
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.ItemsUpdatingScrollMode ItemsUpdatingScrollMode
		{
			get
			{
				throw new global::System.NotImplementedException("The member ItemsUpdatingScrollMode ItemsStackPanel.ItemsUpdatingScrollMode is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsStackPanel", "ItemsUpdatingScrollMode ItemsStackPanel.ItemsUpdatingScrollMode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  int FirstCacheIndex
		{
			get
			{
				throw new global::System.NotImplementedException("The member int ItemsStackPanel.FirstCacheIndex is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  int FirstVisibleIndex
		{
			get
			{
				throw new global::System.NotImplementedException("The member int ItemsStackPanel.FirstVisibleIndex is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  int LastCacheIndex
		{
			get
			{
				throw new global::System.NotImplementedException("The member int ItemsStackPanel.LastCacheIndex is not implemented in Uno.");
			}
		}
		#endif
		#if false || false || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  int LastVisibleIndex
		{
			get
			{
				throw new global::System.NotImplementedException("The member int ItemsStackPanel.LastVisibleIndex is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.PanelScrollingDirection ScrollingDirection
		{
			get
			{
				throw new global::System.NotImplementedException("The member PanelScrollingDirection ItemsStackPanel.ScrollingDirection is not implemented in Uno.");
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
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CacheLengthProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"CacheLength", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsStackPanel), 
			new FrameworkPropertyMetadata(default(double)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty GroupHeaderPlacementProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"GroupHeaderPlacement", typeof(global::Windows.UI.Xaml.Controls.Primitives.GroupHeaderPlacement), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsStackPanel), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.GroupHeaderPlacement)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty GroupPaddingProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"GroupPadding", typeof(global::Windows.UI.Xaml.Thickness), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsStackPanel), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Thickness)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty OrientationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Orientation", typeof(global::Windows.UI.Xaml.Controls.Orientation), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsStackPanel), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Orientation)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AreStickyGroupHeadersEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"AreStickyGroupHeadersEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.ItemsStackPanel), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public ItemsStackPanel() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.ItemsStackPanel", "ItemsStackPanel.ItemsStackPanel()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.ItemsStackPanel()
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.GroupPadding.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.GroupPadding.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.Orientation.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.Orientation.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.FirstCacheIndex.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.FirstVisibleIndex.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.LastVisibleIndex.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.LastCacheIndex.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.ScrollingDirection.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.GroupHeaderPlacement.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.GroupHeaderPlacement.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.ItemsUpdatingScrollMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.ItemsUpdatingScrollMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.CacheLength.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.CacheLength.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.AreStickyGroupHeadersEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.AreStickyGroupHeadersEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.AreStickyGroupHeadersEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.GroupPaddingProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.OrientationProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.GroupHeaderPlacementProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.ItemsStackPanel.CacheLengthProperty.get
	}
}

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class Pivot 
	{
#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate TitleTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(TitleTemplateProperty);
			}
			set
			{
				this.SetValue(TitleTemplateProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  object Title
		{
			get
			{
				return (object)this.GetValue(TitleProperty);
			}
			set
			{
				this.SetValue(TitleProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  object SelectedItem
		{
			get
			{
				return (object)this.GetValue(SelectedItemProperty);
			}
			set
			{
				this.SetValue(SelectedItemProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  int SelectedIndex
		{
			get
			{
				return (int)this.GetValue(SelectedIndexProperty);
			}
			set
			{
				this.SetValue(SelectedIndexProperty, value);
			}
		}
#endif
#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsLocked
		{
			get
			{
				return (bool)this.GetValue(IsLockedProperty);
			}
			set
			{
				this.SetValue(IsLockedProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate HeaderTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(HeaderTemplateProperty);
			}
			set
			{
				this.SetValue(HeaderTemplateProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate RightHeaderTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(RightHeaderTemplateProperty);
			}
			set
			{
				this.SetValue(RightHeaderTemplateProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  object RightHeader
		{
			get
			{
				return (object)this.GetValue(RightHeaderProperty);
			}
			set
			{
				this.SetValue(RightHeaderProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.DataTemplate LeftHeaderTemplate
		{
			get
			{
				return (global::Windows.UI.Xaml.DataTemplate)this.GetValue(LeftHeaderTemplateProperty);
			}
			set
			{
				this.SetValue(LeftHeaderTemplateProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  object LeftHeader
		{
			get
			{
				return (object)this.GetValue(LeftHeaderProperty);
			}
			set
			{
				this.SetValue(LeftHeaderProperty, value);
			}
		}
#endif
#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool IsHeaderItemsCarouselEnabled
		{
			get
			{
				return (bool)this.GetValue(IsHeaderItemsCarouselEnabledProperty);
			}
			set
			{
				this.SetValue(IsHeaderItemsCarouselEnabledProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.PivotHeaderFocusVisualPlacement HeaderFocusVisualPlacement
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.PivotHeaderFocusVisualPlacement)this.GetValue(HeaderFocusVisualPlacementProperty);
			}
			set
			{
				this.SetValue(HeaderFocusVisualPlacementProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"HeaderTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.Pivot), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsLockedProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsLocked", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Pivot), 
			new FrameworkPropertyMetadata(default(bool)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SelectedIndexProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectedIndex", typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.Pivot), 
			new FrameworkPropertyMetadata(default(int)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SelectedItemProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"SelectedItem", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.Pivot), 
			new FrameworkPropertyMetadata(default(object)));
#endif
#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty SlideInAnimationGroupProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"SlideInAnimationGroup", typeof(global::Windows.UI.Xaml.Controls.PivotSlideInAnimationGroup), 
			typeof(global::Windows.UI.Xaml.Controls.Pivot), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.PivotSlideInAnimationGroup)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TitleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Title", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.Pivot), 
			new FrameworkPropertyMetadata(default(object)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TitleTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TitleTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.Pivot), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LeftHeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"LeftHeader", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.Pivot), 
			new FrameworkPropertyMetadata(default(object)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LeftHeaderTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"LeftHeaderTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.Pivot), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty RightHeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"RightHeader", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.Pivot), 
			new FrameworkPropertyMetadata(default(object)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty RightHeaderTemplateProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"RightHeaderTemplate", typeof(global::Windows.UI.Xaml.DataTemplate), 
			typeof(global::Windows.UI.Xaml.Controls.Pivot), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.DataTemplate)));
#endif
#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderFocusVisualPlacementProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"HeaderFocusVisualPlacement", typeof(global::Windows.UI.Xaml.Controls.PivotHeaderFocusVisualPlacement), 
			typeof(global::Windows.UI.Xaml.Controls.Pivot), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.PivotHeaderFocusVisualPlacement)));
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsHeaderItemsCarouselEnabledProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsHeaderItemsCarouselEnabled", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.Pivot), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if false || false || false || false
		[global::Uno.NotImplemented]
		public Pivot() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Pivot", "Pivot.Pivot()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.Pivot()
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.Title.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.Title.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.TitleTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.TitleTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.HeaderTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.HeaderTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.SelectedIndex.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.SelectedIndex.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.SelectedItem.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.SelectedItem.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.IsLocked.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.IsLocked.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.SelectionChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.SelectionChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.PivotItemLoading.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.PivotItemLoading.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.PivotItemLoaded.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.PivotItemLoaded.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.PivotItemUnloading.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.PivotItemUnloading.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.PivotItemUnloaded.add
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.PivotItemUnloaded.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.LeftHeader.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.LeftHeader.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.LeftHeaderTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.LeftHeaderTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.RightHeader.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.RightHeader.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.RightHeaderTemplate.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.RightHeaderTemplate.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.HeaderFocusVisualPlacement.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.HeaderFocusVisualPlacement.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.IsHeaderItemsCarouselEnabled.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.IsHeaderItemsCarouselEnabled.set
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.HeaderFocusVisualPlacementProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.IsHeaderItemsCarouselEnabledProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.LeftHeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.LeftHeaderTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.RightHeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.RightHeaderTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.TitleProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.TitleTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.HeaderTemplateProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.SelectedIndexProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.SelectedItemProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.IsLockedProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Pivot.SlideInAnimationGroupProperty.get
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Controls.PivotSlideInAnimationGroup GetSlideInAnimationGroup( global::Windows.UI.Xaml.FrameworkElement element)
		{
			return (global::Windows.UI.Xaml.Controls.PivotSlideInAnimationGroup)element.GetValue(SlideInAnimationGroupProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static void SetSlideInAnimationGroup( global::Windows.UI.Xaml.FrameworkElement element,  global::Windows.UI.Xaml.Controls.PivotSlideInAnimationGroup value)
		{
			element.SetValue(SlideInAnimationGroupProperty, value);
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Pivot, global::Windows.UI.Xaml.Controls.PivotItemEventArgs> PivotItemLoaded
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Pivot", "event TypedEventHandler<Pivot, PivotItemEventArgs> Pivot.PivotItemLoaded");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Pivot", "event TypedEventHandler<Pivot, PivotItemEventArgs> Pivot.PivotItemLoaded");
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Pivot, global::Windows.UI.Xaml.Controls.PivotItemEventArgs> PivotItemLoading
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Pivot", "event TypedEventHandler<Pivot, PivotItemEventArgs> Pivot.PivotItemLoading");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Pivot", "event TypedEventHandler<Pivot, PivotItemEventArgs> Pivot.PivotItemLoading");
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Pivot, global::Windows.UI.Xaml.Controls.PivotItemEventArgs> PivotItemUnloaded
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Pivot", "event TypedEventHandler<Pivot, PivotItemEventArgs> Pivot.PivotItemUnloaded");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Pivot", "event TypedEventHandler<Pivot, PivotItemEventArgs> Pivot.PivotItemUnloaded");
			}
		}
#endif
#if false
		public event global::Windows.Foundation.TypedEventHandler<global::Windows.UI.Xaml.Controls.Pivot, global::Windows.UI.Xaml.Controls.PivotItemEventArgs> PivotItemUnloading
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Pivot", "event TypedEventHandler<Pivot, PivotItemEventArgs> Pivot.PivotItemUnloading");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Pivot", "event TypedEventHandler<Pivot, PivotItemEventArgs> Pivot.PivotItemUnloading");
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  event global::Windows.UI.Xaml.Controls.SelectionChangedEventHandler SelectionChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Pivot", "event SelectionChangedEventHandler Pivot.SelectionChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Pivot", "event SelectionChangedEventHandler Pivot.SelectionChanged");
			}
		}
#endif
	}
}

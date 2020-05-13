#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class TreeViewItemTemplateSettings : global::Windows.UI.Xaml.DependencyObject
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Visibility CollapsedGlyphVisibility
		{
			get
			{
				return (global::Windows.UI.Xaml.Visibility)this.GetValue(CollapsedGlyphVisibilityProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  int DragItemsCount
		{
			get
			{
				return (int)this.GetValue(DragItemsCountProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Visibility ExpandedGlyphVisibility
		{
			get
			{
				return (global::Windows.UI.Xaml.Visibility)this.GetValue(ExpandedGlyphVisibilityProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Thickness Indentation
		{
			get
			{
				return (global::Windows.UI.Xaml.Thickness)this.GetValue(IndentationProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty CollapsedGlyphVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(CollapsedGlyphVisibility), typeof(global::Windows.UI.Xaml.Visibility), 
			typeof(global::Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Visibility)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty DragItemsCountProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(DragItemsCount), typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ExpandedGlyphVisibilityProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(ExpandedGlyphVisibility), typeof(global::Windows.UI.Xaml.Visibility), 
			typeof(global::Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Visibility)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IndentationProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Indentation), typeof(global::Windows.UI.Xaml.Thickness), 
			typeof(global::Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Thickness)));
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.TreeViewItemTemplateSettings()
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.TreeViewItemTemplateSettings()
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.ExpandedGlyphVisibility.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.CollapsedGlyphVisibility.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.Indentation.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.DragItemsCount.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.ExpandedGlyphVisibilityProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.CollapsedGlyphVisibilityProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.IndentationProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.TreeViewItemTemplateSettings.DragItemsCountProperty.get
	}
}

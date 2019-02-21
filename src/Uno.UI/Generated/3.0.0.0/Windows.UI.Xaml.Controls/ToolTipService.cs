#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ToolTipService 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PlacementProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"Placement", typeof(global::Windows.UI.Xaml.Controls.Primitives.PlacementMode), 
			typeof(global::Windows.UI.Xaml.Controls.ToolTipService), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Primitives.PlacementMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PlacementTargetProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"PlacementTarget", typeof(global::Windows.UI.Xaml.UIElement), 
			typeof(global::Windows.UI.Xaml.Controls.ToolTipService), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.UIElement)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ToolTipProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.RegisterAttached(
			"ToolTip", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.ToolTipService), 
			new FrameworkPropertyMetadata(default(object)));
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ToolTipService.PlacementProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.Controls.Primitives.PlacementMode GetPlacement( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (global::Windows.UI.Xaml.Controls.Primitives.PlacementMode)element.GetValue(PlacementProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetPlacement( global::Windows.UI.Xaml.DependencyObject element,  global::Windows.UI.Xaml.Controls.Primitives.PlacementMode value)
		{
			element.SetValue(PlacementProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ToolTipService.PlacementTargetProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.UIElement GetPlacementTarget( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (global::Windows.UI.Xaml.UIElement)element.GetValue(PlacementTargetProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetPlacementTarget( global::Windows.UI.Xaml.DependencyObject element,  global::Windows.UI.Xaml.UIElement value)
		{
			element.SetValue(PlacementTargetProperty, value);
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.ToolTipService.ToolTipProperty.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static object GetToolTip( global::Windows.UI.Xaml.DependencyObject element)
		{
			return (object)element.GetValue(ToolTipProperty);
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void SetToolTip( global::Windows.UI.Xaml.DependencyObject element,  object value)
		{
			element.SetValue(ToolTipProperty, value);
		}
		#endif
	}
}

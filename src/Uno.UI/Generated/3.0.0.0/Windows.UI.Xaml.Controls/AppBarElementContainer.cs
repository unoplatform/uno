#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppBarElementContainer : global::Windows.UI.Xaml.Controls.ContentControl,global::Windows.UI.Xaml.Controls.ICommandBarElement,global::Windows.UI.Xaml.Controls.ICommandBarElement2
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsCompact
		{
			get
			{
				return (bool)this.GetValue(IsCompactProperty);
			}
			set
			{
				this.SetValue(IsCompactProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int DynamicOverflowOrder
		{
			get
			{
				return (int)this.GetValue(DynamicOverflowOrderProperty);
			}
			set
			{
				this.SetValue(DynamicOverflowOrderProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsInOverflow
		{
			get
			{
				return (bool)this.GetValue(IsInOverflowProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty DynamicOverflowOrderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(DynamicOverflowOrder), typeof(int), 
			typeof(global::Windows.UI.Xaml.Controls.AppBarElementContainer), 
			new FrameworkPropertyMetadata(default(int)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty IsCompactProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsCompact), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.AppBarElementContainer), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty IsInOverflowProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsInOverflow), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.AppBarElementContainer), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AppBarElementContainer() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AppBarElementContainer", "AppBarElementContainer.AppBarElementContainer()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarElementContainer.AppBarElementContainer()
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarElementContainer.IsCompact.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarElementContainer.IsCompact.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarElementContainer.IsInOverflow.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarElementContainer.DynamicOverflowOrder.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarElementContainer.DynamicOverflowOrder.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarElementContainer.IsCompactProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarElementContainer.IsInOverflowProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AppBarElementContainer.DynamicOverflowOrderProperty.get
		// Processing: Windows.UI.Xaml.Controls.ICommandBarElement
		// Processing: Windows.UI.Xaml.Controls.ICommandBarElement2
	}
}

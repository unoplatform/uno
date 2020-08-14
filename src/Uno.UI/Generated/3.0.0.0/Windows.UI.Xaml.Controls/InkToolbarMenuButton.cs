#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InkToolbarMenuButton : global::Windows.UI.Xaml.Controls.Primitives.ToggleButton
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsExtensionGlyphShown
		{
			get
			{
				return (bool)this.GetValue(IsExtensionGlyphShownProperty);
			}
			set
			{
				this.SetValue(IsExtensionGlyphShownProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.InkToolbarMenuKind MenuKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member InkToolbarMenuKind InkToolbarMenuButton.MenuKind is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty IsExtensionGlyphShownProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(IsExtensionGlyphShown), typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.InkToolbarMenuButton), 
			new FrameworkPropertyMetadata(default(bool)));
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.InkToolbarMenuButton.MenuKind.get
		// Forced skipping of method Windows.UI.Xaml.Controls.InkToolbarMenuButton.IsExtensionGlyphShown.get
		// Forced skipping of method Windows.UI.Xaml.Controls.InkToolbarMenuButton.IsExtensionGlyphShown.set
		// Forced skipping of method Windows.UI.Xaml.Controls.InkToolbarMenuButton.IsExtensionGlyphShownProperty.get
	}
}

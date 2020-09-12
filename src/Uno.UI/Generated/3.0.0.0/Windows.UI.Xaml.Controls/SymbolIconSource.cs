#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SymbolIconSource : global::Windows.UI.Xaml.Controls.IconSource
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.Symbol Symbol
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.Symbol)this.GetValue(SymbolProperty);
			}
			set
			{
				this.SetValue(SymbolProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty SymbolProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Symbol), typeof(global::Windows.UI.Xaml.Controls.Symbol), 
			typeof(global::Windows.UI.Xaml.Controls.SymbolIconSource), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.Symbol)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SymbolIconSource() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.SymbolIconSource", "SymbolIconSource.SymbolIconSource()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.SymbolIconSource.SymbolIconSource()
		// Forced skipping of method Windows.UI.Xaml.Controls.SymbolIconSource.Symbol.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SymbolIconSource.Symbol.set
		// Forced skipping of method Windows.UI.Xaml.Controls.SymbolIconSource.SymbolProperty.get
	}
}

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class IconSourceElement : global::Microsoft.UI.Xaml.Controls.IconElement
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.UI.Xaml.Controls.IconSource IconSource
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Controls.IconSource)this.GetValue(IconSourceProperty);
			}
			set
			{
				this.SetValue(IconSourceProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Microsoft.UI.Xaml.DependencyProperty IconSourceProperty { get; } = 
		Microsoft.UI.Xaml.DependencyProperty.Register(
			nameof(IconSource), typeof(global::Microsoft.UI.Xaml.Controls.IconSource), 
			typeof(global::Microsoft.UI.Xaml.Controls.IconSourceElement), 
			new FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.Controls.IconSource)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public IconSourceElement() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.IconSourceElement", "IconSourceElement.IconSourceElement()");
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Controls.IconSourceElement.IconSourceElement()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.IconSourceElement.IconSource.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.IconSourceElement.IconSource.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.IconSourceElement.IconSourceProperty.get
	}
}

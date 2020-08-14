#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class InkToolbarRulerButton : global::Windows.UI.Xaml.Controls.InkToolbarToggleButton
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Input.Inking.InkPresenterRuler Ruler
		{
			get
			{
				return (global::Windows.UI.Input.Inking.InkPresenterRuler)this.GetValue(RulerProperty);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty RulerProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Ruler), typeof(global::Windows.UI.Input.Inking.InkPresenterRuler), 
			typeof(global::Windows.UI.Xaml.Controls.InkToolbarRulerButton), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Input.Inking.InkPresenterRuler)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public InkToolbarRulerButton() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.InkToolbarRulerButton", "InkToolbarRulerButton.InkToolbarRulerButton()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.InkToolbarRulerButton.InkToolbarRulerButton()
		// Forced skipping of method Windows.UI.Xaml.Controls.InkToolbarRulerButton.Ruler.get
		// Forced skipping of method Windows.UI.Xaml.Controls.InkToolbarRulerButton.RulerProperty.get
	}
}

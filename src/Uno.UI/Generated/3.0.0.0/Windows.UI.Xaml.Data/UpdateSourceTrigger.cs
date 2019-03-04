#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Data
{
	#if false || false || false || false || false
	#if false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public   enum UpdateSourceTrigger 
	{
		// Skipping already declared field Windows.UI.Xaml.Data.UpdateSourceTrigger.Default
		// Skipping already declared field Windows.UI.Xaml.Data.UpdateSourceTrigger.PropertyChanged
		// Skipping already declared field Windows.UI.Xaml.Data.UpdateSourceTrigger.Explicit
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		LostFocus,
		#endif
	}
	#endif
}

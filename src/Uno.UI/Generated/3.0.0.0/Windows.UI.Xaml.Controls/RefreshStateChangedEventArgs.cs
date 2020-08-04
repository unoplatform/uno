#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RefreshStateChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.RefreshVisualizerState NewState
		{
			get
			{
				throw new global::System.NotImplementedException("The member RefreshVisualizerState RefreshStateChangedEventArgs.NewState is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.RefreshVisualizerState OldState
		{
			get
			{
				throw new global::System.NotImplementedException("The member RefreshVisualizerState RefreshStateChangedEventArgs.OldState is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.RefreshStateChangedEventArgs.OldState.get
		// Forced skipping of method Windows.UI.Xaml.Controls.RefreshStateChangedEventArgs.NewState.get
	}
}

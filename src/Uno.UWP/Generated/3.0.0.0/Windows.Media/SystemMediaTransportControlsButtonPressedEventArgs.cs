#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SystemMediaTransportControlsButtonPressedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.SystemMediaTransportControlsButton Button
		{
			get
			{
				throw new global::System.NotImplementedException("The member SystemMediaTransportControlsButton SystemMediaTransportControlsButtonPressedEventArgs.Button is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.SystemMediaTransportControlsButtonPressedEventArgs.Button.get
	}
}

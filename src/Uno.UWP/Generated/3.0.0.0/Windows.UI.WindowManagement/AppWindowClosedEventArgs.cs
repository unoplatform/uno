#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WindowManagement
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppWindowClosedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.WindowManagement.AppWindowClosedReason Reason
		{
			get
			{
				throw new global::System.NotImplementedException("The member AppWindowClosedReason AppWindowClosedEventArgs.Reason is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.WindowManagement.AppWindowClosedEventArgs.Reason.get
	}
}

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.WebUI
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SuspendingEventArgs : global::Windows.ApplicationModel.ISuspendingEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.SuspendingOperation SuspendingOperation
		{
			get
			{
				throw new global::System.NotImplementedException("The member SuspendingOperation SuspendingEventArgs.SuspendingOperation is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.WebUI.SuspendingEventArgs.SuspendingOperation.get
		// Processing: Windows.ApplicationModel.ISuspendingEventArgs
	}
}

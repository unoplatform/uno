#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Notifications
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ToastActivatedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Arguments
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ToastActivatedEventArgs.Arguments is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20ToastActivatedEventArgs.Arguments");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.ValueSet UserInput
		{
			get
			{
				throw new global::System.NotImplementedException("The member ValueSet ToastActivatedEventArgs.UserInput is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ValueSet%20ToastActivatedEventArgs.UserInput");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Notifications.ToastActivatedEventArgs.Arguments.get
		// Forced skipping of method Windows.UI.Notifications.ToastActivatedEventArgs.UserInput.get
	}
}

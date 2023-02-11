#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Calls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PhoneLineDialResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Calls.PhoneCallOperationStatus DialCallStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member PhoneCallOperationStatus PhoneLineDialResult.DialCallStatus is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PhoneCallOperationStatus%20PhoneLineDialResult.DialCallStatus");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Calls.PhoneCall DialedCall
		{
			get
			{
				throw new global::System.NotImplementedException("The member PhoneCall PhoneLineDialResult.DialedCall is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PhoneCall%20PhoneLineDialResult.DialedCall");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneLineDialResult.DialCallStatus.get
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneLineDialResult.DialedCall.get
	}
}

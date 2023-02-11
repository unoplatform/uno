#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Calls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PhoneCallsResult 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Calls.PhoneCall> AllActivePhoneCalls
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<PhoneCall> PhoneCallsResult.AllActivePhoneCalls is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CPhoneCall%3E%20PhoneCallsResult.AllActivePhoneCalls");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Calls.PhoneLineOperationStatus OperationStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member PhoneLineOperationStatus PhoneCallsResult.OperationStatus is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PhoneLineOperationStatus%20PhoneCallsResult.OperationStatus");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneCallsResult.OperationStatus.get
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneCallsResult.AllActivePhoneCalls.get
	}
}

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Calls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PhoneCallInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Calls.PhoneCallDirection CallDirection
		{
			get
			{
				throw new global::System.NotImplementedException("The member PhoneCallDirection PhoneCallInfo.CallDirection is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PhoneCallDirection%20PhoneCallInfo.CallDirection");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DisplayName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PhoneCallInfo.DisplayName is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PhoneCallInfo.DisplayName");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsHoldSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PhoneCallInfo.IsHoldSupported is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20PhoneCallInfo.IsHoldSupported");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Guid LineId
		{
			get
			{
				throw new global::System.NotImplementedException("The member Guid PhoneCallInfo.LineId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Guid%20PhoneCallInfo.LineId");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PhoneNumber
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PhoneCallInfo.PhoneNumber is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PhoneCallInfo.PhoneNumber");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.DateTimeOffset StartTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset PhoneCallInfo.StartTime is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DateTimeOffset%20PhoneCallInfo.StartTime");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneCallInfo.LineId.get
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneCallInfo.IsHoldSupported.get
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneCallInfo.StartTime.get
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneCallInfo.PhoneNumber.get
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneCallInfo.DisplayName.get
		// Forced skipping of method Windows.ApplicationModel.Calls.PhoneCallInfo.CallDirection.get
	}
}

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email.DataProvider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailMailboxProposeNewTimeForMeetingRequestEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.DataProvider.EmailMailboxProposeNewTimeForMeetingRequest Request
		{
			get
			{
				throw new global::System.NotImplementedException("The member EmailMailboxProposeNewTimeForMeetingRequest EmailMailboxProposeNewTimeForMeetingRequestEventArgs.Request is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=EmailMailboxProposeNewTimeForMeetingRequest%20EmailMailboxProposeNewTimeForMeetingRequestEventArgs.Request");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxProposeNewTimeForMeetingRequestEventArgs.Request.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral EmailMailboxProposeNewTimeForMeetingRequestEventArgs.GetDeferral() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Deferral%20EmailMailboxProposeNewTimeForMeetingRequestEventArgs.GetDeferral%28%29");
		}
		#endif
	}
}

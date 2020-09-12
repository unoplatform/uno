#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.AllJoyn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AllJoynSessionJoinedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.AllJoyn.AllJoynSession Session
		{
			get
			{
				throw new global::System.NotImplementedException("The member AllJoynSession AllJoynSessionJoinedEventArgs.Session is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AllJoynSessionJoinedEventArgs( global::Windows.Devices.AllJoyn.AllJoynSession session) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynSessionJoinedEventArgs", "AllJoynSessionJoinedEventArgs.AllJoynSessionJoinedEventArgs(AllJoynSession session)");
		}
		#endif
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynSessionJoinedEventArgs.AllJoynSessionJoinedEventArgs(Windows.Devices.AllJoyn.AllJoynSession)
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynSessionJoinedEventArgs.Session.get
	}
}

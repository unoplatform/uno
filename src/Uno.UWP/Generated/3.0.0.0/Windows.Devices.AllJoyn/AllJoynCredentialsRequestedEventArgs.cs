#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.AllJoyn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AllJoynCredentialsRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  ushort AttemptCount
		{
			get
			{
				throw new global::System.NotImplementedException("The member ushort AllJoynCredentialsRequestedEventArgs.AttemptCount is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.AllJoyn.AllJoynCredentials Credentials
		{
			get
			{
				throw new global::System.NotImplementedException("The member AllJoynCredentials AllJoynCredentialsRequestedEventArgs.Credentials is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PeerUniqueName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AllJoynCredentialsRequestedEventArgs.PeerUniqueName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string RequestedUserName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AllJoynCredentialsRequestedEventArgs.RequestedUserName is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentialsRequestedEventArgs.AttemptCount.get
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentialsRequestedEventArgs.Credentials.get
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentialsRequestedEventArgs.PeerUniqueName.get
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynCredentialsRequestedEventArgs.RequestedUserName.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Deferral GetDeferral()
		{
			throw new global::System.NotImplementedException("The member Deferral AllJoynCredentialsRequestedEventArgs.GetDeferral() is not implemented in Uno.");
		}
		#endif
	}
}

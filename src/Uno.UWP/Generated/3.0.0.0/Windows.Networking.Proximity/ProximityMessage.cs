#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.Proximity
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ProximityMessage 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Storage.Streams.IBuffer Data
		{
			get
			{
				throw new global::System.NotImplementedException("The member IBuffer ProximityMessage.Data is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IBuffer%20ProximityMessage.Data");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string DataAsString
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ProximityMessage.DataAsString is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20ProximityMessage.DataAsString");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string MessageType
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ProximityMessage.MessageType is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20ProximityMessage.MessageType");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  long SubscriptionId
		{
			get
			{
				throw new global::System.NotImplementedException("The member long ProximityMessage.SubscriptionId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=long%20ProximityMessage.SubscriptionId");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.Proximity.ProximityMessage.MessageType.get
		// Forced skipping of method Windows.Networking.Proximity.ProximityMessage.SubscriptionId.get
		// Forced skipping of method Windows.Networking.Proximity.ProximityMessage.Data.get
		// Forced skipping of method Windows.Networking.Proximity.ProximityMessage.DataAsString.get
	}
}

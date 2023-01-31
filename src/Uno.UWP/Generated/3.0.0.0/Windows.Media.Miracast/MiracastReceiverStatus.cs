#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.Miracast
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class MiracastReceiverStatus 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsConnectionTakeoverSupported
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool MiracastReceiverStatus.IsConnectionTakeoverSupported is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20MiracastReceiverStatus.IsConnectionTakeoverSupported");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.Media.Miracast.MiracastTransmitter> KnownTransmitters
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<MiracastTransmitter> MiracastReceiverStatus.KnownTransmitters is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CMiracastTransmitter%3E%20MiracastReceiverStatus.KnownTransmitters");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Miracast.MiracastReceiverListeningStatus ListeningStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member MiracastReceiverListeningStatus MiracastReceiverStatus.ListeningStatus is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MiracastReceiverListeningStatus%20MiracastReceiverStatus.ListeningStatus");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int MaxSimultaneousConnections
		{
			get
			{
				throw new global::System.NotImplementedException("The member int MiracastReceiverStatus.MaxSimultaneousConnections is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20MiracastReceiverStatus.MaxSimultaneousConnections");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.Miracast.MiracastReceiverWiFiStatus WiFiStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member MiracastReceiverWiFiStatus MiracastReceiverStatus.WiFiStatus is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=MiracastReceiverWiFiStatus%20MiracastReceiverStatus.WiFiStatus");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverStatus.ListeningStatus.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverStatus.WiFiStatus.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverStatus.IsConnectionTakeoverSupported.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverStatus.MaxSimultaneousConnections.get
		// Forced skipping of method Windows.Media.Miracast.MiracastReceiverStatus.KnownTransmitters.get
	}
}

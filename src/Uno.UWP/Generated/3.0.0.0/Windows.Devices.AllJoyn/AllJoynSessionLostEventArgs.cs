#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.AllJoyn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AllJoynSessionLostEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Devices.AllJoyn.AllJoynSessionLostReason Reason
		{
			get
			{
				throw new global::System.NotImplementedException("The member AllJoynSessionLostReason AllJoynSessionLostEventArgs.Reason is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AllJoynSessionLostEventArgs( global::Windows.Devices.AllJoyn.AllJoynSessionLostReason reason) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynSessionLostEventArgs", "AllJoynSessionLostEventArgs.AllJoynSessionLostEventArgs(AllJoynSessionLostReason reason)");
		}
		#endif
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynSessionLostEventArgs.AllJoynSessionLostEventArgs(Windows.Devices.AllJoyn.AllJoynSessionLostReason)
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynSessionLostEventArgs.Reason.get
	}
}

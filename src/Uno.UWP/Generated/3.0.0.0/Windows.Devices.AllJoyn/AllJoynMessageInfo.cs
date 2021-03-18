#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.AllJoyn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AllJoynMessageInfo 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string SenderUniqueName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AllJoynMessageInfo.SenderUniqueName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AllJoynMessageInfo( string senderUniqueName) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynMessageInfo", "AllJoynMessageInfo.AllJoynMessageInfo(string senderUniqueName)");
		}
		#endif
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynMessageInfo.AllJoynMessageInfo(string)
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynMessageInfo.SenderUniqueName.get
	}
}

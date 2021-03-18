#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.AllJoyn
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AllJoynSessionMemberAddedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string UniqueName
		{
			get
			{
				throw new global::System.NotImplementedException("The member string AllJoynSessionMemberAddedEventArgs.UniqueName is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public AllJoynSessionMemberAddedEventArgs( string uniqueName) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Devices.AllJoyn.AllJoynSessionMemberAddedEventArgs", "AllJoynSessionMemberAddedEventArgs.AllJoynSessionMemberAddedEventArgs(string uniqueName)");
		}
		#endif
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynSessionMemberAddedEventArgs.AllJoynSessionMemberAddedEventArgs(string)
		// Forced skipping of method Windows.Devices.AllJoyn.AllJoynSessionMemberAddedEventArgs.UniqueName.get
	}
}

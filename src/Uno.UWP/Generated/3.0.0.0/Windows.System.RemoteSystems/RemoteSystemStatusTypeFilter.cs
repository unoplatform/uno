#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.RemoteSystems
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteSystemStatusTypeFilter : global::Windows.System.RemoteSystems.IRemoteSystemFilter
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.RemoteSystems.RemoteSystemStatusType RemoteSystemStatusType
		{
			get
			{
				throw new global::System.NotImplementedException("The member RemoteSystemStatusType RemoteSystemStatusTypeFilter.RemoteSystemStatusType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RemoteSystemStatusTypeFilter( global::Windows.System.RemoteSystems.RemoteSystemStatusType remoteSystemStatusType) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemStatusTypeFilter", "RemoteSystemStatusTypeFilter.RemoteSystemStatusTypeFilter(RemoteSystemStatusType remoteSystemStatusType)");
		}
		#endif
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemStatusTypeFilter.RemoteSystemStatusTypeFilter(Windows.System.RemoteSystems.RemoteSystemStatusType)
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemStatusTypeFilter.RemoteSystemStatusType.get
		// Processing: Windows.System.RemoteSystems.IRemoteSystemFilter
	}
}

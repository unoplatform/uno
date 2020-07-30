#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.RemoteSystems
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteSystemSessionJoinRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.RemoteSystems.RemoteSystemSessionParticipant Participant
		{
			get
			{
				throw new global::System.NotImplementedException("The member RemoteSystemSessionParticipant RemoteSystemSessionJoinRequest.Participant is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionJoinRequest.Participant.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void Accept()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionJoinRequest", "void RemoteSystemSessionJoinRequest.Accept()");
		}
		#endif
	}
}

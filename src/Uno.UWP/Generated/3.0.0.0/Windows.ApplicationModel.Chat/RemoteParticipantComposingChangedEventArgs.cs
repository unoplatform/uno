#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteParticipantComposingChangedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsComposing
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool RemoteParticipantComposingChangedEventArgs.IsComposing is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string ParticipantAddress
		{
			get
			{
				throw new global::System.NotImplementedException("The member string RemoteParticipantComposingChangedEventArgs.ParticipantAddress is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string TransportId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string RemoteParticipantComposingChangedEventArgs.TransportId is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Chat.RemoteParticipantComposingChangedEventArgs.TransportId.get
		// Forced skipping of method Windows.ApplicationModel.Chat.RemoteParticipantComposingChangedEventArgs.ParticipantAddress.get
		// Forced skipping of method Windows.ApplicationModel.Chat.RemoteParticipantComposingChangedEventArgs.IsComposing.get
	}
}

#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.RemoteSystems
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class RemoteSystemSessionMessageChannel 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.System.RemoteSystems.RemoteSystemSession Session
		{
			get
			{
				throw new global::System.NotImplementedException("The member RemoteSystemSession RemoteSystemSessionMessageChannel.Session is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RemoteSystemSessionMessageChannel( global::Windows.System.RemoteSystems.RemoteSystemSession session,  string channelName) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionMessageChannel", "RemoteSystemSessionMessageChannel.RemoteSystemSessionMessageChannel(RemoteSystemSession session, string channelName)");
		}
		#endif
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionMessageChannel.RemoteSystemSessionMessageChannel(Windows.System.RemoteSystems.RemoteSystemSession, string)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public RemoteSystemSessionMessageChannel( global::Windows.System.RemoteSystems.RemoteSystemSession session,  string channelName,  global::Windows.System.RemoteSystems.RemoteSystemSessionMessageChannelReliability reliability) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionMessageChannel", "RemoteSystemSessionMessageChannel.RemoteSystemSessionMessageChannel(RemoteSystemSession session, string channelName, RemoteSystemSessionMessageChannelReliability reliability)");
		}
		#endif
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionMessageChannel.RemoteSystemSessionMessageChannel(Windows.System.RemoteSystems.RemoteSystemSession, string, Windows.System.RemoteSystems.RemoteSystemSessionMessageChannelReliability)
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionMessageChannel.Session.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> BroadcastValueSetAsync( global::Windows.Foundation.Collections.ValueSet messageData)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> RemoteSystemSessionMessageChannel.BroadcastValueSetAsync(ValueSet messageData) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> SendValueSetAsync( global::Windows.Foundation.Collections.ValueSet messageData,  global::Windows.System.RemoteSystems.RemoteSystemSessionParticipant participant)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> RemoteSystemSessionMessageChannel.SendValueSetAsync(ValueSet messageData, RemoteSystemSessionParticipant participant) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> SendValueSetToParticipantsAsync( global::Windows.Foundation.Collections.ValueSet messageData,  global::System.Collections.Generic.IEnumerable<global::Windows.System.RemoteSystems.RemoteSystemSessionParticipant> participants)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> RemoteSystemSessionMessageChannel.SendValueSetToParticipantsAsync(ValueSet messageData, IEnumerable<RemoteSystemSessionParticipant> participants) is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionMessageChannel.ValueSetReceived.add
		// Forced skipping of method Windows.System.RemoteSystems.RemoteSystemSessionMessageChannel.ValueSetReceived.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.System.RemoteSystems.RemoteSystemSessionMessageChannel, global::Windows.System.RemoteSystems.RemoteSystemSessionValueSetReceivedEventArgs> ValueSetReceived
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionMessageChannel", "event TypedEventHandler<RemoteSystemSessionMessageChannel, RemoteSystemSessionValueSetReceivedEventArgs> RemoteSystemSessionMessageChannel.ValueSetReceived");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.RemoteSystems.RemoteSystemSessionMessageChannel", "event TypedEventHandler<RemoteSystemSessionMessageChannel, RemoteSystemSessionValueSetReceivedEventArgs> RemoteSystemSessionMessageChannel.ValueSetReceived");
			}
		}
		#endif
	}
}

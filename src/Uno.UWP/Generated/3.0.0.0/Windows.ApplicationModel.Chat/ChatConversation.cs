#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Chat
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ChatConversation : global::Windows.ApplicationModel.Chat.IChatItem
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Subject
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ChatConversation.Subject is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Chat.ChatConversation", "string ChatConversation.Subject");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsConversationMuted
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ChatConversation.IsConversationMuted is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Chat.ChatConversation", "bool ChatConversation.IsConversationMuted");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool HasUnreadMessages
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ChatConversation.HasUnreadMessages is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Id
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ChatConversation.Id is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string MostRecentMessageId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ChatConversation.MostRecentMessageId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<string> Participants
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<string> ChatConversation.Participants is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Chat.ChatConversationThreadingInfo ThreadingInfo
		{
			get
			{
				throw new global::System.NotImplementedException("The member ChatConversationThreadingInfo ChatConversation.ThreadingInfo is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool CanModifyParticipants
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ChatConversation.CanModifyParticipants is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Chat.ChatConversation", "bool ChatConversation.CanModifyParticipants");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Chat.ChatItemKind ItemKind
		{
			get
			{
				throw new global::System.NotImplementedException("The member ChatItemKind ChatConversation.ItemKind is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatConversation.HasUnreadMessages.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatConversation.Id.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatConversation.Subject.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatConversation.Subject.set
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatConversation.IsConversationMuted.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatConversation.IsConversationMuted.set
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatConversation.MostRecentMessageId.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatConversation.Participants.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatConversation.ThreadingInfo.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction DeleteAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ChatConversation.DeleteAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Chat.ChatMessageReader GetMessageReader()
		{
			throw new global::System.NotImplementedException("The member ChatMessageReader ChatConversation.GetMessageReader() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction MarkMessagesAsReadAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ChatConversation.MarkMessagesAsReadAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction MarkMessagesAsReadAsync( global::System.DateTimeOffset value)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ChatConversation.MarkMessagesAsReadAsync(DateTimeOffset value) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction SaveAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ChatConversation.SaveAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void NotifyLocalParticipantComposing( string transportId,  string participantAddress,  bool isComposing)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Chat.ChatConversation", "void ChatConversation.NotifyLocalParticipantComposing(string transportId, string participantAddress, bool isComposing)");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  void NotifyRemoteParticipantComposing( string transportId,  string participantAddress,  bool isComposing)
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Chat.ChatConversation", "void ChatConversation.NotifyRemoteParticipantComposing(string transportId, string participantAddress, bool isComposing)");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatConversation.RemoteParticipantComposingChanged.add
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatConversation.RemoteParticipantComposingChanged.remove
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatConversation.CanModifyParticipants.get
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatConversation.CanModifyParticipants.set
		// Forced skipping of method Windows.ApplicationModel.Chat.ChatConversation.ItemKind.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.Chat.ChatConversation, global::Windows.ApplicationModel.Chat.RemoteParticipantComposingChangedEventArgs> RemoteParticipantComposingChanged
		{
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Chat.ChatConversation", "event TypedEventHandler<ChatConversation, RemoteParticipantComposingChangedEventArgs> ChatConversation.RemoteParticipantComposingChanged");
			}
			[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Chat.ChatConversation", "event TypedEventHandler<ChatConversation, RemoteParticipantComposingChangedEventArgs> ChatConversation.RemoteParticipantComposingChanged");
			}
		}
		#endif
		// Processing: Windows.ApplicationModel.Chat.IChatItem
	}
}

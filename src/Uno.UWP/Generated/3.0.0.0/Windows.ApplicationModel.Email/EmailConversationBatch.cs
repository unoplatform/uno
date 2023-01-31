#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailConversationBatch 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Email.EmailConversation> Conversations
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<EmailConversation> EmailConversationBatch.Conversations is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyList%3CEmailConversation%3E%20EmailConversationBatch.Conversations");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.Email.EmailBatchStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member EmailBatchStatus EmailConversationBatch.Status is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=EmailBatchStatus%20EmailConversationBatch.Status");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.EmailConversationBatch.Conversations.get
		// Forced skipping of method Windows.ApplicationModel.Email.EmailConversationBatch.Status.get
	}
}

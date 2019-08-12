#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email.DataProvider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailMailboxForwardMeetingRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Comment
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxForwardMeetingRequest.Comment is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string EmailMailboxId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxForwardMeetingRequest.EmailMailboxId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string EmailMessageId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxForwardMeetingRequest.EmailMessageId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string ForwardHeader
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxForwardMeetingRequest.ForwardHeader is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.Email.EmailMessageBodyKind ForwardHeaderType
		{
			get
			{
				throw new global::System.NotImplementedException("The member EmailMessageBodyKind EmailMailboxForwardMeetingRequest.ForwardHeaderType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.Email.EmailRecipient> Recipients
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyList<EmailRecipient> EmailMailboxForwardMeetingRequest.Recipients is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Subject
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxForwardMeetingRequest.Subject is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxForwardMeetingRequest.EmailMailboxId.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxForwardMeetingRequest.EmailMessageId.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxForwardMeetingRequest.Recipients.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxForwardMeetingRequest.Subject.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxForwardMeetingRequest.ForwardHeaderType.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxForwardMeetingRequest.ForwardHeader.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxForwardMeetingRequest.Comment.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction ReportCompletedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxForwardMeetingRequest.ReportCompletedAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxForwardMeetingRequest.ReportFailedAsync() is not implemented in Uno.");
		}
		#endif
	}
}

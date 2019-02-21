#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.Email.DataProvider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class EmailMailboxProposeNewTimeForMeetingRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Comment
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxProposeNewTimeForMeetingRequest.Comment is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string EmailMailboxId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxProposeNewTimeForMeetingRequest.EmailMailboxId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string EmailMessageId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxProposeNewTimeForMeetingRequest.EmailMessageId is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.TimeSpan NewDuration
		{
			get
			{
				throw new global::System.NotImplementedException("The member TimeSpan EmailMailboxProposeNewTimeForMeetingRequest.NewDuration is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset NewStartTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset EmailMailboxProposeNewTimeForMeetingRequest.NewStartTime is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Subject
		{
			get
			{
				throw new global::System.NotImplementedException("The member string EmailMailboxProposeNewTimeForMeetingRequest.Subject is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxProposeNewTimeForMeetingRequest.EmailMailboxId.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxProposeNewTimeForMeetingRequest.EmailMessageId.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxProposeNewTimeForMeetingRequest.NewStartTime.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxProposeNewTimeForMeetingRequest.NewDuration.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxProposeNewTimeForMeetingRequest.Subject.get
		// Forced skipping of method Windows.ApplicationModel.Email.DataProvider.EmailMailboxProposeNewTimeForMeetingRequest.Comment.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction ReportCompletedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxProposeNewTimeForMeetingRequest.ReportCompletedAsync() is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction EmailMailboxProposeNewTimeForMeetingRequest.ReportFailedAsync() is not implemented in Uno.");
		}
		#endif
	}
}

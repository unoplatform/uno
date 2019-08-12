#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataTasks
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserDataTaskListSyncManager 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.ApplicationModel.UserDataTasks.UserDataTaskListSyncStatus Status
		{
			get
			{
				throw new global::System.NotImplementedException("The member UserDataTaskListSyncStatus UserDataTaskListSyncManager.Status is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.UserDataTasks.UserDataTaskListSyncManager", "UserDataTaskListSyncStatus UserDataTaskListSyncManager.Status");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset LastSuccessfulSyncTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset UserDataTaskListSyncManager.LastSuccessfulSyncTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.UserDataTasks.UserDataTaskListSyncManager", "DateTimeOffset UserDataTaskListSyncManager.LastSuccessfulSyncTime");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::System.DateTimeOffset LastAttemptedSyncTime
		{
			get
			{
				throw new global::System.NotImplementedException("The member DateTimeOffset UserDataTaskListSyncManager.LastAttemptedSyncTime is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.UserDataTasks.UserDataTaskListSyncManager", "DateTimeOffset UserDataTaskListSyncManager.LastAttemptedSyncTime");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.UserDataTaskListSyncManager.LastAttemptedSyncTime.get
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.UserDataTaskListSyncManager.LastAttemptedSyncTime.set
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.UserDataTaskListSyncManager.LastSuccessfulSyncTime.get
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.UserDataTaskListSyncManager.LastSuccessfulSyncTime.set
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.UserDataTaskListSyncManager.Status.get
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.UserDataTaskListSyncManager.Status.set
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.Foundation.IAsyncOperation<bool> SyncAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> UserDataTaskListSyncManager.SyncAsync() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.UserDataTaskListSyncManager.SyncStatusChanged.add
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.UserDataTaskListSyncManager.SyncStatusChanged.remove
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  event global::Windows.Foundation.TypedEventHandler<global::Windows.ApplicationModel.UserDataTasks.UserDataTaskListSyncManager, object> SyncStatusChanged
		{
			[global::Uno.NotImplemented]
			add
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.UserDataTasks.UserDataTaskListSyncManager", "event TypedEventHandler<UserDataTaskListSyncManager, object> UserDataTaskListSyncManager.SyncStatusChanged");
			}
			[global::Uno.NotImplemented]
			remove
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.UserDataTasks.UserDataTaskListSyncManager", "event TypedEventHandler<UserDataTaskListSyncManager, object> UserDataTaskListSyncManager.SyncStatusChanged");
			}
		}
		#endif
	}
}

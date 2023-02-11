#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataTasks.DataProvider
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserDataTaskListCreateOrUpdateTaskRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.ApplicationModel.UserDataTasks.UserDataTask Task
		{
			get
			{
				throw new global::System.NotImplementedException("The member UserDataTask UserDataTaskListCreateOrUpdateTaskRequest.Task is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=UserDataTask%20UserDataTaskListCreateOrUpdateTaskRequest.Task");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string TaskListId
		{
			get
			{
				throw new global::System.NotImplementedException("The member string UserDataTaskListCreateOrUpdateTaskRequest.TaskListId is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20UserDataTaskListCreateOrUpdateTaskRequest.TaskListId");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.DataProvider.UserDataTaskListCreateOrUpdateTaskRequest.TaskListId.get
		// Forced skipping of method Windows.ApplicationModel.UserDataTasks.DataProvider.UserDataTaskListCreateOrUpdateTaskRequest.Task.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportCompletedAsync( global::Windows.ApplicationModel.UserDataTasks.UserDataTask createdOrUpdatedUserDataTask)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction UserDataTaskListCreateOrUpdateTaskRequest.ReportCompletedAsync(UserDataTask createdOrUpdatedUserDataTask) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20UserDataTaskListCreateOrUpdateTaskRequest.ReportCompletedAsync%28UserDataTask%20createdOrUpdatedUserDataTask%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction ReportFailedAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction UserDataTaskListCreateOrUpdateTaskRequest.ReportFailedAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncAction%20UserDataTaskListCreateOrUpdateTaskRequest.ReportFailedAsync%28%29");
		}
		#endif
	}
}

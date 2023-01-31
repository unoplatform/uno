#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataTasks
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserDataTaskListLimitedWriteOperations 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<string> TryCompleteTaskAsync( string userDataTaskId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<string> UserDataTaskListLimitedWriteOperations.TryCompleteTaskAsync(string userDataTaskId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cstring%3E%20UserDataTaskListLimitedWriteOperations.TryCompleteTaskAsync%28string%20userDataTaskId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryCreateOrUpdateTaskAsync( global::Windows.ApplicationModel.UserDataTasks.UserDataTask userDataTask)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> UserDataTaskListLimitedWriteOperations.TryCreateOrUpdateTaskAsync(UserDataTask userDataTask) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20UserDataTaskListLimitedWriteOperations.TryCreateOrUpdateTaskAsync%28UserDataTask%20userDataTask%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TryDeleteTaskAsync( string userDataTaskId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> UserDataTaskListLimitedWriteOperations.TryDeleteTaskAsync(string userDataTaskId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20UserDataTaskListLimitedWriteOperations.TryDeleteTaskAsync%28string%20userDataTaskId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<bool> TrySkipOccurrenceAsync( string userDataTaskId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<bool> UserDataTaskListLimitedWriteOperations.TrySkipOccurrenceAsync(string userDataTaskId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3Cbool%3E%20UserDataTaskListLimitedWriteOperations.TrySkipOccurrenceAsync%28string%20userDataTaskId%29");
		}
		#endif
	}
}

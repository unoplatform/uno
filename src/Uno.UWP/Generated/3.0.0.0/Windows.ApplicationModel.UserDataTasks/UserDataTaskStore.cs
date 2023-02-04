#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataTasks
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class UserDataTaskStore 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.UserDataTasks.UserDataTaskList> CreateListAsync( string name)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<UserDataTaskList> UserDataTaskStore.CreateListAsync(string name) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CUserDataTaskList%3E%20UserDataTaskStore.CreateListAsync%28string%20name%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.UserDataTasks.UserDataTaskList> CreateListAsync( string name,  string userDataAccountId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<UserDataTaskList> UserDataTaskStore.CreateListAsync(string name, string userDataAccountId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CUserDataTaskList%3E%20UserDataTaskStore.CreateListAsync%28string%20name%2C%20string%20userDataAccountId%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::System.Collections.Generic.IReadOnlyList<global::Windows.ApplicationModel.UserDataTasks.UserDataTaskList>> FindListsAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IReadOnlyList<UserDataTaskList>> UserDataTaskStore.FindListsAsync() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIReadOnlyList%3CUserDataTaskList%3E%3E%20UserDataTaskStore.FindListsAsync%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.UserDataTasks.UserDataTaskList> GetListAsync( string taskListId)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<UserDataTaskList> UserDataTaskStore.GetListAsync(string taskListId) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CUserDataTaskList%3E%20UserDataTaskStore.GetListAsync%28string%20taskListId%29");
		}
		#endif
	}
}

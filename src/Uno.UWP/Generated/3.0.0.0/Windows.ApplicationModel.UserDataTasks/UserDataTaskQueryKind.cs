#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.UserDataTasks
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum UserDataTaskQueryKind 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		All,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Incomplete,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Complete,
		#endif
	}
	#endif
}

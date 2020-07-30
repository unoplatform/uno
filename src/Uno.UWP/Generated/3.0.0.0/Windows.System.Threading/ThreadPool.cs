#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Threading
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ThreadPool 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction RunAsync( global::Windows.System.Threading.WorkItemHandler handler)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ThreadPool.RunAsync(WorkItemHandler handler) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction RunAsync( global::Windows.System.Threading.WorkItemHandler handler,  global::Windows.System.Threading.WorkItemPriority priority)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ThreadPool.RunAsync(WorkItemHandler handler, WorkItemPriority priority) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncAction RunAsync( global::Windows.System.Threading.WorkItemHandler handler,  global::Windows.System.Threading.WorkItemPriority priority,  global::Windows.System.Threading.WorkItemOptions options)
		{
			throw new global::System.NotImplementedException("The member IAsyncAction ThreadPool.RunAsync(WorkItemHandler handler, WorkItemPriority priority, WorkItemOptions options) is not implemented in Uno.");
		}
		#endif
	}
}

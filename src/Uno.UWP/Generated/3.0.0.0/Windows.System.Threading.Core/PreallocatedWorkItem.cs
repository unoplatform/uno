#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.System.Threading.Core
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PreallocatedWorkItem 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PreallocatedWorkItem( global::Windows.System.Threading.WorkItemHandler handler) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Threading.Core.PreallocatedWorkItem", "PreallocatedWorkItem.PreallocatedWorkItem(WorkItemHandler handler)");
		}
		#endif
		// Forced skipping of method Windows.System.Threading.Core.PreallocatedWorkItem.PreallocatedWorkItem(Windows.System.Threading.WorkItemHandler)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PreallocatedWorkItem( global::Windows.System.Threading.WorkItemHandler handler,  global::Windows.System.Threading.WorkItemPriority priority) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Threading.Core.PreallocatedWorkItem", "PreallocatedWorkItem.PreallocatedWorkItem(WorkItemHandler handler, WorkItemPriority priority)");
		}
		#endif
		// Forced skipping of method Windows.System.Threading.Core.PreallocatedWorkItem.PreallocatedWorkItem(Windows.System.Threading.WorkItemHandler, Windows.System.Threading.WorkItemPriority)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PreallocatedWorkItem( global::Windows.System.Threading.WorkItemHandler handler,  global::Windows.System.Threading.WorkItemPriority priority,  global::Windows.System.Threading.WorkItemOptions options) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.System.Threading.Core.PreallocatedWorkItem", "PreallocatedWorkItem.PreallocatedWorkItem(WorkItemHandler handler, WorkItemPriority priority, WorkItemOptions options)");
		}
		#endif
		// Forced skipping of method Windows.System.Threading.Core.PreallocatedWorkItem.PreallocatedWorkItem(Windows.System.Threading.WorkItemHandler, Windows.System.Threading.WorkItemPriority, Windows.System.Threading.WorkItemOptions)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncAction RunAsync()
		{
			throw new global::System.NotImplementedException("The member IAsyncAction PreallocatedWorkItem.RunAsync() is not implemented in Uno.");
		}
		#endif
	}
}

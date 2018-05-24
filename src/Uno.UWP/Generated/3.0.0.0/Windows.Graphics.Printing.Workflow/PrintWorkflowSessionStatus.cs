#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.Workflow
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum PrintWorkflowSessionStatus 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Started,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Completed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Aborted,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Closed,
		#endif
	}
	#endif
}

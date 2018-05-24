#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Devices.Printers.Extensions
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum Print3DWorkflowDetail 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Unknown,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ModelExceedsPrintBed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UploadFailed,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidMaterialSelection,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidModel,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ModelNotManifold,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		InvalidPrintTicket,
		#endif
	}
	#endif
}

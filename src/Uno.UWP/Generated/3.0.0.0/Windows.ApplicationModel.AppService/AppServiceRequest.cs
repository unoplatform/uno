#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.ApplicationModel.AppService
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AppServiceRequest 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Collections.ValueSet Message
		{
			get
			{
				throw new global::System.NotImplementedException("The member ValueSet AppServiceRequest.Message is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=ValueSet%20AppServiceRequest.Message");
			}
		}
		#endif
		// Forced skipping of method Windows.ApplicationModel.AppService.AppServiceRequest.Message.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.ApplicationModel.AppService.AppServiceResponseStatus> SendResponseAsync( global::Windows.Foundation.Collections.ValueSet message)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<AppServiceResponseStatus> AppServiceRequest.SendResponseAsync(ValueSet message) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CAppServiceResponseStatus%3E%20AppServiceRequest.SendResponseAsync%28ValueSet%20message%29");
		}
		#endif
	}
}

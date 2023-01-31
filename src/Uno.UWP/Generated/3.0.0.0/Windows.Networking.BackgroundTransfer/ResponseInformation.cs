#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Networking.BackgroundTransfer
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ResponseInformation 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri ActualUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri ResponseInformation.ActualUri is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Uri%20ResponseInformation.ActualUri");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IReadOnlyDictionary<string, string> Headers
		{
			get
			{
				throw new global::System.NotImplementedException("The member IReadOnlyDictionary<string, string> ResponseInformation.Headers is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IReadOnlyDictionary%3Cstring%2C%20string%3E%20ResponseInformation.Headers");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool IsResumable
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool ResponseInformation.IsResumable is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=bool%20ResponseInformation.IsResumable");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint StatusCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint ResponseInformation.StatusCode is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20ResponseInformation.StatusCode");
			}
		}
		#endif
		// Forced skipping of method Windows.Networking.BackgroundTransfer.ResponseInformation.IsResumable.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.ResponseInformation.ActualUri.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.ResponseInformation.StatusCode.get
		// Forced skipping of method Windows.Networking.BackgroundTransfer.ResponseInformation.Headers.get
	}
}

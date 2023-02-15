#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Foundation.Diagnostics
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ErrorDetails 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string Description
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ErrorDetails.Description is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20ErrorDetails.Description");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Uri HelpUri
		{
			get
			{
				throw new global::System.NotImplementedException("The member Uri ErrorDetails.HelpUri is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Uri%20ErrorDetails.HelpUri");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string LongDescription
		{
			get
			{
				throw new global::System.NotImplementedException("The member string ErrorDetails.LongDescription is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20ErrorDetails.LongDescription");
			}
		}
		#endif
		// Forced skipping of method Windows.Foundation.Diagnostics.ErrorDetails.Description.get
		// Forced skipping of method Windows.Foundation.Diagnostics.ErrorDetails.LongDescription.get
		// Forced skipping of method Windows.Foundation.Diagnostics.ErrorDetails.HelpUri.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Foundation.IAsyncOperation<global::Windows.Foundation.Diagnostics.ErrorDetails> CreateFromHResultAsync( int errorCode)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<ErrorDetails> ErrorDetails.CreateFromHResultAsync(int errorCode) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CErrorDetails%3E%20ErrorDetails.CreateFromHResultAsync%28int%20errorCode%29");
		}
		#endif
	}
}

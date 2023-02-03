#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.PlayTo
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlayToSourceRequestedEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.PlayTo.PlayToSourceRequest SourceRequest
		{
			get
			{
				throw new global::System.NotImplementedException("The member PlayToSourceRequest PlayToSourceRequestedEventArgs.SourceRequest is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PlayToSourceRequest%20PlayToSourceRequestedEventArgs.SourceRequest");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.PlayTo.PlayToSourceRequestedEventArgs.SourceRequest.get
	}
}

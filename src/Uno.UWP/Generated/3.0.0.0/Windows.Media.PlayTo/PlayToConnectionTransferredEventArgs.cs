#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Media.PlayTo
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PlayToConnectionTransferredEventArgs 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.PlayTo.PlayToSource CurrentSource
		{
			get
			{
				throw new global::System.NotImplementedException("The member PlayToSource PlayToConnectionTransferredEventArgs.CurrentSource is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PlayToSource%20PlayToConnectionTransferredEventArgs.CurrentSource");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Media.PlayTo.PlayToSource PreviousSource
		{
			get
			{
				throw new global::System.NotImplementedException("The member PlayToSource PlayToConnectionTransferredEventArgs.PreviousSource is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PlayToSource%20PlayToConnectionTransferredEventArgs.PreviousSource");
			}
		}
		#endif
		// Forced skipping of method Windows.Media.PlayTo.PlayToConnectionTransferredEventArgs.PreviousSource.get
		// Forced skipping of method Windows.Media.PlayTo.PlayToConnectionTransferredEventArgs.CurrentSource.get
	}
}

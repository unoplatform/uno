#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintPageRange 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int FirstPageNumber
		{
			get
			{
				throw new global::System.NotImplementedException("The member int PrintPageRange.FirstPageNumber is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int LastPageNumber
		{
			get
			{
				throw new global::System.NotImplementedException("The member int PrintPageRange.LastPageNumber is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PrintPageRange( int firstPage,  int lastPage) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintPageRange", "PrintPageRange.PrintPageRange(int firstPage, int lastPage)");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.PrintPageRange.PrintPageRange(int, int)
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PrintPageRange( int page) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintPageRange", "PrintPageRange.PrintPageRange(int page)");
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.PrintPageRange.PrintPageRange(int)
		// Forced skipping of method Windows.Graphics.Printing.PrintPageRange.FirstPageNumber.get
		// Forced skipping of method Windows.Graphics.Printing.PrintPageRange.LastPageNumber.get
	}
}

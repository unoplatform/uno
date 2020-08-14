#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PrintPageRangeOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AllowCustomSetOfPages
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PrintPageRangeOptions.AllowCustomSetOfPages is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintPageRangeOptions", "bool PrintPageRangeOptions.AllowCustomSetOfPages");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AllowCurrentPage
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PrintPageRangeOptions.AllowCurrentPage is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintPageRangeOptions", "bool PrintPageRangeOptions.AllowCurrentPage");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool AllowAllPages
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool PrintPageRangeOptions.AllowAllPages is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Graphics.Printing.PrintPageRangeOptions", "bool PrintPageRangeOptions.AllowAllPages");
			}
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.PrintPageRangeOptions.AllowAllPages.set
		// Forced skipping of method Windows.Graphics.Printing.PrintPageRangeOptions.AllowAllPages.get
		// Forced skipping of method Windows.Graphics.Printing.PrintPageRangeOptions.AllowCurrentPage.set
		// Forced skipping of method Windows.Graphics.Printing.PrintPageRangeOptions.AllowCurrentPage.get
		// Forced skipping of method Windows.Graphics.Printing.PrintPageRangeOptions.AllowCustomSetOfPages.set
		// Forced skipping of method Windows.Graphics.Printing.PrintPageRangeOptions.AllowCustomSetOfPages.get
	}
}

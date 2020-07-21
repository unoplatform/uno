#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.OptionDetails
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IPrintTextOptionDetails : global::Windows.Graphics.Printing.OptionDetails.IPrintOptionDetails
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		uint MaxCharacters
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.IPrintTextOptionDetails.MaxCharacters.get
	}
}

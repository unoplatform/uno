#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Graphics.Printing.OptionDetails
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IPrintCustomOptionDetails : global::Windows.Graphics.Printing.OptionDetails.IPrintOptionDetails
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string DisplayName
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.IPrintCustomOptionDetails.DisplayName.set
		// Forced skipping of method Windows.Graphics.Printing.OptionDetails.IPrintCustomOptionDetails.DisplayName.get
	}
}

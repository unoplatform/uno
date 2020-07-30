#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Globalization.NumberFormatting
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface ISignificantDigitsOption 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int SignificantDigits
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Globalization.NumberFormatting.ISignificantDigitsOption.SignificantDigits.get
		// Forced skipping of method Windows.Globalization.NumberFormatting.ISignificantDigitsOption.SignificantDigits.set
	}
}

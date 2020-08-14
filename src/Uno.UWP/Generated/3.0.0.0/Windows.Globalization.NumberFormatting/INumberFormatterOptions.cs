#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Globalization.NumberFormatting
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial interface INumberFormatterOptions 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int FractionDigits
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string GeographicRegion
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		int IntegerDigits
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsDecimalPointAlwaysDisplayed
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		bool IsGrouped
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		global::System.Collections.Generic.IReadOnlyList<string> Languages
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string NumeralSystem
		{
			get;
			set;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string ResolvedGeographicRegion
		{
			get;
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		string ResolvedLanguage
		{
			get;
		}
		#endif
		// Forced skipping of method Windows.Globalization.NumberFormatting.INumberFormatterOptions.Languages.get
		// Forced skipping of method Windows.Globalization.NumberFormatting.INumberFormatterOptions.GeographicRegion.get
		// Forced skipping of method Windows.Globalization.NumberFormatting.INumberFormatterOptions.IntegerDigits.get
		// Forced skipping of method Windows.Globalization.NumberFormatting.INumberFormatterOptions.IntegerDigits.set
		// Forced skipping of method Windows.Globalization.NumberFormatting.INumberFormatterOptions.FractionDigits.get
		// Forced skipping of method Windows.Globalization.NumberFormatting.INumberFormatterOptions.FractionDigits.set
		// Forced skipping of method Windows.Globalization.NumberFormatting.INumberFormatterOptions.IsGrouped.get
		// Forced skipping of method Windows.Globalization.NumberFormatting.INumberFormatterOptions.IsGrouped.set
		// Forced skipping of method Windows.Globalization.NumberFormatting.INumberFormatterOptions.IsDecimalPointAlwaysDisplayed.get
		// Forced skipping of method Windows.Globalization.NumberFormatting.INumberFormatterOptions.IsDecimalPointAlwaysDisplayed.set
		// Forced skipping of method Windows.Globalization.NumberFormatting.INumberFormatterOptions.NumeralSystem.get
		// Forced skipping of method Windows.Globalization.NumberFormatting.INumberFormatterOptions.NumeralSystem.set
		// Forced skipping of method Windows.Globalization.NumberFormatting.INumberFormatterOptions.ResolvedLanguage.get
		// Forced skipping of method Windows.Globalization.NumberFormatting.INumberFormatterOptions.ResolvedGeographicRegion.get
	}
}

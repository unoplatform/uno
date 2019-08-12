#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Globalization.PhoneNumberFormatting
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PhoneNumberFormatter 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public PhoneNumberFormatter() 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Globalization.PhoneNumberFormatting.PhoneNumberFormatter", "PhoneNumberFormatter.PhoneNumberFormatter()");
		}
		#endif
		// Forced skipping of method Windows.Globalization.PhoneNumberFormatting.PhoneNumberFormatter.PhoneNumberFormatter()
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Format( global::Windows.Globalization.PhoneNumberFormatting.PhoneNumberInfo number)
		{
			throw new global::System.NotImplementedException("The member string PhoneNumberFormatter.Format(PhoneNumberInfo number) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string Format( global::Windows.Globalization.PhoneNumberFormatting.PhoneNumberInfo number,  global::Windows.Globalization.PhoneNumberFormatting.PhoneNumberFormat numberFormat)
		{
			throw new global::System.NotImplementedException("The member string PhoneNumberFormatter.Format(PhoneNumberInfo number, PhoneNumberFormat numberFormat) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string FormatPartialString( string number)
		{
			throw new global::System.NotImplementedException("The member string PhoneNumberFormatter.FormatPartialString(string number) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string FormatString( string number)
		{
			throw new global::System.NotImplementedException("The member string PhoneNumberFormatter.FormatString(string number) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  string FormatStringWithLeftToRightMarkers( string number)
		{
			throw new global::System.NotImplementedException("The member string PhoneNumberFormatter.FormatStringWithLeftToRightMarkers(string number) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static void TryCreate( string regionCode, out global::Windows.Globalization.PhoneNumberFormatting.PhoneNumberFormatter phoneNumber)
		{
			throw new global::System.NotImplementedException("The member void PhoneNumberFormatter.TryCreate(string regionCode, out PhoneNumberFormatter phoneNumber) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static int GetCountryCodeForRegion( string regionCode)
		{
			throw new global::System.NotImplementedException("The member int PhoneNumberFormatter.GetCountryCodeForRegion(string regionCode) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static string GetNationalDirectDialingPrefixForRegion( string regionCode,  bool stripNonDigit)
		{
			throw new global::System.NotImplementedException("The member string PhoneNumberFormatter.GetNationalDirectDialingPrefixForRegion(string regionCode, bool stripNonDigit) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public static string WrapWithLeftToRightMarkers( string number)
		{
			throw new global::System.NotImplementedException("The member string PhoneNumberFormatter.WrapWithLeftToRightMarkers(string number) is not implemented in Uno.");
		}
		#endif
	}
}

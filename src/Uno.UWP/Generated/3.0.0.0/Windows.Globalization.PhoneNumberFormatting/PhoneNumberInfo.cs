#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Globalization.PhoneNumberFormatting
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class PhoneNumberInfo : global::Windows.Foundation.IStringable
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int CountryCode
		{
			get
			{
				throw new global::System.NotImplementedException("The member int PhoneNumberInfo.CountryCode is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20PhoneNumberInfo.CountryCode");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string PhoneNumber
		{
			get
			{
				throw new global::System.NotImplementedException("The member string PhoneNumberInfo.PhoneNumber is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PhoneNumberInfo.PhoneNumber");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public PhoneNumberInfo( string number) 
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.Globalization.PhoneNumberFormatting.PhoneNumberInfo", "PhoneNumberInfo.PhoneNumberInfo(string number)");
		}
		#endif
		// Forced skipping of method Windows.Globalization.PhoneNumberFormatting.PhoneNumberInfo.PhoneNumberInfo(string)
		// Forced skipping of method Windows.Globalization.PhoneNumberFormatting.PhoneNumberInfo.CountryCode.get
		// Forced skipping of method Windows.Globalization.PhoneNumberFormatting.PhoneNumberInfo.PhoneNumber.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int GetLengthOfGeographicalAreaCode()
		{
			throw new global::System.NotImplementedException("The member int PhoneNumberInfo.GetLengthOfGeographicalAreaCode() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20PhoneNumberInfo.GetLengthOfGeographicalAreaCode%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string GetNationalSignificantNumber()
		{
			throw new global::System.NotImplementedException("The member string PhoneNumberInfo.GetNationalSignificantNumber() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PhoneNumberInfo.GetNationalSignificantNumber%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int GetLengthOfNationalDestinationCode()
		{
			throw new global::System.NotImplementedException("The member int PhoneNumberInfo.GetLengthOfNationalDestinationCode() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20PhoneNumberInfo.GetLengthOfNationalDestinationCode%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Globalization.PhoneNumberFormatting.PredictedPhoneNumberKind PredictNumberKind()
		{
			throw new global::System.NotImplementedException("The member PredictedPhoneNumberKind PhoneNumberInfo.PredictNumberKind() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PredictedPhoneNumberKind%20PhoneNumberInfo.PredictNumberKind%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  string GetGeographicRegionCode()
		{
			throw new global::System.NotImplementedException("The member string PhoneNumberInfo.GetGeographicRegionCode() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PhoneNumberInfo.GetGeographicRegionCode%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Globalization.PhoneNumberFormatting.PhoneNumberMatchResult CheckNumberMatch( global::Windows.Globalization.PhoneNumberFormatting.PhoneNumberInfo otherNumber)
		{
			throw new global::System.NotImplementedException("The member PhoneNumberMatchResult PhoneNumberInfo.CheckNumberMatch(PhoneNumberInfo otherNumber) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PhoneNumberMatchResult%20PhoneNumberInfo.CheckNumberMatch%28PhoneNumberInfo%20otherNumber%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public override string ToString()
		{
			throw new global::System.NotImplementedException("The member string PhoneNumberInfo.ToString() is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=string%20PhoneNumberInfo.ToString%28%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Globalization.PhoneNumberFormatting.PhoneNumberParseResult TryParse( string input, out global::Windows.Globalization.PhoneNumberFormatting.PhoneNumberInfo phoneNumber)
		{
			throw new global::System.NotImplementedException("The member PhoneNumberParseResult PhoneNumberInfo.TryParse(string input, out PhoneNumberInfo phoneNumber) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PhoneNumberParseResult%20PhoneNumberInfo.TryParse%28string%20input%2C%20out%20PhoneNumberInfo%20phoneNumber%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.Globalization.PhoneNumberFormatting.PhoneNumberParseResult TryParse( string input,  string regionCode, out global::Windows.Globalization.PhoneNumberFormatting.PhoneNumberInfo phoneNumber)
		{
			throw new global::System.NotImplementedException("The member PhoneNumberParseResult PhoneNumberInfo.TryParse(string input, string regionCode, out PhoneNumberInfo phoneNumber) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=PhoneNumberParseResult%20PhoneNumberInfo.TryParse%28string%20input%2C%20string%20regionCode%2C%20out%20PhoneNumberInfo%20phoneNumber%29");
		}
		#endif
	}
}

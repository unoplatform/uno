#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Text.Core
{
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public   enum CoreTextInputScope 
	{
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Default,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Url,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FilePath,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FileName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EmailUserName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EmailAddress,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		UserName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PersonalFullName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PersonalNamePrefix,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PersonalGivenName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PersonalMiddleName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PersonalSurname,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PersonalNameSuffix,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Address,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AddressPostalCode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AddressStreet,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AddressStateOrProvince,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AddressCity,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AddressCountryName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AddressCountryShortName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CurrencyAmountAndSymbol,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CurrencyAmount,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Date,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DateMonth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DateDay,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DateYear,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DateMonthName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		DateDayName,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Number,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SingleCharacter,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Password,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TelephoneNumber,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TelephoneCountryCode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TelephoneAreaCode,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TelephoneLocalNumber,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Time,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TimeHour,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		TimeMinuteOrSecond,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NumberFullWidth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AlphanumericHalfWidth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		AlphanumericFullWidth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		CurrencyChinese,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Bopomofo,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hiragana,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		KatakanaHalfWidth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		KatakanaFullWidth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Hanja,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HangulHalfWidth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		HangulFullWidth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Search,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Formula,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		SearchIncremental,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChineseHalfWidth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChineseFullWidth,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NativeScript,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Text,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Chat,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		NameOrPhoneNumber,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		EmailUserNameOrAddress,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Private,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Maps,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PasswordNumeric,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		FormulaNumber,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		ChatWithoutEmoji,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		Digits,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PinNumeric,
		#endif
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		PinAlphanumeric,
		#endif
	}
	#endif
}

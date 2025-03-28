using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml.Input
{
	/// <summary>
	/// Specifies a particular named input mode (<see cref="InputScopeName"/>) used to populate an <see cref="InputScope"/>.
	/// </summary>
	public enum InputScopeNameValue
	{
		/// <summary>
		/// No input scope is applied.
		/// </summary>
		Default = 0,
		/// <summary>
		/// Indicates a Uniform Resource Identifier (URI). This can include URL, File, or File Transfer Protocol (FTP) formats.
		/// </summary>
		Url = 1,
		/// <summary>
		/// Input scope is intended for working with a Simple Mail Transport Protocol (SMTP) form e-mail address (accountnamehost).
		/// </summary>
		EmailSmtpAddress = 5,
		/// <summary>
		/// Input scope is intended for working with a complete personal name.
		/// </summary>
		PersonalFullName = 7,
		/// <summary>
		/// Input scope is intended for working with amount and symbol of currency.
		/// </summary>
		CurrencyAmountAndSymbol = 20,
		/// <summary>
		/// Input scope is intended for working with a currency amount (no currency symbol).
		/// </summary>
		CurrencyAmount = 21,
		/// <summary>
		/// Input scope is intended for working with a numeric month of the year.
		/// </summary>
		DateMonthNumber = 23,
		/// <summary>
		/// Input scope is intended for working with a numeric day of the month.
		/// </summary>
		DateDayNumber = 24,
		/// <summary>
		/// Input scope is intended for working with a numeric year.
		/// </summary>
		DateYear = 25,
		/// <summary>
		/// Input scope is intended for working with a collection of numbers.
		/// </summary>
		Digits = 28,
		/// <summary>
		/// Input scope is intended for working with digits 0-9.
		/// </summary>
		Number = 29,
		/// <summary>
		/// Input scope is intended for working with an alphanumeric password, including other symbols, such as punctuation and mathematical symbols.
		/// </summary>
		Password = 31,
		/// <summary>
		/// Input scope is intended for working with telephone numbers.
		/// </summary>
		TelephoneNumber = 32,
		/// <summary>
		/// Input scope is intended for working with a numeric telephone country code.
		/// </summary>
		TelephoneCountryCode = 33,
		/// <summary>
		/// Input scope is intended for working with a numeric telephone area code.
		/// </summary>
		TelephoneAreaCode = 34,
		/// <summary>
		/// Input scope is intended for working with a local telephone number.
		/// </summary>
		TelephoneLocalNumber = 35,
		/// <summary>
		/// Input scope is intended for working with a numeric hour of the day.
		/// </summary>
		TimeHour = 37,
		/// <summary>
		/// Input scope is intended for working with a numeric minute of the hour, or second of the minute.
		/// </summary>
		TimeMinutesOrSeconds = 38,
		/// <summary>
		/// Input scope is intended for full-width number characters.
		/// </summary>
		NumberFullWidth = 39,
		/// <summary>
		/// Input scope is intended for alphanumeric half-width characters.
		/// </summary>
		AlphanumericHalfWidth = 40,
		/// <summary>
		/// Input scope is intended for alphanumeric full-width characters.
		/// </summary>
		AlphanumericFullWidth = 41,
		/// <summary>
		/// Input scope is intended for Hiragana characters.
		/// </summary>
		Hiragana = 44,
		/// <summary>
		/// Input scope is intended for Katakana half-width characters.
		/// </summary>
		KatakanaHalfWidth = 45,
		/// <summary>
		/// Input scope is intended for Katakana full-width characters.
		/// </summary>
		KatakanaFullWidth = 46,
		/// <summary>
		/// Input scope is intended for Hanja characters.
		/// </summary>
		Hanja = 47,
		/// <summary>
		/// Input scope is intended for Hangul half-width characters.
		/// </summary>
		HangulHalfWidth = 48,
		/// <summary>
		/// Input scope is intended for Hangul full-width characters.
		/// </summary>
		HangulFullWidth = 49,
		/// <summary>
		/// Input scope is intended for search strings.
		/// </summary>
		Search = 50,
		/// <summary>
		/// Input scope is intended for spreadsheet formula strings.
		/// </summary>
		Formula = 51,
		/// <summary>
		/// Input scope is intended for search boxes where incremental results are displayed as the user types.
		/// </summary>
		SearchIncremental = 52,
		/// <summary>
		/// Input scope is intended for Chinese half-width characters.
		/// </summary>
		ChineseHalfWidth = 53,
		/// <summary>
		/// Input scope is intended for Chinese full-width characters.
		/// </summary>
		ChineseFullWidth = 54,
		/// <summary>
		/// Input scope is intended for native script.
		/// </summary>
		NativeScript = 55,
		/// <summary>
		/// Input scope is intended for working with text.
		/// </summary>
		Text = 57,
		/// <summary>
		/// Input scope is intended for chat strings.
		/// </summary>
		Chat = 58,
		/// <summary>
		/// Input scope is intended for working with a name or telephone number.
		/// </summary>
		NameOrPhoneNumber = 59,
		/// <summary>
		/// Input scope is intended for working with an email name or full email address.
		/// </summary>
		EmailNameOrAddress = 60,
		/// <summary>
		/// Input scope is intended for working with a map location.
		/// </summary>
		Maps = 62,
		/// <summary>
		/// Expected input is a numeric password, or PIN.
		/// </summary>
		NumericPassword = 63,
		/// <summary>
		/// Expected input is a numeric PIN.
		/// </summary>
		NumericPin = 64,
		/// <summary>
		/// Expected input is an alphanumeric PIN.
		/// </summary>
		AlphanumericPin = 65,
		/// <summary>
		/// Expected input is a mathematical formula. Advises input processors to show the number page.
		/// </summary>
		FormulaNumber = 67,
		/// <summary>
		/// Expected input does not include emoji. Advises input processors to not show the emoji key.
		/// </summary>
		ChatWithoutEmoji = 68
	}
}

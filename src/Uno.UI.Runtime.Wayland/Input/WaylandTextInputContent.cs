#nullable enable

using Microsoft.UI.Xaml.Input;
using Hint = Uno.UI.Runtime.Wayland.Protocols.ZwpTextInputV3.ContentHintValues;
using Purpose = Uno.UI.Runtime.Wayland.Protocols.ZwpTextInputV3.ContentPurposeValues;

namespace Uno.UI.Runtime.Wayland;

internal static class WaylandTextInputContent
{
	internal static (uint Hint, uint Purpose) Get(InputScopeNameValue scope, bool prediction, bool spelling, bool multiline)
	{
		var purpose = scope switch
		{
			InputScopeNameValue.Url => Purpose.Url,
			InputScopeNameValue.EmailSmtpAddress or InputScopeNameValue.EmailNameOrAddress => Purpose.Email,
			InputScopeNameValue.PersonalFullName => Purpose.Name,
			InputScopeNameValue.Digits => Purpose.Digits,
			InputScopeNameValue.Number or InputScopeNameValue.NumberFullWidth or InputScopeNameValue.CurrencyAmount or InputScopeNameValue.CurrencyAmountAndSymbol or InputScopeNameValue.FormulaNumber => Purpose.Number,
			InputScopeNameValue.TelephoneNumber or InputScopeNameValue.TelephoneCountryCode or InputScopeNameValue.TelephoneAreaCode or InputScopeNameValue.TelephoneLocalNumber => Purpose.Phone,
			InputScopeNameValue.Password or InputScopeNameValue.AlphanumericPin => Purpose.Password,
			InputScopeNameValue.NumericPassword or InputScopeNameValue.NumericPin => Purpose.Pin,
			InputScopeNameValue.DateMonthNumber or InputScopeNameValue.DateDayNumber or InputScopeNameValue.DateYear => Purpose.Date,
			InputScopeNameValue.TimeHour or InputScopeNameValue.TimeMinutesOrSeconds => Purpose.Time,
			_ => Purpose.Normal
		};
		var hints = (prediction ? Hint.Completion : Hint.None)
			| (spelling ? Hint.Spellcheck : Hint.None)
			| (multiline ? Hint.Multiline : Hint.None);
		if (purpose is Purpose.Password or Purpose.Pin)
		{
			hints = (hints & ~(Hint.Completion | Hint.Spellcheck)) | Hint.HiddenText | Hint.SensitiveData;
		}
		return ((uint)hints, (uint)purpose);
	}
}

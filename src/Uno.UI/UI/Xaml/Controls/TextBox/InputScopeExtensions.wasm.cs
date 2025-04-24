using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Xaml.Controls;

internal static class InputScopeExtensions
{
	internal static string ToInputModeValue(this InputScope scope)
	{
		// https://developer.mozilla.org/en-US/docs/Web/HTML/Global_attributes/inputmode
		// Allowed html values: none, text (default value), decimal, numeric, tel, search, email, url.
		var scopeNameValue = scope.GetFirstInputScopeNameValue();
		var inputModeValue = scopeNameValue switch
		{
			InputScopeNameValue.CurrencyAmount => "decimal",
			InputScopeNameValue.CurrencyAmountAndSymbol => "decimal",

			InputScopeNameValue.NumericPin => "numeric",
			InputScopeNameValue.Digits => "numeric",
			InputScopeNameValue.Number => "numeric",
			InputScopeNameValue.NumberFullWidth => "numeric",
			InputScopeNameValue.DateDayNumber => "numeric",
			InputScopeNameValue.DateMonthNumber => "numeric",

			InputScopeNameValue.TelephoneNumber => "tel",
			InputScopeNameValue.TelephoneLocalNumber => "tel",

			InputScopeNameValue.Search => "search",
			InputScopeNameValue.SearchIncremental => "search",

			InputScopeNameValue.EmailNameOrAddress => "email",
			InputScopeNameValue.EmailSmtpAddress => "email",

			InputScopeNameValue.Url => "url",

			_ => "text"
		};

		return inputModeValue;
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Text;
using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls
{
	internal static class InputScopeHelper
	{
		internal static InputTypes ConvertToCapitalization(InputTypes types, InputScope value)
		{
			switch (value.GetFirstInputScopeNameValue())
			{
				case InputScopeNameValue.Default:
				case InputScopeNameValue.PersonalFullName:
					return types | InputTypes.TextFlagCapSentences;
				default:
					return types;
			}
		}

		internal static InputTypes ConvertToRemoveSuggestions(InputTypes value, bool forceRemove)
		{
			value |= InputTypes.TextFlagNoSuggestions;

			if (forceRemove && (value & InputTypes.MaskVariation) == 0)
			{
				value |= InputTypes.TextVariationVisiblePassword;
			}
			return value;
		}

		internal static InputTypes ConvertInputScope(InputScope value)
		{
			var firstInputScope = value.GetFirstInputScopeNameValue();

			switch (firstInputScope)
			{
				case InputScopeNameValue.Number:
				case InputScopeNameValue.NumericPin:
					return InputTypes.ClassNumber;

				case InputScopeNameValue.NumberFullWidth:
					// Android has no InputType that accepts numbers and punctuation other than "Phone" and "Text".
					// "Phone" is the closest one to what we're looking for here, but phone-specific keys could be confusing in some cases.
					return InputTypes.ClassPhone;

				case InputScopeNameValue.CurrencyAmount:
					return InputTypes.ClassNumber | InputTypes.NumberFlagDecimal;

				case InputScopeNameValue.Url:
					return InputTypes.ClassText | InputTypes.TextVariationUri;

				case InputScopeNameValue.TelephoneNumber:
					return InputTypes.ClassPhone;

				case InputScopeNameValue.Search:
					return InputTypes.ClassText;

				case InputScopeNameValue.EmailNameOrAddress:
				case InputScopeNameValue.EmailSmtpAddress:
					return InputTypes.ClassText | InputTypes.TextVariationEmailAddress;

				case InputScopeNameValue.Default:
				default:
					return InputTypes.ClassText;
			}
		}

		internal static InputScopeNameValue ConvertInputScope(InputTypes inputType)
		{
			switch (inputType)
			{
				default:
					return InputScopeNameValue.Default;

				case InputTypes.ClassNumber:
					return InputScopeNameValue.Number;

				case InputTypes.TextVariationUri:
					return InputScopeNameValue.Url;

				case InputTypes.ClassPhone:
					// Could also be InputScope.NumberAndPunctuation, beware.
					return InputScopeNameValue.TelephoneNumber;

				case InputTypes.ClassText:
					return InputScopeNameValue.Search;

				case InputTypes.TextVariationEmailAddress:
					return InputScopeNameValue.EmailSmtpAddress;

				case InputTypes.NumberFlagDecimal:
					return InputScopeNameValue.CurrencyAmount;
			}
		}
	}
}

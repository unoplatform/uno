using System;
using System.Collections.Generic;
using System.Text;
using Foundation;
using UIKit;
using CoreGraphics;
using Windows.UI.Xaml.Input;
using System.Linq;

namespace Windows.UI.Xaml.Controls
{
	internal static class InputScopeHelper
	{
		public static UITextAutocapitalizationType ConvertInputScopeToCapitalization(InputScope value)
		{
			switch (value.GetFirstInputScopeNameValue())
			{
				case InputScopeNameValue.PersonalFullName:
					return UITextAutocapitalizationType.Sentences;

				default:
					return UITextAutocapitalizationType.None;
			}
		}

		public static UIKeyboardType ConvertInputScopeToKeyboardType(InputScope value)
		{
			switch (value.GetFirstInputScopeNameValue())
			{
				default:
					return UIKeyboardType.Default;

				case InputScopeNameValue.Number:
				case InputScopeNameValue.DateDayNumber:
				case InputScopeNameValue.DateMonthNumber:
				case InputScopeNameValue.DateYear:
				case InputScopeNameValue.Digits:
					return UIKeyboardType.NumberPad;

				case InputScopeNameValue.NameOrPhoneNumber:
					return UIKeyboardType.NamePhonePad;

				case InputScopeNameValue.NumberFullWidth:
				case InputScopeNameValue.NumericPin:
					return UIKeyboardType.NumbersAndPunctuation;

				case InputScopeNameValue.CurrencyAmount:
					return UIKeyboardType.DecimalPad;

				case InputScopeNameValue.Url:
					return UIKeyboardType.Url;

				case InputScopeNameValue.TelephoneNumber:
					return UIKeyboardType.PhonePad;

				case InputScopeNameValue.Search:
					return UIKeyboardType.WebSearch;

				case InputScopeNameValue.EmailNameOrAddress:
				case InputScopeNameValue.EmailSmtpAddress:
					return UIKeyboardType.EmailAddress;
			}
		}

		public static InputScopeNameValue ConvertInputScope(UIKeyboardType keyboardType)
		{
			switch (keyboardType)
			{
				default:
				case UIKeyboardType.Default:
					return InputScopeNameValue.Default;

				case UIKeyboardType.NumberPad:
					return InputScopeNameValue.Number;

				case UIKeyboardType.DecimalPad:
				case UIKeyboardType.NumbersAndPunctuation:
					return InputScopeNameValue.NumberFullWidth;

				case UIKeyboardType.Url:
					return InputScopeNameValue.Url;

				case UIKeyboardType.NamePhonePad:
				case UIKeyboardType.PhonePad:
					return InputScopeNameValue.TelephoneNumber;

				case UIKeyboardType.EmailAddress:
					return InputScopeNameValue.EmailSmtpAddress;

				case UIKeyboardType.Twitter:
				case UIKeyboardType.WebSearch:
					return InputScopeNameValue.Search;
			}
		}
	}
}

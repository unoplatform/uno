#if !NETFX_CORE
using System;

using Uno.Extensions;
using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.Foundation;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI;
using Uno.UI.Extensions;
using System.Text.RegularExpressions;

#if XAMARIN_ANDROID
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
#endif

#if HAS_UNO_WINUI
using Microsoft.UI.Text;
using FontWeights = Windows.UI.Text.FontWeights;
using FontWeight = Windows.UI.Text.FontWeight;
#else
using Windows.UI.Text;
using FontWeights = Windows.UI.Text.FontWeights;
using FontWeight = Windows.UI.Text.FontWeight;
#endif

namespace Uno.UI.DataBinding
{
	internal static partial class BindingPropertyHelper
	{
		/// <summary>
		/// Converts the input to the outputType using a fast conversion, for known system types.
		/// </summary>
		/// <param name="outputType">The target type</param>
		/// <param name="input">The input value to use</param>
		/// <param name="output">The input value converted to the <paramref name="outputType"/>.</param>
		/// <returns>True if the conversion succeeded, otherwise false.</returns>
		/// <remarks>
		/// This is a fast path conversion that avoids going through the TypeConverter
		/// infrastructure for known system types.
		/// </remarks>
		private static bool FastConvert(Type outputType, object input, out object output)
		{
			output = null;

			if (input is string stringInput &&
				FastStringConvert(outputType, stringInput, ref output))
			{
				return true;
			}

			if (FastNumberConvert(outputType, input, ref output))
			{
				return true;
			}

			return input switch
			{
				Enum _ => FastEnumConvert(outputType, input, ref output),
				bool boolInput => FastBooleanConvert(outputType, boolInput, ref output),
				Windows.UI.Color color => FastColorConvert(outputType, color, ref output),
				SolidColorBrush solidColorBrush => FastSolidColorBrushConvert(outputType, solidColorBrush, ref output),
				ColorOffset colorOffsetInput => FastColorOffsetConvert(outputType, colorOffsetInput, ref output),
				Thickness thicknessInput => FastThicknessConvert(outputType, thicknessInput, ref output),
				_ => false
			};
		}

		private static bool FastBooleanConvert(Type outputType, bool boolInput, ref object output)
		{
			if (outputType == typeof(Visibility))
			{
				output = boolInput ? Visibility.Visible : Visibility.Collapsed;
				return true;
			}

			return false;
		}

		private static bool FastEnumConvert(Type outputType, object input, ref object output)
		{
			if (outputType == typeof(string))
			{
				output = input.ToString();
				return true;
			}

			return false;
		}

		private static bool FastColorOffsetConvert(Type outputType, ColorOffset input, ref object output)
		{
			if (outputType == typeof(Windows.UI.Color))
			{
				output = (Windows.UI.Color)input;
				return true;
			}

			return false;
		}

		private static bool FastColorConvert(Type outputType, Windows.UI.Color color, ref object output)
		{
			if (outputType == typeof(SolidColorBrush))
			{
				output = new SolidColorBrush(color);
				return true;
			}

			return false;
		}

		private static bool FastSolidColorBrushConvert(Type outputType, SolidColorBrush solidColorBrush,
			ref object output)
		{
			if (outputType == typeof(Windows.UI.Color) || outputType == typeof(Windows.UI.Color?))
			{
				output = solidColorBrush.Color;
				return true;
			}

			return false;
		}

		private static bool FastThicknessConvert(Type outputType, Thickness thickness, ref object output)
		{
			if (outputType == typeof(double))
			{
				if (thickness.IsUniform())
				{
					output = thickness.Left;
					return true;
				}

				// TODO: test what Windows does in non-uniform case
			}

			return false;
		}

		private static bool FastNumberConvert(Type outputType, object input, ref object output)
		{
			if (input is double doubleInput)
			{
				if (outputType == typeof(float))
				{
					output = (float)doubleInput;
					return true;
				}
				if (outputType == typeof(TimeSpan))
				{
					output = TimeSpan.FromSeconds(doubleInput);
					return true;
				}
				if (outputType == typeof(GridLength))
				{
					output = GridLengthHelper.FromPixels(doubleInput);
					return true;
				}
			}

			if (input is int intInput)
			{
				if (outputType == typeof(float))
				{
					output = (float)intInput;
					return true;
				}
				if (outputType == typeof(TimeSpan))
				{
					output = TimeSpan.FromSeconds(intInput);
					return true;
				}
				if (outputType == typeof(GridLength))
				{
					output = GridLengthHelper.FromPixels(intInput);
					return true;
				}
			}

			return false;
		}

		private static bool FastStringConvert(Type outputType, string input, ref object output)
		{
			if (FastStringToVisibilityConvert(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToHorizontalAlignmentConvert(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToVerticalAlignmentConvert(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToBrushConvert(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToDoubleConvert(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToSingleConvert(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToThicknessConvert(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToGridLengthConvert(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToOrientationConvert(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToColorConvert(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToTextAlignmentConvert(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToImageSource(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToFontWeightConvert(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToFontFamilyConvert(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToIntegerConvert(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToPointF(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToPoint(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToMatrix(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToDuration(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToRepeatBehavior(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToKeyTime(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToKeySpline(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToPointCollection(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToPath(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToInputScope(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToToolTip(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToIconElement(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToUriConvert(outputType, input, ref output))
			{
				return true;
			}

			if (FastStringToTypeConvert(outputType, input, ref output))
			{
				return true;
			}

			// Fallback for Enums. Leave it at the end.
			if (outputType.IsEnum)
			{
				output = Enum.Parse(outputType, input, true);
				return true;
			}

			return false;
		}

		private static bool FastStringToIconElement(Type outputType, string input, ref object output)
		{
			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_IconElement_Available
				&& outputType == typeof(Windows.UI.Xaml.Controls.IconElement))
			{
				output = (Windows.UI.Xaml.Controls.IconElement)input;
				return true;
			}

			return false;
		}

		private static bool FastStringToInputScope(Type outputType, string input, ref object output)
		{
			if (outputType != typeof(Windows.UI.Xaml.Input.InputScope)) return false;

			var trimmed = input.AsSpan().Trim();
			Windows.UI.Xaml.Input.InputScopeNameValue nameValue;
			if (trimmed.Equals("0", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("default", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Default;
			}
			else if (trimmed.Equals("1", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("url", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Url;
			}
			else if (trimmed.Equals("5", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("emailsmtpaddress", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.EmailSmtpAddress;
			}
			else if (trimmed.Equals("7", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("personalfullname", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.PersonalFullName;
			}
			else if (trimmed.Equals("20", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("currencyamountandsymbol", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.CurrencyAmountAndSymbol;
			}
			else if (trimmed.Equals("21", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("currencyamount", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.CurrencyAmount;
			}
			else if (trimmed.Equals("23", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("datemonthnumber", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.DateMonthNumber;
			}
			else if (trimmed.Equals("24", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("datedaynumber", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.DateDayNumber;
			}
			else if (trimmed.Equals("25", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("dateyear", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.DateYear;
			}
			else if (trimmed.Equals("28", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("digits", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Digits;
			}
			else if (trimmed.Equals("29", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("number", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Number;
			}
			else if (trimmed.Equals("31", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("password", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Password;
			}
			else if (trimmed.Equals("32", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("telephonenumber", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.TelephoneNumber;
			}
			else if (trimmed.Equals("33", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("telephonecountrycode", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.TelephoneCountryCode;
			}
			else if (trimmed.Equals("34", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("telephoneareacode", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.TelephoneAreaCode;
			}
			else if (trimmed.Equals("35", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("telephonelocalnumber", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.TelephoneLocalNumber;
			}
			else if (trimmed.Equals("37", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("timehour", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.TimeHour;
			}
			else if (trimmed.Equals("38", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("timeminutesorseconds", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.TimeMinutesOrSeconds;
			}
			else if (trimmed.Equals("39", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("numberfullwidth", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.NumberFullWidth;
			}
			else if (trimmed.Equals("40", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("alphanumerichalfwidth", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.AlphanumericHalfWidth;
			}
			else if (trimmed.Equals("41", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("alphanumericfullwidth", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.AlphanumericFullWidth;
			}
			else if (trimmed.Equals("44", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("hiragana", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Hiragana;
			}
			else if (trimmed.Equals("45", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("katakanahalfwidth", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.KatakanaHalfWidth;
			}
			else if (trimmed.Equals("46", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("katakanafullwidth", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.KatakanaFullWidth;
			}
			else if (trimmed.Equals("47", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("hanja", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Hanja;
			}
			else if (trimmed.Equals("48", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("hangulhalfwidth", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.HangulHalfWidth;
			}
			else if (trimmed.Equals("49", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("hangulfullwidth", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.HangulFullWidth;
			}
			else if (trimmed.Equals("50", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("search", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Search;
			}
			else if (trimmed.Equals("51", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("formula", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Formula;
			}
			else if (trimmed.Equals("52", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("searchincremental", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.SearchIncremental;
			}
			else if (trimmed.Equals("53", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("chinesehalfwidth", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.ChineseHalfWidth;
			}
			else if (trimmed.Equals("54", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("chinesefullwidth", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.ChineseFullWidth;
			}
			else if (trimmed.Equals("55", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("nativescript", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.NativeScript;
			}
			else if (trimmed.Equals("57", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("text", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Text;
			}
			else if (trimmed.Equals("58", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("chat", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Chat;
			}
			else if (trimmed.Equals("59", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("nameorphonenumber", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.NameOrPhoneNumber;
			}
			else if (trimmed.Equals("60", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("emailnameoraddress", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.EmailNameOrAddress;
			}
			else if (trimmed.Equals("62", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("maps", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.Maps;
			}
			else if (trimmed.Equals("63", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("numericpassword", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.NumericPassword;
			}
			else if (trimmed.Equals("64", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("numericpin", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.NumericPin;
			}
			else if (trimmed.Equals("65", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("alphanumericpin", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.AlphanumericPin;
			}
			else if (trimmed.Equals("67", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("formulanumber", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.FormulaNumber;
			}
			else if (trimmed.Equals("68", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("chatwithoutemoji", StringComparison.OrdinalIgnoreCase))
			{
				nameValue = Windows.UI.Xaml.Input.InputScopeNameValue.ChatWithoutEmoji;
			}
			else
			{
				throw new InvalidOperationException($"Failed to create a '{outputType.FullName}' from the text '{input}'.");
			}

			output = new Windows.UI.Xaml.Input.InputScope
			{
				Names =
				{
					new Windows.UI.Xaml.Input.InputScopeName
					{
						NameValue = nameValue
					}
				}
			};

			return true;
		}

		private static bool FastStringToToolTip(Type outputType, string input, ref object output)
		{
			if (__LinkerHints.Is_Windows_UI_Xaml_Controls_ToolTip_Available
				&& outputType == typeof(Windows.UI.Xaml.Controls.ToolTip))
			{
				output = new Windows.UI.Xaml.Controls.ToolTip { Content = input };
				return true;
			}

			return false;
		}

		private static bool FastStringToKeyTime(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(KeyTime))
			{
				output = KeyTime.FromTimeSpan(TimeSpan.Parse(input, CultureInfo.InvariantCulture));
				return true;
			}

			return false;
		}

		private static bool FastStringToKeySpline(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(KeySpline))
			{
				output = KeySpline.FromString(input);
				return true;
			}

			return false;
		}

		private static bool FastStringToDuration(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(Duration))
			{
				if (input == nameof(Duration.Forever))
				{
					output = Duration.Forever;
				}
				else if (input == nameof(Duration.Automatic))
				{
					output = Duration.Automatic;
				}
				else
				{
					output = new Duration(TimeSpan.Parse(input, CultureInfo.InvariantCulture));
				}

				return true;
			}

			return false;
		}

		private static bool FastStringToRepeatBehavior(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(RepeatBehavior)
				&& input == nameof(RepeatBehavior.Forever))
			{
				output = RepeatBehavior.Forever;

				return true;
			}

			return false;
		}

		private static bool FastStringToPointCollection(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(PointCollection))
			{
				output = (PointCollection)input;

				return true;
			}

			return false;
		}

		private static bool FastStringToPath(Type outputType, string input, ref object output)
		{
#if __WASM__
			if (__LinkerHints.Is_Windows_UI_Xaml_Media_Geometry_Available
				&& outputType == typeof(Geometry))
			{
				output = (Geometry)input;

				return true;
			}
#endif

			return false;
		}

		private static bool FastStringToFontFamilyConvert(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(FontFamily))
			{
				output = new FontFamily(input);

				return true;
			}

			return false;
		}

		private static bool FastStringToMatrix(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(Windows.UI.Xaml.Media.Matrix))
			{
				var fields = GetDoubleValues(input);

				output = new Windows.UI.Xaml.Media.Matrix(fields[0], fields[1], fields[2], fields[3], fields[4], fields[5]);
				return true;
			}

			return false;
		}

		private static bool FastStringToPointF(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(System.Drawing.PointF))
			{
				var fields = GetFloatValues(input);

				if (fields.Count == 2)
				{
					output = new System.Drawing.PointF(fields[0], fields[1]);
					return true;
				}

				if (fields.Count == 1)
				{
					output = new System.Drawing.PointF(fields[0], fields[0]);
					return true;
				}
			}

			return false;
		}

		private static bool FastStringToPoint(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(Windows.Foundation.Point))
			{
				var fields = GetDoubleValues(input);

				if (fields.Count == 2)
				{
					output = new Windows.Foundation.Point(fields[0], fields[1]);
					return true;
				}

				if (fields.Count == 1)
				{
					output = new Windows.Foundation.Point(fields[0], fields[0]);
					return true;
				}
			}

			return false;
		}

		private static bool FastStringToBrushConvert(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(Brush))
			{
				output = SolidColorBrushHelper.Parse(input);
				return true;
			}

			return false;
		}

		private static bool FastStringToColorConvert(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(Windows.UI.Color))
			{
				output = Windows.UI.Colors.Parse(input);
				return true;
			}

			return false;
		}

		private static bool FastStringToImageSource(Type outputType, string input, ref object output)
		{
			if (__LinkerHints.Is_Windows_UI_Xaml_Media_ImageSource_Available
				&& outputType == typeof(Windows.UI.Xaml.Media.ImageSource))
			{
				output = (Windows.UI.Xaml.Media.ImageSource)input;
				return true;
			}

			return false;
		}

		private static bool FastStringToTextAlignmentConvert(Type outputType, string input, ref object output)
		{
			if (outputType != typeof(TextAlignment)) return false;

			var trimmed = input.AsSpan().Trim();
			if (trimmed.Equals("0", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("center", StringComparison.OrdinalIgnoreCase))
			{
				output = TextAlignment.Center;
			}
			else if (trimmed.Equals("1", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("left", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("start", StringComparison.OrdinalIgnoreCase))
			{
				output = TextAlignment.Left;
			}
			else if (trimmed.Equals("2", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("right", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("end", StringComparison.OrdinalIgnoreCase))
			{
				output = TextAlignment.Right;
			}
			else if (trimmed.Equals("3", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("justify", StringComparison.OrdinalIgnoreCase))
			{
				output = TextAlignment.Justify;
			}
			else if (trimmed.Equals("4", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("detectfromcontent", StringComparison.OrdinalIgnoreCase))
			{
				output = TextAlignment.DetectFromContent;
			}
			else
			{
				throw new InvalidOperationException($"Failed to create a '{outputType.FullName}' from the text '{input}'.");
			}

			return true;
		}

		private static bool FastStringToGridLengthConvert(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(GridLength))
			{
				var gridLengths = GridLength.ParseGridLength(input);

				if (gridLengths.Length > 0)
				{
					output = gridLengths[0];
					return true;
				}
			}

			return false;
		}

		private static bool FastStringToDoubleConvert(Type outputType, string input, ref object output)
		{
			const NumberStyles numberStyles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;

			if (outputType == typeof(double))
			{
				if (input == "") // empty string is NaN when bound in XAML
				{
					output = double.NaN;
					return true;
				}

				if (input.Length == 1)
				{
					// Fast path for one digit string-to-double.
					// Often use in VisualStateManager to set double values (like opacity) to zero or one...
					var c = input[0];
					if (c >= '0' && c <= '9')
					{
						output = (double)(c - '0');
						return true;
					}
				}

				var trimmed = IgnoreStartingFromFirstSpaceIgnoreLeading(input);

				if (trimmed.Equals("0", StringComparison.OrdinalIgnoreCase) || trimmed.Length == 0) // Fast path for zero / empty values (means zero in XAML)
				{
					output = 0d;
					return true;
				}

				if (trimmed.Equals("nan", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
				{
					output = double.NaN;
					return true;
				}

				if (trimmed.Equals("-infinity", StringComparison.OrdinalIgnoreCase))
				{
					output = double.NegativeInfinity;
					return true;
				}

				if (trimmed.Equals("infinity", StringComparison.OrdinalIgnoreCase))
				{
					output = double.PositiveInfinity;
					return true;
				}

#if NET6_0_OR_GREATER
				if (double.TryParse(trimmed, numberStyles, NumberFormatInfo.InvariantInfo, out var d))
#else
				if (double.TryParse(trimmed.ToString(), numberStyles, NumberFormatInfo.InvariantInfo, out var d))
#endif
				{
					output = d;
					return true;
				}
			}

			return false;
		}

		private static bool FastStringToSingleConvert(Type outputType, string input, ref object output)
		{
			const NumberStyles numberStyles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;

			if (outputType == typeof(float))
			{
				if (input == "") // empty string is NaN when bound in XAML
				{
					output = float.NaN;
					return true;
				}

				if (input.Length == 1)
				{
					// Fast path for one digit string-to-float
					var c = input[0];
					if (c >= '0' && c <= '9')
					{
						output = (float)(c - '0');
						return true;
					}
				}

				var trimmed = input.AsSpan().Trim();

				if (trimmed.Equals("0", StringComparison.OrdinalIgnoreCase) || trimmed.Length == 0) // Fast path for zero / empty values (means zero in XAML)
				{
					output = 0f;
					return true;
				}

				if (trimmed.Equals("nan", StringComparison.OrdinalIgnoreCase)) // "Auto" is for sizes, which are only of type double
				{
					output = float.NaN;
					return true;
				}

				if (trimmed.Equals("-infinity", StringComparison.OrdinalIgnoreCase))
				{
					output = float.NegativeInfinity;
					return true;
				}

				if (trimmed.Equals("infinity", StringComparison.OrdinalIgnoreCase))
				{
					output = float.PositiveInfinity;
					return true;
				}
#if NET6_0_OR_GREATER
				if (float.TryParse(trimmed, numberStyles, NumberFormatInfo.InvariantInfo, out var f))
#else
				if (float.TryParse(trimmed.ToString(), numberStyles, NumberFormatInfo.InvariantInfo, out var f))
#endif
				{
					output = f;
					return true;
				}
			}

			return false;
		}

		private static bool FastStringToIntegerConvert(Type outputType, string input, ref object output)
		{
			const NumberStyles numberStyles = NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite;

			if (outputType == typeof(int))
			{
				if (input.Length == 1)
				{
					// Fast path for one digit string-to-float
					var c = input[0];
					if (c >= '0' && c <= '9')
					{
						output = (int)(c - '0');
						return true;
					}
				}

				var trimmed = IgnoreStartingFromFirstSpaceIgnoreLeading(input);

				if (trimmed.Equals("0", StringComparison.OrdinalIgnoreCase) || trimmed.Length == 0) // Fast path for zero / empty values (means zero in XAML)
				{
					output = 0;
					return true;
				}

#if NET6_0_OR_GREATER
				if (int.TryParse(trimmed, numberStyles, NumberFormatInfo.InvariantInfo, out var i))
#else
				if (int.TryParse(trimmed.ToString(), numberStyles, NumberFormatInfo.InvariantInfo, out var i))
#endif
				{
					output = i;
					return true;
				}
			}

			return false;
		}

		private static bool FastStringToOrientationConvert(Type outputType, string input, ref object output)
		{
			if (outputType != typeof(Windows.UI.Xaml.Controls.Orientation)) return false;

			var trimmed = input.AsSpan().Trim();
			if (trimmed.Equals("0", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("vertical", StringComparison.OrdinalIgnoreCase))
			{
				output = Windows.UI.Xaml.Controls.Orientation.Vertical;
			}
			else if (trimmed.Equals("1", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("horizontal", StringComparison.OrdinalIgnoreCase))
			{
				output = Windows.UI.Xaml.Controls.Orientation.Horizontal;
			}
			else
			{
				throw new InvalidOperationException($"Failed to create a '{outputType.FullName}' from the text '{input}'.");
			}

			return true;
		}

		private static bool FastStringToThicknessConvert(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(Thickness))
			{
				output = new ThicknessConverter().ConvertFrom(input);
				return true;
			}

			return false;
		}

		private static bool FastStringToVerticalAlignmentConvert(Type outputType, string input, ref object output)
		{
			if (outputType != typeof(VerticalAlignment)) return false;
			var trimmed = input.AsSpan().Trim();
			if (trimmed.Equals("0", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("top", StringComparison.OrdinalIgnoreCase))
			{
				output = VerticalAlignment.Top;
			}
			else if (trimmed.Equals("1", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("center", StringComparison.OrdinalIgnoreCase))
			{
				output = VerticalAlignment.Center;
			}
			else if (trimmed.Equals("2", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("bottom", StringComparison.OrdinalIgnoreCase))
			{
				output = VerticalAlignment.Bottom;
			}
			else if (trimmed.Equals("3", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("stretch", StringComparison.OrdinalIgnoreCase))
			{
				output = VerticalAlignment.Stretch;
			}
			else
			{
				throw new InvalidOperationException($"Failed to create a '{outputType.FullName}' from the text '{input}'.");
			}

			return true;
		}

		private static bool FastStringToHorizontalAlignmentConvert(Type outputType, string input, ref object output)
		{
			if (outputType != typeof(HorizontalAlignment)) return false;

			var trimmed = input.AsSpan().Trim();
			if (trimmed.Equals("0", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("left", StringComparison.OrdinalIgnoreCase))
			{
				output = HorizontalAlignment.Left;
			}
			else if (trimmed.Equals("1", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("center", StringComparison.OrdinalIgnoreCase))
			{
				output = HorizontalAlignment.Center;
			}
			else if (trimmed.Equals("2", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("right", StringComparison.OrdinalIgnoreCase))
			{
				output = HorizontalAlignment.Right;
			}
			else if (trimmed.Equals("3", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("stretch", StringComparison.OrdinalIgnoreCase))
			{
				output = HorizontalAlignment.Stretch;
			}
			else
			{
				throw new InvalidOperationException($"Failed to create a '{outputType.FullName}' from the text '{input}'.");
			}

			return true;
		}

		private static bool FastStringToVisibilityConvert(Type outputType, string input, ref object output)
		{
			if (outputType != typeof(Visibility)) return false;

			var trimmed = input.AsSpan().Trim();
			if (trimmed.Equals("0", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("visible", StringComparison.OrdinalIgnoreCase))
			{
				output = Visibility.Visible;
			}
			else if (trimmed.Equals("1", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("collapsed", StringComparison.OrdinalIgnoreCase))
			{
				output = Visibility.Collapsed;
			}
			else
			{
				throw new InvalidOperationException($"Failed to create a '{outputType.FullName}' from the text '{input}'.");
			}

			return true;
		}

		private static bool FastStringToFontWeightConvert(Type outputType, string input, ref object output)
		{
			if (outputType != typeof(FontWeight)) return false;

			// Note that list is hard coded to avoid the cold path cost of reflection.
			var trimmed = input.AsSpan().Trim();
			if (trimmed.Equals("100", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("thin", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.Thin;
			}
			else if (trimmed.Equals("200", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("extralight", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.ExtraLight;
			}
			else if (trimmed.Equals("250", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("semilight", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.SemiLight;
			}
			else if (trimmed.Equals("300", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("light", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.Light;
			}
			else if (trimmed.Equals("400", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("normal", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.Normal;
			}
			else if (trimmed.Equals("500", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("medium", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.Medium;
			}
			else if (trimmed.Equals("600", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("semibold", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.SemiBold;
			}
			else if (trimmed.Equals("700", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("bold", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.Bold;
			}
			else if (trimmed.Equals("800", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("extrabold", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.ExtraBold;
			}
			else if (trimmed.Equals("900", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("black", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.Black;
			}
			else if (trimmed.Equals("950", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("extrablack", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.ExtraBlack;
			}
			// legacy wpf aliases
			else if (trimmed.Equals("ultralight", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.UltraLight; // 200 ExtraLight
			}
			else if (trimmed.Equals("regular", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.Regular; // 400 Normal
			}
			else if (trimmed.Equals("demibold", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.DemiBold; // 600 SemiBold
			}
			else if (trimmed.Equals("ultrabold", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.UltraBold; // 800 ExtraBold
			}
			else if (trimmed.Equals("heavy", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.Heavy; // 900 Black
			}
			else if (trimmed.Equals("ultrablack", StringComparison.OrdinalIgnoreCase))
			{
				output = FontWeights.UltraBlack; // 950 ExtraBlack
			}
			else
			{
				throw new InvalidOperationException($"Failed to create a '{outputType.FullName}' from the text '{input}'.");
			}

			return true;
		}

		private static bool FastStringToUriConvert(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(Uri))
			{
				output = new Uri(input);
				return true;
			}
			else
			{
				return false;
			}
		}

		private static bool FastStringToTypeConvert(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(Type))
			{
				output = Type.GetType(input, throwOnError: true);
				return true;
			}
			else
			{
				return false;
			}
		}

		private static List<double> GetDoubleValues(string input)
		{
			var list = new List<double>();
			var s = input.AsSpan().Trim();
			while (!s.IsEmpty)
			{
				var length = NextDoubleLength(s);
				list.Add(double.Parse(s.Slice(0, length).ToString(), NumberStyles.Float, CultureInfo.InvariantCulture));
				s = s.Slice(length);
				s = EatSeparator(s);
			}

			return list;
		}

		private static List<float> GetFloatValues(string input)
		{
			var list = new List<float>();
			var s = input.AsSpan().Trim();
			while (!s.IsEmpty)
			{
				var length = NextDoubleLength(s);
				list.Add(float.Parse(s.Slice(0, length).ToString(), NumberStyles.Float, CultureInfo.InvariantCulture));
				s = s.Slice(length);
				s = EatSeparator(s);
			}

			return list;
		}

		private static ReadOnlySpan<char> EatSeparator(ReadOnlySpan<char> s)
		{
			if (s.IsEmpty)
			{
				return s;
			}

			var seenWhitespace = false;
			var seenComma = false;
			int i = 0;
			for (; i < s.Length; i++)
			{
				if (char.IsWhiteSpace(s[i]))
				{
					seenWhitespace = true;
				}
				else if (s[i] == ',')
				{
					if (seenComma)
					{
						throw new ArgumentException("Comma shouldn't appear twice between two double values.");
					}

					seenComma = true;
				}
				else
				{
					break;
				}
			}

			Debug.Assert(seenWhitespace || seenComma);

			return s.Slice(i);
		}

		/// <summary>
		/// Returns the number of characters containing the next double.
		/// </summary>
		private static int NextDoubleLength(ReadOnlySpan<char> s)
		{
			int i = 0;
			for (; i < s.Length; i++)
			{
				if (char.IsWhiteSpace(s[i]) || s[i] == ',')
				{
					break;
				}
			}

			return i;
		}

		private static ReadOnlySpan<char> IgnoreStartingFromFirstSpaceIgnoreLeading(string value)
		{
			var span = value.AsSpan().TrimStart();

			var firstWhitespace = -1;
			for (int i = 0; i < span.Length; i++)
			{
				if (char.IsWhiteSpace(span[i]))
				{
					firstWhitespace = i;
					break;
				}
			}

			return firstWhitespace == -1
				? value.AsSpan()
				: span.Slice(0, firstWhitespace);
		}
	}
}
#endif

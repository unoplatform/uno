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
using Windows.UI.Text;
using FontWeight = Windows.UI.Text.FontWeight;

#if __ANDROID__
using View = Android.Views.View;
using Font = Android.Graphics.Typeface;
using Android.Graphics;
#elif __IOS__
using View = UIKit.UIView;
using Color = UIKit.UIColor;
using Font = UIKit.UIFont;
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

			if (FastStringToRect(outputType, input, ref output))
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

			if (FastStringToBoolean(outputType, input, ref output))
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

			var nameValue = input.ToLowerInvariant().Trim() switch
			{
				"0" or "default" => Windows.UI.Xaml.Input.InputScopeNameValue.Default,
				"1" or "url" => Windows.UI.Xaml.Input.InputScopeNameValue.Url,
				"5" or "emailsmtpaddress" => Windows.UI.Xaml.Input.InputScopeNameValue.EmailSmtpAddress,
				"7" or "personalfullname" => Windows.UI.Xaml.Input.InputScopeNameValue.PersonalFullName,
				"20" or "currencyamountandsymbol" => Windows.UI.Xaml.Input.InputScopeNameValue.CurrencyAmountAndSymbol,
				"21" or "currencyamount" => Windows.UI.Xaml.Input.InputScopeNameValue.CurrencyAmount,
				"23" or "datemonthnumber" => Windows.UI.Xaml.Input.InputScopeNameValue.DateMonthNumber,
				"24" or "datedaynumber" => Windows.UI.Xaml.Input.InputScopeNameValue.DateDayNumber,
				"25" or "dateyear" => Windows.UI.Xaml.Input.InputScopeNameValue.DateYear,
				"28" or "digits" => Windows.UI.Xaml.Input.InputScopeNameValue.Digits,
				"29" or "number" => Windows.UI.Xaml.Input.InputScopeNameValue.Number,
				"31" or "password" => Windows.UI.Xaml.Input.InputScopeNameValue.Password,
				"32" or "telephonenumber" => Windows.UI.Xaml.Input.InputScopeNameValue.TelephoneNumber,
				"33" or "telephonecountrycode" => Windows.UI.Xaml.Input.InputScopeNameValue.TelephoneCountryCode,
				"34" or "telephoneareacode" => Windows.UI.Xaml.Input.InputScopeNameValue.TelephoneAreaCode,
				"35" or "telephonelocalnumber" => Windows.UI.Xaml.Input.InputScopeNameValue.TelephoneLocalNumber,
				"37" or "timehour" => Windows.UI.Xaml.Input.InputScopeNameValue.TimeHour,
				"38" or "timeminutesorseconds" => Windows.UI.Xaml.Input.InputScopeNameValue.TimeMinutesOrSeconds,
				"39" or "numberfullwidth" => Windows.UI.Xaml.Input.InputScopeNameValue.NumberFullWidth,
				"40" or "alphanumerichalfwidth" => Windows.UI.Xaml.Input.InputScopeNameValue.AlphanumericHalfWidth,
				"41" or "alphanumericfullwidth" => Windows.UI.Xaml.Input.InputScopeNameValue.AlphanumericFullWidth,
				"44" or "hiragana" => Windows.UI.Xaml.Input.InputScopeNameValue.Hiragana,
				"45" or "katakanahalfwidth" => Windows.UI.Xaml.Input.InputScopeNameValue.KatakanaHalfWidth,
				"46" or "katakanafullwidth" => Windows.UI.Xaml.Input.InputScopeNameValue.KatakanaFullWidth,
				"47" or "hanja" => Windows.UI.Xaml.Input.InputScopeNameValue.Hanja,
				"48" or "hangulhalfwidth" => Windows.UI.Xaml.Input.InputScopeNameValue.HangulHalfWidth,
				"49" or "hangulfullwidth" => Windows.UI.Xaml.Input.InputScopeNameValue.HangulFullWidth,
				"50" or "search" => Windows.UI.Xaml.Input.InputScopeNameValue.Search,
				"51" or "formula" => Windows.UI.Xaml.Input.InputScopeNameValue.Formula,
				"52" or "searchincremental" => Windows.UI.Xaml.Input.InputScopeNameValue.SearchIncremental,
				"53" or "chinesehalfwidth" => Windows.UI.Xaml.Input.InputScopeNameValue.ChineseHalfWidth,
				"54" or "chinesefullwidth" => Windows.UI.Xaml.Input.InputScopeNameValue.ChineseFullWidth,
				"55" or "nativescript" => Windows.UI.Xaml.Input.InputScopeNameValue.NativeScript,
				"57" or "text" => Windows.UI.Xaml.Input.InputScopeNameValue.Text,
				"58" or "chat" => Windows.UI.Xaml.Input.InputScopeNameValue.Chat,
				"59" or "nameorphonenumber" => Windows.UI.Xaml.Input.InputScopeNameValue.NameOrPhoneNumber,
				"60" or "emailnameoraddress" => Windows.UI.Xaml.Input.InputScopeNameValue.EmailNameOrAddress,
				"62" or "maps" => Windows.UI.Xaml.Input.InputScopeNameValue.Maps,
				"63" or "numericpassword" => Windows.UI.Xaml.Input.InputScopeNameValue.NumericPassword,
				"64" or "numericpin" => Windows.UI.Xaml.Input.InputScopeNameValue.NumericPin,
				"65" or "alphanumericpin" => Windows.UI.Xaml.Input.InputScopeNameValue.AlphanumericPin,
				"67" or "formulanumber" => Windows.UI.Xaml.Input.InputScopeNameValue.FormulaNumber,
				"68" or "chatwithoutemoji" => Windows.UI.Xaml.Input.InputScopeNameValue.ChatWithoutEmoji,

				_ => throw new InvalidOperationException($"Failed to create a '{outputType.FullName}' from the text '{input}'."),
			};
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

		private static bool FastStringToRect(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(Windows.Foundation.Rect))
			{
				var fields = GetDoubleValues(input);

				if (fields.Count == 4)
				{
					output = new Windows.Foundation.Rect(fields[0], fields[1], fields[2], fields[3]);
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

			output = input.ToLowerInvariant().Trim() switch
			{
				"0" or "center" => TextAlignment.Center,
				"1" or "left" or "start" => TextAlignment.Left,
				"2" or "right" or "end" => TextAlignment.Right,
				"3" or "justify" => TextAlignment.Justify,
				"4" or "detectfromcontent" => TextAlignment.DetectFromContent,

				_ => throw new InvalidOperationException($"Failed to create a '{outputType.FullName}' from the text '{input}'."),
			};

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

				if (trimmed == "0" || trimmed.Length == 0) // Fast path for zero / empty values (means zero in XAML)
				{
					output = 0d;
					return true;
				}

				trimmed = trimmed.ToLowerInvariant();

				if (trimmed == "nan" || trimmed == "auto")
				{
					output = double.NaN;
					return true;
				}

				if (trimmed == "-infinity")
				{
					output = double.NegativeInfinity;
					return true;
				}

				if (trimmed == "infinity")
				{
					output = double.PositiveInfinity;
					return true;
				}

				if (double.TryParse(trimmed, numberStyles, NumberFormatInfo.InvariantInfo, out var d))
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

				var trimmed = input.Trim();

				if (trimmed == "0" || trimmed == "") // Fast path for zero / empty values (means zero in XAML)
				{
					output = 0f;
					return true;
				}

				trimmed = trimmed.ToLowerInvariant();

				if (trimmed == "nan") // "Auto" is for sizes, which are only of type double
				{
					output = float.NaN;
					return true;
				}

				if (trimmed == "-infinity")
				{
					output = float.NegativeInfinity;
					return true;
				}

				if (trimmed == "infinity")
				{
					output = float.PositiveInfinity;
					return true;
				}

				if (float.TryParse(trimmed, numberStyles, NumberFormatInfo.InvariantInfo, out var f))
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

				if (trimmed == "0" || trimmed.Length == 0) // Fast path for zero / empty values (means zero in XAML)
				{
					output = 0;
					return true;
				}

				if (int.TryParse(trimmed, numberStyles, NumberFormatInfo.InvariantInfo, out var i))
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

			output = input.ToLowerInvariant().Trim() switch
			{
				"0" or "vertical" => Windows.UI.Xaml.Controls.Orientation.Vertical,
				"1" or "horizontal" => Windows.UI.Xaml.Controls.Orientation.Horizontal,

				_ => throw new InvalidOperationException($"Failed to create a '{outputType.FullName}' from the text '{input}'."),
			};

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

			output = input.ToLowerInvariant().Trim() switch
			{
				"0" or "top" => VerticalAlignment.Top,
				"1" or "center" => VerticalAlignment.Center,
				"2" or "bottom" => VerticalAlignment.Bottom,
				"3" or "stretch" => VerticalAlignment.Stretch,

				_ => throw new InvalidOperationException($"Failed to create a '{outputType.FullName}' from the text '{input}'."),
			};

			return true;
		}

		private static bool FastStringToHorizontalAlignmentConvert(Type outputType, string input, ref object output)
		{
			if (outputType != typeof(HorizontalAlignment)) return false;

			output = input.ToLowerInvariant().Trim() switch
			{
				"0" or "left" => HorizontalAlignment.Left,
				"1" or "center" => HorizontalAlignment.Center,
				"2" or "right" => HorizontalAlignment.Right,
				"3" or "stretch" => HorizontalAlignment.Stretch,

				_ => throw new InvalidOperationException($"Failed to create a '{outputType.FullName}' from the text '{input}'."),
			};

			return true;
		}

		private static bool FastStringToVisibilityConvert(Type outputType, string input, ref object output)
		{
			if (outputType != typeof(Visibility)) return false;

			output = input.ToLowerInvariant().Trim() switch
			{
				"0" or "visible" => Visibility.Visible,
				"1" or "collapsed" => Visibility.Collapsed,

				_ => throw new InvalidOperationException($"Failed to create a '{outputType.FullName}' from the text '{input}'."),
			};

			return true;
		}

		private static bool FastStringToFontWeightConvert(Type outputType, string input, ref object output)
		{
			if (outputType != typeof(FontWeight)) return false;

			// Note that list is hard coded to avoid the cold path cost of reflection.
			output = input.ToLowerInvariant().Trim() switch
			{
				"100" or "thin" => FontWeights.Thin,
				"200" or "extralight" => FontWeights.ExtraLight,
				"250" or "semilight" => FontWeights.SemiLight,
				"300" or "light" => FontWeights.Light,
				"400" or "normal" => FontWeights.Normal,
				"500" or "medium" => FontWeights.Medium,
				"600" or "semibold" => FontWeights.SemiBold,
				"700" or "bold" => FontWeights.Bold,
				"800" or "extrabold" => FontWeights.ExtraBold,
				"900" or "black" => FontWeights.Black,
				"950" or "extrablack" => FontWeights.ExtraBlack,

				// legacy wpf aliases
				"ultralight" => FontWeights.UltraLight, // 200 ExtraLight
				"regular" => FontWeights.Regular, // 400 Normal
				"demibold" => FontWeights.DemiBold, // 600 SemiBold
				"ultrabold" => FontWeights.UltraBold, // 800 ExtraBold
				"heavy" => FontWeights.Heavy, // 900 Black
				"ultrablack" => FontWeights.UltraBlack, // 950 ExtraBlack

				_ => throw new InvalidOperationException($"Failed to create a '{outputType.FullName}' from the text '{input}'."),
			};

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

		private static bool FastStringToBoolean(Type outputType, string input, ref object output)
		{
			if (outputType == typeof(bool) && bool.TryParse(input, out var result))
			{
				output = result;
				return true;
			}

			return false;
		}

		private static List<double> GetDoubleValues(string input)
		{
			var list = new List<double>();
			var s = input.AsSpan().Trim();
			while (!s.IsEmpty)
			{
				var length = NextDoubleLength(s);
				list.Add(double.Parse(s.Slice(0, length), NumberStyles.Float, CultureInfo.InvariantCulture));
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
				list.Add(float.Parse(s.Slice(0, length), NumberStyles.Float, CultureInfo.InvariantCulture));
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

		private static string IgnoreStartingFromFirstSpaceIgnoreLeading(string value)
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
				? value
				: span.Slice(0, firstWhitespace).ToString();
		}
	}
}
#endif

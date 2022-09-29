#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

namespace Uno.UI.Converters
{
	/// <summary>
	/// Do not use outside of Uno
	/// </summary>
	public class UnoNativeDefaultProgressBarReverseBoolConverter : ConverterBase
	{
		[SuppressMessage("Microsoft.Globalization", "CA1305:SpecifyIFormatProvider", Justification = "Not for end user")]
		protected override object Convert(object value, Type targetType, object parameter)
		{
			if (parameter != null)
			{
				throw new ArgumentException($"This converter does not use any parameters. You should remove \"{parameter}\" passed as parameter.");
			}

			if (value != null && !(value is bool))
			{
				throw new ArgumentException($"Value must either be null or of type bool. Got {value} ({value.GetType().FullName})");
			}

			var valueToConvert = value != null && System.Convert.ToBoolean(value, CultureInfo.InvariantCulture);

			return !valueToConvert;
		}

		protected override object ConvertBack(object value, Type targetType, object parameter)
		{
			// Same as Convert, except it should never be null
			if (value == null)
			{
				throw new InvalidOperationException("Since results should never be null, reverse conversion does not support null values");
			}

			return Convert(value, targetType, parameter);
		}
	}
}

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Windows.UI.Xaml
{
	public class ThicknessConverter : TypeConverter
	{
		private static readonly char[] _valueSeparator = new char[] { ',', ' ', '\t', '\r', '\n' };

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			var canConvert = sourceType == typeof(string)
				|| sourceType == typeof(double)
				|| sourceType == typeof(float)
				|| sourceType == typeof(int);

			if (canConvert)
			{
				return true;
			}

			return base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value is string stringValue)
			{
				var values = stringValue
					.Split(_valueSeparator, StringSplitOptions.RemoveEmptyEntries)
					.Select(s => double.Parse(s, CultureInfo.InvariantCulture))
					.ToArray();

				if (values.Length == 4)
				{
					return new Thickness(values[0], values[1], values[2], values[3]);
				}
				else if (values.Length == 2)
				{
					return new Thickness(values[0], values[1], values[0], values[1]);
				}
				else
				{
					return new Thickness(values[0], values[0], values[0], values[0]);
				}
			}
			else if (value is double doubleValue)
			{
				return new Thickness(doubleValue);
			}
			else if (value is float floatValue)
			{
				return new Thickness(floatValue);
			}
			else if (value is int intValue)
			{
				return new Thickness(intValue);
			}

			return base.ConvertFrom(context, culture, value);
		}
	}
}


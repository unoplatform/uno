using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Windows.UI.Xaml
{
	public class ThicknessConverter : TypeConverter
	{
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			var canConvert = sourceType == typeof(string);

			if (canConvert)
			{
				return true;
			}

			return base.CanConvertFrom(context, sourceType);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			var stringValue = value as string;

			if (stringValue != null)
			{
				var values = stringValue
					.Split(',')
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

			return base.ConvertFrom(context, culture, value);
		}
	}
}


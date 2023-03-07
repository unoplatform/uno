using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace Windows.Foundation;

[EditorBrowsable(EditorBrowsableState.Never)]
public class SizeConverter : TypeConverter
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
				.Split(new[] { ',' })
				.Select(s => double.Parse(s, CultureInfo.InvariantCulture))
				.ToArray();

			if (values.Length == 2)
			{
				return new Size(values[0], values[1]);
			}
		}

		return base.ConvertFrom(context, culture, value);
	}
}

#if IS_UNO
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Uno.Extensions;

namespace Windows.UI.Xaml
{
	[TypeConverter(typeof(GridLength.Converter))]
	public partial struct GridLength
	{
		public class Converter : TypeConverter
		{
			// Overrides the CanConvertFrom method of TypeConverter.
			// The ITypeDescriptorContext interface provides the context for the
			// conversion. Typically, this interface is used at design time to 
			// provide information about the design-time container.
			public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
			{
				if (
					sourceType == typeof(string)
					|| sourceType.IsPrimitive
				)
				{
					return true;
				}

				return base.CanConvertFrom(context, sourceType);
			}

			// Overrides the ConvertFrom method of TypeConverter.
			public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
			{
				if (value is string)
				{
					return GridLength.ParseGridLength(value as string).FirstOrDefault();
				}

				if (value is ValueType)
				{
					return GridLengthHelper.FromPixels(Convert.ToDouble(value, CultureInfo.InvariantCulture));
				}

				return base.ConvertFrom(context, culture, value);
			}

			// Overrides the ConvertTo method of TypeConverter.
			public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
			{
				return base.ConvertTo(context, culture, value, destinationType);
			}
		}

	}
}
#endif

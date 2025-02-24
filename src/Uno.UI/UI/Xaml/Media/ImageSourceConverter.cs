using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using Uno.Extensions;

namespace Windows.UI.Xaml.Media
{
	public partial class ImageSourceConverter : TypeConverter
	{
		partial void CanConvertFromPartial(Type sourceType, ref bool canConvert);
		partial void ConvertFromPartial(object value, ref ImageSource result);
		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			var canConvert = sourceType == typeof(string)
				|| sourceType == typeof(Uri)
				|| sourceType.Is(typeof(Stream));

			if (canConvert)
			{
				return true;
			}

			bool canConvertPartial = false;
			CanConvertFromPartial(sourceType, ref canConvertPartial);

			if (canConvertPartial)
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
				return (ImageSource)(string)value;
			}

			if (value is Uri)
			{
				return (ImageSource)(Uri)value;
			}

			if (value is Stream)
			{
				return (ImageSource)(Stream)value;
			}

			ImageSource source = null;
			ConvertFromPartial(value, ref source);
			if (source != null)
			{
				return source;
			}

			return base.ConvertFrom(context, culture, value);
		}
	}


}

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Media
{
	public partial class ImageSourceConverter
	{
		partial void CanConvertFromPartial(Type sourceType, ref bool canConvert)
		{
			canConvert = sourceType == typeof(ABitmap)
				|| sourceType == typeof(ABitmapDrawable);
		}
		partial void ConvertFromPartial(object value, ref ImageSource result)
		{
			if (value is ABitmap)
			{
				result = (ABitmap)value;
				return;
			}
			if (value is ABitmapDrawable)
			{
				result = (ABitmapDrawable)value;
				return;
			}
		}
	}
}

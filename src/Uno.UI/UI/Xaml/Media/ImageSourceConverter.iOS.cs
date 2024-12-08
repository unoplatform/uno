using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media
{
	public partial class ImageSourceConverter
	{
		partial void CanConvertFromPartial(Type sourceType, ref bool canConvert)
		{
			canConvert = sourceType == typeof(UIKit.UIImage);
		}

		partial void ConvertFromPartial(object value, ref ImageSource result)
		{
			if (value is UIKit.UIImage)
			{
				result = (UIKit.UIImage)value;
				return;
			}
		}
	}
}

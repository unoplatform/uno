using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Media
{
    public partial class ImageSourceConverter
    {
		static partial void CanConvertFromPartial(Type sourceType, ref bool canConvert)
		{
			canConvert = sourceType == typeof(Android.Graphics.Bitmap)
				|| sourceType == typeof(Android.Graphics.Drawables.BitmapDrawable);
		}
		static partial void ConvertFromPartial(object value, ref ImageSource result)
		{
			if (value is Android.Graphics.Bitmap)
			{
				result = (Android.Graphics.Bitmap)value;
				return;
			}
			if (value is Android.Graphics.Drawables.BitmapDrawable)
			{
				result = (Android.Graphics.Drawables.BitmapDrawable)value;
				return;
			}
		}
	}
}

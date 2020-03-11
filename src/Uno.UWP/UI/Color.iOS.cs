#if __IOS__
using System;

namespace Windows.UI
{
	public partial struct Color : IFormattable
	{
		public static implicit operator UIKit.UIColor(Color color) => UIKit.UIColor.FromRGBA(color.R, color.G, color.B, color.A);
		public static implicit operator CoreGraphics.CGColor(Color color) => UIKit.UIColor.FromRGBA(color.R, color.G, color.B, color.A).CGColor;

		public static implicit operator Color(UIKit.UIColor color) => color.CGColor;

		public static implicit operator Color(CoreGraphics.CGColor color)
		{
			nfloat red, green, blue, alpha;

			nfloat[] components = color.Components;
			switch (components.Length)
			{
				case 2:
					red = components[0];
					green = components[0];
					blue = components[0];
					alpha = components[1];
					break;
				case 3:
					red = components[0];
					green = components[1];
					blue = components[2];
					alpha = 1f;
					break;
				case 4:
					red = components[0];
					green = components[1];
					blue = components[2];
					alpha = components[3];
					break;
				default:
					throw new InvalidOperationException("Unsupported colorspace component length: " + components.Length);
			}

			return FromArgb((byte)(alpha * 255 + .5), (byte)(red * 255 + .5), (byte)(green * 255 + .5), (byte)(blue * 255 + .5));
		}
	}
}
#endif

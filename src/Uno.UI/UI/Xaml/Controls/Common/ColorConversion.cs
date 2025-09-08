using System;
using System.Globalization;
using Windows.UI;

namespace Microsoft.UI.Xaml.Controls
{
	internal static class ColorConversion
	{
		public static UInt64? TryParseInt(string s)
		{
			return TryParseInt(s, 10 /* numBase */);
		}

		public static UInt64? TryParseInt(string str, int numBase)
		{
			// If we have a zero-length string, then we can immediately know
			// that this is not a valid integer.
			if (string.IsNullOrEmpty(str))
			{
				return null;
			}

			// Uno Doc: Using C#/.net methods instead, only works for base 10 or 16
			switch (numBase)
			{
				case 10:
					{
						if (UInt64.TryParse(str, out UInt64 result))
						{
							return result;
						}
						break;
					}

				case 16:
					{
						if (UInt64.TryParse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out UInt64 result))
						{
							return result;
						}
						break;
					}
			}

			// Uno Doc: C++ methodology does not easily convert to C#
			/*wchar_t *end;

			// wcstoll takes in a string and converts as much as it as it can to an integer value,
			// returning a pointer to the first element that it wasn't able to consider part of an integer.
			// If we got all the way to the end of the string, then the whole thing was a valid string.
			auto result = wcstoul(str.data(), &end, base);
			if (*end == '\0')
			{
				return result;
			}*/

			return null;
		}

		public static Hsv RgbToHsv(Rgb rgb)
		{
			double hue;
			double saturation;
			double value;

			double max = rgb.R >= rgb.G ? (rgb.R >= rgb.B ? rgb.R : rgb.B) : (rgb.G >= rgb.B ? rgb.G : rgb.B);
			double min = rgb.R <= rgb.G ? (rgb.R <= rgb.B ? rgb.R : rgb.B) : (rgb.G <= rgb.B ? rgb.G : rgb.B);

			// The value, a number between 0 and 1, is the largest of R, G, and B (divided by 255).
			// Conceptually speaking, it represents how much color is present.
			// If at least one of R, G, B is 255, then there exists as much color as there can be.
			// If RGB = (0, 0, 0), then there exists no color at all - a value of zero corresponds
			// to black (i.e., the absence of any color).
			value = max;

			// The "chroma" of the color is a value directly proportional to the extent to which
			// the color diverges from greyscale.  If, for example, we have RGB = (255, 255, 0),
			// then the chroma is maximized - this is a pure yellow, no grey of any kind.
			// On the other hand, if we have RGB = (128, 128, 128), then the chroma being zero
			// implies that this color is pure greyscale, with no actual hue to be found.
			double chroma = max - min;

			// If the chrome is zero, then hue is technically undefined - a greyscale color
			// has no hue.  For the sake of convenience, we'll just set hue to zero, since
			// it will be unused in this circumstance.  Since the color is purely grey,
			// saturation is also equal to zero - you can think of saturation as basically
			// a measure of hue intensity, such that no hue at all corresponds to a
			// nonexistent intensity.
			if (chroma == 0)
			{
				hue = 0.0;
				saturation = 0.0;
			}
			else
			{
				// In this block, hue is properly defined, so we'll extract both hue
				// and saturation information from the RGB color.

				// Hue can be thought of as a cyclical thing, between 0 degrees and 360 degrees.
				// A hue of 0 degrees is red; 120 degrees is green; 240 degrees is blue; and 360 is back to red.
				// Every other hue is somewhere between either red and green, green and blue, and blue and red,
				// so every other hue can be thought of as an angle on this color wheel.
				// These if/else statements determines where on this color wheel our color lies.
				if (rgb.R == max)
				{
					// If the red channel is the most pronounced channel, then we exist
					// somewhere between (-60, 60) on the color wheel - i.e., the section around 0 degrees
					// where red dominates.  We figure out where in that section we are exactly
					// by considering whether the green or the blue channel is greater - by subtracting green from blue,
					// then if green is greater, we'll nudge ourselves closer to 60, whereas if blue is greater, then
					// we'll nudge ourselves closer to -60.  We then divide by chroma (which will actually make the result larger,
					// since chroma is a value between 0 and 1) to normalize the value to ensure that we get the right hue
					// even if we're very close to greyscale.
					hue = 60 * (rgb.G - rgb.B) / chroma;
				}
				else if (rgb.G == max)
				{
					// We do the exact same for the case where the green channel is the most pronounced channel,
					// only this time we want to see if we should tilt towards the blue direction or the red direction.
					// We add 120 to center our value in the green third of the color wheel.
					hue = 120 + 60 * (rgb.B - rgb.R) / chroma;
				}
				else // rgb.B == max
				{
					// And we also do the exact same for the case where the blue channel is the most pronounced channel,
					// only this time we want to see if we should tilt towards the red direction or the green direction.
					// We add 240 to center our value in the blue third of the color wheel.
					hue = 240 + 60 * (rgb.R - rgb.G) / chroma;
				}

				// Since we want to work within the range [0, 360), we'll add 360 to any value less than zero -
				// this will bump red values from within -60 to -1 to 300 to 359.  The hue is the same at both values.
				if (hue < 0.0)
				{
					hue += 360.0;
				}

				// The saturation, our final HSV axis, can be thought of as a value between 0 and 1 indicating how intense our color is.
				// To find it, we divide the chroma - the distance between the minimum and the maximum RGB channels - by the maximum channel (i.e., the value).
				// This effectively normalizes the chroma - if the maximum is 0.5 and the minimum is 0, the saturation will be (0.5 - 0) / 0.5 = 1,
				// meaning that although this color is not as bright as it can be, the dark color is as intense as it possibly could be.
				// If, on the other hand, the maximum is 0.5 and the minimum is 0.25, then the saturation will be (0.5 - 0.25) / 0.5 = 0.5,
				// meaning that this color is partially washed out.
				// A saturation value of 0 corresponds to a greyscale color, one in which the color is *completely* washed out and there is no actual hue.
				saturation = chroma / value;
			}

			return new Hsv(hue, saturation, value);
		}

		public static Rgb HsvToRgb(Hsv hsv)
		{
			double hue = hsv.H;
			double saturation = hsv.S;
			double value = hsv.V;

			// We want the hue to be between 0 and 359,
			// so we first ensure that that's the case.
			while (hue >= 360.0)
			{
				hue -= 360.0;
			}

			while (hue < 0.0)
			{
				hue += 360.0;
			}

			// We similarly clamp saturation and value between 0 and 1.
			saturation = saturation < 0.0 ? 0.0 : saturation;
			saturation = saturation > 1.0 ? 1.0 : saturation;

			value = value < 0.0 ? 0.0 : value;
			value = value > 1.0 ? 1.0 : value;

			// The first thing that we need to do is to determine the chroma (see above for its definition).
			// Remember from above that:
			//
			// 1. The chroma is the difference between the maximum and the minimum of the RGB channels,
			// 2. The value is the maximum of the RGB channels, and
			// 3. The saturation comes from dividing the chroma by the maximum of the RGB channels (i.e., the value).
			//
			// From these facts, you can see that we can retrieve the chroma by simply multiplying the saturation and the value,
			// and we can retrieve the minimum of the RGB channels by subtracting the chroma from the value.
			double chroma = saturation * value;
			double min = value - chroma;

			// If the chroma is zero, then we have a greyscale color.  In that case, the maximum and the minimum RGB channels
			// have the same value (and, indeed, all of the RGB channels are the same), so we can just immediately return
			// the minimum value as the value of all the channels.
			if (chroma == 0)
			{
				return new Rgb(min, min, min);
			}

			// If the chroma is not zero, then we need to continue.  The first step is to figure out
			// what section of the color wheel we're located in.  In order to do that, we'll divide the hue by 60.
			// The resulting value means we're in one of the following locations:
			//
			// 0 - Between red and yellow.
			// 1 - Between yellow and green.
			// 2 - Between green and cyan.
			// 3 - Between cyan and blue.
			// 4 - Between blue and purple.
			// 5 - Between purple and red.
			//
			// In each of these sextants, one of the RGB channels is completely present, one is partially present, and one is not present.
			// For example, as we transition between red and yellow, red is completely present, green is becoming increasingly present, and blue is not present.
			// Then, as we transition from yellow and green, green is now completely present, red is becoming decreasingly present, and blue is still not present.
			// As we transition from green to cyan, green is still completely present, blue is becoming increasingly present, and red is no longer present.  And so on.
			// 
			// To convert from hue to RGB value, we first need to figure out which of the three channels is in which configuration
			// in the sextant that we're located in.  Next, we figure out what value the completely-present color should have.
			// We know that chroma = (max - min), and we know that this color is the max color, so to find its value we simply add
			// min to chroma to retrieve max.  Finally, we consider how far we've transitioned from the pure form of that color
			// to the next color (e.g., how far we are from pure red towards yellow), and give a value to the partially present channel
			// equal to the minimum plus the chroma (i.e., the max minus the min), multiplied by the percentage towards the new color.
			// This gets us a value between the maximum and the minimum representing the partially present channel.
			// Finally, the not-present color must be equal to the minimum value, since it is the one least participating in the overall color.
			int sextant = (int)(hue / 60);
			double intermediateColorPercentage = hue / 60 - sextant;
			double max = chroma + min;

			double r = 0;
			double g = 0;
			double b = 0;

			switch (sextant)
			{
				case 0:
					r = max;
					g = min + chroma * intermediateColorPercentage;
					b = min;
					break;
				case 1:
					r = min + chroma * (1 - intermediateColorPercentage);
					g = max;
					b = min;
					break;
				case 2:
					r = min;
					g = max;
					b = min + chroma * intermediateColorPercentage;
					break;
				case 3:
					r = min;
					g = min + chroma * (1 - intermediateColorPercentage);
					b = max;
					break;
				case 4:
					r = min + chroma * intermediateColorPercentage;
					g = min;
					b = max;
					break;
				case 5:
					r = max;
					g = min;
					b = min + chroma * (1 - intermediateColorPercentage);
					break;
			}

			return new Rgb(r, g, b);
		}

		public static Rgb HexToRgb(string input)
		{
			var (rgb, a) = HexToRgba(input);
			return rgb;
		}

		public static string RgbToHex(Rgb rgb)
		{
			byte rByte = (byte)Math.Round(rgb.R * 255.0);
			byte gByte = (byte)Math.Round(rgb.G * 255.0);
			byte bByte = (byte)Math.Round(rgb.B * 255.0);

			UInt64 hexValue = ((UInt64)rByte << 16) + ((UInt64)gByte << 8) + (UInt64)bByte;

			// Uno Doc: Using C#/.net string methods instead
			string hexString = string.Format(CultureInfo.InvariantCulture, "#{0:X6}", hexValue);

			// We'll size this string to accommodate "#XXXXXX" - i.e., a full RGB number with a # sign.
			//wchar_t hexString[8];
			//winrt::check_hresult(StringCchPrintfW(&hexString[0], 8, L"#%06X", hexValue));

			return hexString;
		}

		public static (Rgb, double) HexToRgba(string input)
		{
			// The input always begins with a #, so we'll move past that.
			input = input?.Length > 1 ? input?.Substring(1) : input;

			var hexValue = TryParseInt(input, 16);

			// If we failed to parse the string into an integer, then we'll return all -1's.
			// ARGB values can never be negative, so this is a convenient error state to use
			// to indicate that this value should not actually be used.
			if (!hexValue.HasValue)
			{
				return (new Rgb(-1, -1, -1), -1);
			}

			var hex = hexValue.Value;
			byte a = (byte)((hex & 0xff000000) >> 24);
			byte r = (byte)((hex & 0x00ff0000) >> 16);
			byte g = (byte)((hex & 0x0000ff00) >> 8);
			byte b = (byte)(hex & 0x000000ff);

			return (new Rgb(r / 255.0, g / 255.0, b / 255.0), a / 255.0);
		}

		public static string RgbaToHex(Rgb rgb, double alpha)
		{
			byte aByte = (byte)Math.Round(alpha * 255.0);
			byte rByte = (byte)Math.Round(rgb.R * 255.0);
			byte gByte = (byte)Math.Round(rgb.G * 255.0);
			byte bByte = (byte)Math.Round(rgb.B * 255.0);

			UInt64 hexValue = ((UInt64)aByte << 24) + ((UInt64)rByte << 16) + ((UInt64)gByte << 8) + ((UInt64)bByte & 0xff);

			// Uno Doc: Using C#/.net string methods instead
			string hexString = string.Format(CultureInfo.InvariantCulture, "#{0:X8}", hexValue);

			// We'll size this string to accommodate "#XXXXXXXX" - i.e., a full ARGB number with a # sign.
			//wchar_t hexString[10];
			//winrt::check_hresult(StringCchPrintfW(&hexString[0], 10, L"#%08X", hexValue));

			return hexString;
		}

		public static Color ColorFromRgba(Rgb rgb, double alpha = 1.0)
		{
			return Color.FromArgb(
				(byte)Math.Round(alpha * 255),
				(byte)Math.Round(rgb.R * 255),
				(byte)Math.Round(rgb.G * 255),
				(byte)Math.Round(rgb.B * 255));
		}

		public static Rgb RgbFromColor(Color color)
		{
			return new Rgb(color.R / 255.0, color.G / 255.0, color.B / 255.0);
		}
	}
}

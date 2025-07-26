using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	internal static class ColorHelpers
	{
		public const int CheckerSize = 4;

		// Uno Doc: These enums were originally in ColorHelpers.h in WinUI.
		// They could be moved to the namespace instead of the class here
		public enum IncrementDirection
		{
			Lower,
			Higher,
		};

		public enum IncrementAmount
		{
			Small,
			Large,
		};

		public static Hsv IncrementColorChannel(
			Hsv originalHsv,
			ColorPickerHsvChannel channel,
			IncrementDirection direction,
			IncrementAmount amount,
			bool shouldWrap,
			double minBound,
			double maxBound)
		{
			Hsv newHsv = originalHsv;

			if (amount == IncrementAmount.Small || !DownlevelHelper.ToDisplayNameExists())
			{
				// In order to avoid working with small values that can incur rounding issues,
				// we'll multiple saturation and value by 100 to put them in the range of 0-100 instead of 0-1.
				newHsv.S *= 100;
				newHsv.V *= 100;

				// Uno Doc: *valueToIncrement replaced with ref local variable for C#, must be initialized
				ref double valueToIncrement = ref newHsv.H;
				double incrementAmount = 0.0;

				// If we're adding a small increment, then we'll just add or subtract 1.
				// If we're adding a large increment, then we want to snap to the next
				// or previous major value - for hue, this is every increment of 30;
				// for saturation and value, this is every increment of 10.
				switch (channel)
				{
					case ColorPickerHsvChannel.Hue:
						valueToIncrement = ref newHsv.H;
						incrementAmount = amount == IncrementAmount.Small ? 1 : 30;
						break;

					case ColorPickerHsvChannel.Saturation:
						valueToIncrement = ref newHsv.S;
						incrementAmount = amount == IncrementAmount.Small ? 1 : 10;
						break;

					case ColorPickerHsvChannel.Value:
						valueToIncrement = ref newHsv.V;
						incrementAmount = amount == IncrementAmount.Small ? 1 : 10;
						break;

					default:
						throw new InvalidOperationException("Invalid ColorPickerHsvChannel."); // Uno Doc: 'winrt::hresult_error(E_FAIL);'
				}

				double previousValue = valueToIncrement;

				valueToIncrement += (direction == IncrementDirection.Lower ? -incrementAmount : incrementAmount);

				// If the value has reached outside the bounds, we were previous at the boundary, and we should wrap,
				// then we'll place the selection on the other side of the spectrum.
				// Otherwise, we'll place it on the boundary that was exceeded.
				if (valueToIncrement < minBound)
				{
					valueToIncrement = (shouldWrap && previousValue == minBound) ? maxBound : minBound;
				}

				if (valueToIncrement > maxBound)
				{
					valueToIncrement = (shouldWrap && previousValue == maxBound) ? minBound : maxBound;
				}

				// We multiplied saturation and value by 100 previously, so now we want to put them back in the 0-1 range.
				newHsv.S /= 100;
				newHsv.V /= 100;
			}
			else
			{
				// While working with named colors, we're going to need to be working in actual HSV units,
				// so we'll divide the min bound and max bound by 100 in the case of saturation or value,
				// since we'll have received units between 0-100 and we need them within 0-1.
				if (channel == ColorPickerHsvChannel.Saturation ||
					channel == ColorPickerHsvChannel.Value)
				{
					minBound /= 100;
					maxBound /= 100;
				}

				newHsv = FindNextNamedColor(originalHsv, channel, direction, shouldWrap, minBound, maxBound);
			}

			return newHsv;
		}

		// Uno Doc: This function is removed and instead Math.Sign() is used.
		// Implementation of the signum function, which returns the sign of a number while discarding its value.
		/*template <typename T>
		int sgn(T val)
		{
			const int first = (static_cast<T>(0) < val) ? 1 : 0;
			const int second = (static_cast<T>(0) > val) ? 1 : 0;

			return first - second;
		}*/

		public static Hsv FindNextNamedColor(
			Hsv originalHsv,
			ColorPickerHsvChannel channel,
			IncrementDirection direction,
			bool shouldWrap,
			double minBound,
			double maxBound)
		{
			// There's no easy way to directly get the next named color, so what we'll do
			// is just iterate in the direction that we want to find it until we find a color
			// in that direction that has a color name different than our current color name.
			// Once we find a new color name, then we'll iterate across that color name until
			// we find its bounds on the other side, and then select the color that is exactly
			// in the middle of that color's bounds.
			Hsv newHsv = originalHsv;

			string originalColorName = ColorHelper.ToDisplayName(ColorConversion.ColorFromRgba(ColorConversion.HsvToRgb(originalHsv)));
			string newColorName = originalColorName;

			// Uno Doc: *newValue replaced with ref local variable for C#, must be initialized
			double originalValue = 0.0;
			ref double newValue = ref newHsv.H;
			double incrementAmount = 0.0;

			switch (channel)
			{
				case ColorPickerHsvChannel.Hue:
					originalValue = originalHsv.H;
					newValue = ref newHsv.H;
					incrementAmount = 1;
					break;

				case ColorPickerHsvChannel.Saturation:
					originalValue = originalHsv.S;
					newValue = ref newHsv.S;
					incrementAmount = 0.01;
					break;

				case ColorPickerHsvChannel.Value:
					originalValue = originalHsv.V;
					newValue = ref newHsv.V;
					incrementAmount = 0.01;
					break;

				default:
					throw new InvalidOperationException("Invalid ColorPickerHsvChannel."); // Uno Doc: 'winrt::hresult_error(E_FAIL);'
			}

			bool shouldFindMidPoint = true;

			while (newColorName == originalColorName)
			{
				double previousValue = newValue;
				newValue += (direction == IncrementDirection.Lower ? -1 : 1) * incrementAmount;

				bool justWrapped = false;

				// If we've hit a boundary, then either we should wrap or we shouldn't.
				// If we should, then we'll perform that wrapping if we were previously up against
				// the boundary that we've now hit.  Otherwise, we'll stop at that boundary.
				if (newValue > maxBound)
				{
					if (shouldWrap)
					{
						newValue = minBound;
						justWrapped = true;
					}
					else
					{
						newValue = maxBound;
						shouldFindMidPoint = false;
						newColorName = ColorHelper.ToDisplayName(ColorConversion.ColorFromRgba(ColorConversion.HsvToRgb(newHsv)));
						break;
					}
				}
				else if (newValue < minBound)
				{
					if (shouldWrap)
					{
						newValue = maxBound;
						justWrapped = true;
					}
					else
					{
						newValue = minBound;
						shouldFindMidPoint = false;
						newColorName = ColorHelper.ToDisplayName(ColorConversion.ColorFromRgba(ColorConversion.HsvToRgb(newHsv)));
						break;
					}
				}

				if (!justWrapped &&
					previousValue != originalValue &&
					Math.Sign(newValue - originalValue) != Math.Sign(previousValue - originalValue))
				{
					// If we've wrapped all the way back to the start and have failed to find a new color name,
					// then we'll just quit - there isn't a new color name that we're going to find.
					shouldFindMidPoint = false;
					break;
				}

				newColorName = ColorHelper.ToDisplayName(ColorConversion.ColorFromRgba(ColorConversion.HsvToRgb(newHsv)));
			}

			if (shouldFindMidPoint)
			{
				Hsv startHsv = newHsv;
				Hsv currentHsv = startHsv;
				double startEndOffset = 0;
				string currentColorName = newColorName;

				// Uno Doc: *startValue/*currentValue replaced with ref local variables for C#, must be initialized
				ref double startValue = ref startHsv.H;
				ref double currentValue = ref currentHsv.H;
				double wrapIncrement = 0;

				switch (channel)
				{
					case ColorPickerHsvChannel.Hue:
						startValue = ref startHsv.H;
						currentValue = ref currentHsv.H;
						wrapIncrement = 360.0;
						break;

					case ColorPickerHsvChannel.Saturation:
						startValue = ref startHsv.S;
						currentValue = ref currentHsv.S;
						wrapIncrement = 1.0;
						break;

					case ColorPickerHsvChannel.Value:
						startValue = ref startHsv.V;
						currentValue = ref currentHsv.V;
						wrapIncrement = 1.0;
						break;

					default:
						throw new InvalidOperationException("Invalid ColorPickerHsvChannel."); // Uno Doc: 'winrt::hresult_error(E_FAIL);'
				}

				while (newColorName == currentColorName)
				{
					currentValue += (direction == IncrementDirection.Lower ? -1 : 1) * incrementAmount;

					// If we've hit a boundary, then either we should wrap or we shouldn't.
					// If we should, then we'll perform that wrapping if we were previously up against
					// the boundary that we've now hit.  Otherwise, we'll stop at that boundary.
					if (currentValue > maxBound)
					{
						if (shouldWrap)
						{
							currentValue = minBound;
							startEndOffset = maxBound - minBound;
						}
						else
						{
							currentValue = maxBound;
							break;
						}
					}
					else if (currentValue < minBound)
					{
						if (shouldWrap)
						{
							currentValue = maxBound;
							startEndOffset = minBound - maxBound;
						}
						else
						{
							currentValue = minBound;
							break;
						}
					}

					currentColorName = ColorHelper.ToDisplayName(ColorConversion.ColorFromRgba(ColorConversion.HsvToRgb(currentHsv)));
				}

				newValue = (startValue + currentValue + startEndOffset) / 2;

				// Dividing by 2 may have gotten us halfway through a single step, so we'll
				// remove that half-step if it exists.
				double leftoverValue = Math.Abs(newValue);

				while (leftoverValue > incrementAmount)
				{
					leftoverValue -= incrementAmount;
				}

				newValue -= leftoverValue;

				while (newValue < minBound)
				{
					newValue += wrapIncrement;
				}

				while (newValue > maxBound)
				{
					newValue -= wrapIncrement;
				}
			}

			return newHsv;
		}

		public static double IncrementAlphaChannel(
			double originalAlpha,
			IncrementDirection direction,
			IncrementAmount amount,
			bool shouldWrap,
			double minBound,
			double maxBound)
		{
			// In order to avoid working with small values that can incur rounding issues,
			// we'll multiple alpha by 100 to put it in the range of 0-100 instead of 0-1.
			originalAlpha *= 100;

			const double smallIncrementAmount = 1;
			const double largeIncrementAmount = 10;

			if (amount == IncrementAmount.Small)
			{
				originalAlpha += (direction == IncrementDirection.Lower ? -1 : 1) * smallIncrementAmount;
			}
			else
			{
				if (direction == IncrementDirection.Lower)
				{
					originalAlpha = Math.Ceiling((originalAlpha - largeIncrementAmount) / largeIncrementAmount) * largeIncrementAmount;
				}
				else
				{
					originalAlpha = Math.Floor((originalAlpha + largeIncrementAmount) / largeIncrementAmount) * largeIncrementAmount;
				}
			}

			// If the value has reached outside the bounds and we should wrap, then we'll place the selection
			// on the other side of the spectrum.  Otherwise, we'll place it on the boundary that was exceeded.
			if (originalAlpha < minBound)
			{
				originalAlpha = shouldWrap ? maxBound : minBound;
			}

			if (originalAlpha > maxBound)
			{
				originalAlpha = shouldWrap ? minBound : maxBound;
			}

			// We multiplied alpha by 100 previously, so now we want to put it back in the 0-1 range.
			return originalAlpha / 100;
		}

		public static async void CreateCheckeredBackgroundAsync(
			int width,
			int height,
			Color checkerColor,
			ArrayList<byte> bgraCheckeredPixelData,
			IAsyncAction asyncActionToAssign,
			CoreDispatcher dispatcherHelper,
			Action<WriteableBitmap> completedFunction)
		{
			if (width == 0 || height == 0)
			{
				return;
			}

			bgraCheckeredPixelData.Capacity = width * height * 4;

			//WorkItemHandler workItemHandler =
			//(IAsyncAction workItem) =>
			await Task.Run(() =>
			{
				for (int y = 0; y < height; y++)
				{
					for (int x = 0; x < width; x++)
					{
						//if (workItem.Status == AsyncStatus.Canceled)
						//{
						//	break;
						//}

						// We want the checkered pattern to alternate both vertically and horizontally.
						// In order to achieve that, we'll toggle visibility of the current pixel on or off
						// depending on both its x- and its y-position.  If x == CheckerSize, we'll turn visibility off,
						// but then if y == CheckerSize, we'll turn it back on.
						// The below is a shorthand for the above intent.
						bool pixelShouldBeBlank = ((x / CheckerSize) + (y / CheckerSize)) % 2 == 0 ? true : false;

						if (pixelShouldBeBlank)
						{
							bgraCheckeredPixelData.Add(0);
							bgraCheckeredPixelData.Add(0);
							bgraCheckeredPixelData.Add(0);
							bgraCheckeredPixelData.Add(0);
						}
						else
						{
							bgraCheckeredPixelData.Add((byte)(checkerColor.B * checkerColor.A / 255));
							bgraCheckeredPixelData.Add((byte)(checkerColor.G * checkerColor.A / 255));
							bgraCheckeredPixelData.Add((byte)(checkerColor.R * checkerColor.A / 255));
							bgraCheckeredPixelData.Add(checkerColor.A);
						}
					}
				}
			});

			//if (asyncActionToAssign != null)
			//{
			//	asyncActionToAssign.Cancel();
			//}

			//asyncActionToAssign = ThreadPool.RunAsync(workItemHandler);
			//asyncActionToAssign.Completed = new AsyncActionCompletedHandler(
			//async (IAsyncAction asyncInfo, AsyncStatus asyncStatus) =>
			//{
			//	if (asyncStatus != AsyncStatus.Completed)
			//	{
			//		return;
			//	}

			//	asyncActionToAssign = null;

			// Uno Doc: Assumed normal priority is acceptable
			await dispatcherHelper.RunAsync(CoreDispatcherPriority.Normal, () =>
			{
				WriteableBitmap checkeredBackgroundBitmap = CreateBitmapFromPixelData(width, height, bgraCheckeredPixelData);
				completedFunction?.Invoke(checkeredBackgroundBitmap);
			});
			//});
		}

		public static WriteableBitmap CreateBitmapFromPixelData(
			int pixelWidth,
			int pixelHeight,
			ArrayList<byte> bgraPixelData)
		{
			// IBufferByteAccess isn't included in any WinMD file, because its sole method - Buffer() -
			// allows direct pointer access, which isn't applicable to C#.  In C#, there's a separate ToStream()
			// method that similarly allows read access.  As a result, we have no C++/WinMD projection for IBufferByteAccess;
			// we need to flip to ABI in this circumstance to do what this method requires.
			WriteableBitmap bitmap = new WriteableBitmap(pixelWidth, pixelHeight);

			// Uno Doc:
			// Since Uno uses C#, the method of converting to a bitmap was changed to what is below.
			// A new 'ArrayList' class is used to avoid an extra copy using List<T>.ToArray() here.
			using (Stream stream = bitmap.PixelBuffer.AsStream())
			{
				stream.Write(bgraPixelData.Array, 0, bgraPixelData.Count);
			}

			// Uno Doc: The following code is not supported in C# and is removed.
			//byte *pixelBuffer = nullptr;
			//winrt::check_hresult(bitmap.PixelBuffer().as<Windows::Storage::Streams::IBufferByteAccess>()->Buffer(&pixelBuffer));

			//std::memcpy(pixelBuffer, (*bgraPixelData).data(), (*bgraPixelData).size());
			//bitmap.Invalidate();

			return bitmap;
		}

		// UNO TODO: This method is only used when drawing the color spectrum using WinUI composition API's which are currently unsupported.
		// This method is only partially ported to C#.
		/*public static LoadedImageSurface CreateSurfaceFromPixelData(
			int pixelWidth,
			int pixelHeight,
			List<byte> bgraPixelData)
		{
			// Uno Doc: Removed 'MUX_ASSERT(SharedHelpers::IsRS2OrHigher());'

			// LoadedImageSurface uses WIC to load images, so we need to put the pixel data into an image format.
			// We'll use the BMP format, since it stores uncompressed pixel data.
			List<byte> bmpData;

			// Size is header (14 bytes) + DIB header (40 bytes) + Pixel array (size of bgraPixelData).
			const size_t dibHeaderSize = 40;
			const size_t headerSize = 14 + dibHeaderSize;
			const size_t fileSize = headerSize + bgraPixelData.Count;

			// Header field to identify as BMP.
			bmpData.Add(Encoding.UTF8.GetBytes("B")[0]);
			bmpData.Add(Encoding.UTF8.GetBytes("M")[0]);

			// File size.  Note that the BMP format is always little-endian.
			bmpData.Add((byte)(fileSize & 0x000000FF));
			bmpData.Add((byte)((fileSize & 0x0000FF00) >> 8));
			bmpData.Add((byte)((fileSize & 0x00FF0000) >> 16));
			bmpData.Add((byte)((fileSize & 0xFF000000) >> 24));

			// Reserved for application-specific usage.  We don't care about these bytes.
			bmpData.Add(0);
			bmpData.Add(0);
			bmpData.Add(0);
			bmpData.Add(0);

			// Offset of the pixel data from the start.  Since our header is 14 + 12 bytes long, it starts after that.
			var pixelDataOffset = (UInt32)(headerSize);
			bmpData.Add((byte)(pixelDataOffset & 0x000000FF));
			bmpData.Add((byte)((pixelDataOffset & 0x0000FF00) >> 8));
			bmpData.Add((byte)((pixelDataOffset & 0x00FF0000) >> 16));
			bmpData.Add((byte)((pixelDataOffset & 0xFF000000) >> 24));

			// Beginning of DIB header.  First 4 bytes are the size of the header (12 bytes in our case).
			bmpData.Add((byte)(dibHeaderSize & 0x000000FF));
			bmpData.Add((byte)((dibHeaderSize & 0x0000FF00) >> 8));
			bmpData.Add((byte)((dibHeaderSize & 0x00FF0000) >> 16));
			bmpData.Add((byte)((dibHeaderSize & 0xFF000000) >> 24));

			// Bitmap width in pixels (32-bit).
			var bitmapWidth = (UInt32)(pixelWidth);
			bmpData.Add((byte)(bitmapWidth & 0x000000FF));
			bmpData.Add((byte)((bitmapWidth & 0x0000FF00) >> 8));
			bmpData.Add((byte)((bitmapWidth & 0x00FF0000) >> 16));
			bmpData.Add((byte)((bitmapWidth & 0xFF000000) >> 24));

			// Bitmap height in pixels (32-bit).
			var bitmapHeight = (UInt32)(pixelHeight);
			bmpData.Add((byte)(bitmapHeight & 0x000000FF));
			bmpData.Add((byte)((bitmapHeight & 0x0000FF00) >> 8));
			bmpData.Add((byte)((bitmapHeight & 0x00FF0000) >> 16));
			bmpData.Add((byte)((bitmapHeight & 0xFF000000) >> 24));

			// Color plane count.
			UInt16 colorPlaneCount = 1;
			bmpData.Add((byte)(colorPlaneCount & 0x00FF));
			bmpData.Add((byte)((colorPlaneCount & 0xFF00) >> 8));

			// Bits per pixel.
			UInt16 bitsPerPixel = 32;
			bmpData.Add((byte)(bitsPerPixel & 0x00FF));
			bmpData.Add((byte)((bitsPerPixel & 0xFF00) >> 8));

			// Compression method.  0 means uncompressed.
			UInt32 compressionMethod = 0;
			bmpData.Add((byte)(compressionMethod & 0x000000FF));
			bmpData.Add((byte)((compressionMethod & 0x0000FF00) >> 8));
			bmpData.Add((byte)((compressionMethod & 0x00FF0000) >> 16));
			bmpData.Add((byte)((compressionMethod & 0xFF000000) >> 24));

			// Image size.  Not needed for uncompressed images.
			UInt32 imageSize = 0;
			bmpData.Add((byte)(imageSize & 0x000000FF));
			bmpData.Add((byte)((imageSize & 0x0000FF00) >> 8));
			bmpData.Add((byte)((imageSize & 0x00FF0000) >> 16));
			bmpData.Add((byte)((imageSize & 0xFF000000) >> 24));

			// Horizontal resolution.  We don't care about this value.
			UInt32 horizontalResolution = 1;
			bmpData.Add((byte)(horizontalResolution & 0x000000FF));
			bmpData.Add((byte)((horizontalResolution & 0x0000FF00) >> 8));
			bmpData.Add((byte)((horizontalResolution & 0x00FF0000) >> 16));
			bmpData.Add((byte)((horizontalResolution & 0xFF000000) >> 24));

			// Vertical resolution.  We don't care about this value.
			UInt32 verticalResolution = 1;
			bmpData.Add((byte)(verticalResolution & 0x000000FF));
			bmpData.Add((byte)((verticalResolution & 0x0000FF00) >> 8));
			bmpData.Add((byte)((verticalResolution & 0x00FF0000) >> 16));
			bmpData.Add((byte)((verticalResolution & 0xFF000000) >> 24));

			// Number of colors in the palette.  0 means use the default.
			UInt32 colorsInPalette = 0;
			bmpData.Add((byte)(colorsInPalette & 0x000000FF));
			bmpData.Add((byte)((colorsInPalette & 0x0000FF00) >> 8));
			bmpData.Add((byte)((colorsInPalette & 0x00FF0000) >> 16));
			bmpData.Add((byte)((colorsInPalette & 0xFF000000) >> 24));

			// Important colors.  0 means all colors are important.
			UInt32 importantColors = 0;
			bmpData.Add((byte)(importantColors & 0x000000FF));
			bmpData.Add((byte)((importantColors & 0x0000FF00) >> 8));
			bmpData.Add((byte)((importantColors & 0x00FF0000) >> 16));
			bmpData.Add((byte)((importantColors & 0xFF000000) >> 24));

			// Pixel data.  BMP images are stored upside-down, so we need to copy the image data backwards.
			bmpData.resize(fileSize);
			UInt32 stride = pixelWidth * 4;

			for (int y = pixelHeight - 1; y >= 0; y--)
			{
				memcpy(bmpData.data() + (headerSize + (pixelHeight - 1 - y) * stride), (*bgraPixelData).data() + (y * stride), stride);
			}

			InMemoryRandomAccessStream stream = SharedHelpers::CreateStreamFromBytes(winrt::array_view<const byte>(bmpData));

			return LoadedImageSurface.StartLoadFromStream(stream);
		}*/

		public static void CancelAsyncAction(IAsyncAction action)
		{
			if (action != null && action.Status == AsyncStatus.Started)
			{
				action.Cancel();
			}
		}
	}
}

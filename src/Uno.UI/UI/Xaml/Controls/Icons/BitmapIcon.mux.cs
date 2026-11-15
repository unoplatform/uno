// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference icon.cpp

#nullable enable

using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Microsoft.UI.Xaml.Controls;

partial class BitmapIcon
{
	private protected override void ApplyTemplate(out bool fAddedVisuals)
	{
		fAddedVisuals = false;

		if (GetChildElementNoRef() is null)
		{
			// Create the new Image object.
			var pImage = new Image();
			AddChildElement(pImage);

			// Set the value on the image's source property
			ReloadSourceImage();

			fAddedVisuals = true;
		}

		// Make sure our writeable bitmap is up-to-date with the foreground color value (inherited
		// or not).
		// TODO Uno specific: WinUI skips non-solid brushes here because the bitmap was already filled
		// white when it was decoded. Uno has no such bitmap, so white is applied here.
		ApplyForegroundColorToImage(Foreground as SolidColorBrush);
	}

	private protected override void OnForegroundChanged(DependencyPropertyChangedEventArgs e) =>
		ApplyForegroundColorToImage(e.NewValue as SolidColorBrush);

	//-------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Reloads the source image when our source URI changes.
	//
	//-------------------------------------------------------------------------
	private void ReloadSourceImage()
	{
		// TODO Uno specific: WinUI decodes the bitmap into a tinted WriteableBitmap, Uno shows the bitmap
		// directly and tints it with Image.MonochromeColor (see ApplyForegroundColorToImage).
		if (GetChildElementNoRef() is Image image)
		{
			image.Source = UriSource is { } uriSource ? new BitmapImage(uriSource) : null;
		}
	}

	private void ApplyForegroundColorToImage(SolidColorBrush? pBrush)
	{
		if (GetChildElementNoRef() is Image image)
		{
			if (ShowAsMonochrome)
			{
				// If a valid brush has been set, then we'll use its RGB values, otherwise
				// we'll use white.
				image.MonochromeColor = pBrush?.Color ?? Colors.White;
			}
#if HAS_UNO
			else
			{
				// TODO Uno specific: WinUI keeps the previous tint until the image reloads.
				image.MonochromeColor = null;
			}
#endif
		}
	}
}

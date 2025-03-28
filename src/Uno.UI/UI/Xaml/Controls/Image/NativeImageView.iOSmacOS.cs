using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;

#if __IOS__
using _UIImage = UIKit.UIImage;
using _UIImageView = UIKit.UIImageView;
#elif __MACOS__
using _UIImage = AppKit.NSImage;
using _UIImageView = AppKit.NSImageView;
#endif

namespace Windows.UI.Xaml.Controls;

internal partial class NativeImageView : _UIImageView
{
	public bool HasImage => Image != null;

	public CGSize ImageSize => Image?.Size ?? default;

	public void Reset() => Image = null;

	public void SetImage(_UIImage image) => Image = image;

}

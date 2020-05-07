using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using UIKit;

namespace Windows.UI.Xaml.Controls
{
	internal partial class NativeImage : UIImageView
	{
		public bool HasImage => Image != null;

		public CGSize ImageSize => Image?.Size ?? default;

		public void Reset() => Image = null;

		public void SetImage(UIImage image) => Image = image;

	}
}

using Windows.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Text;
using CoreGraphics;
using Windows.UI.Xaml;
using System.Globalization;
using UIKit;

namespace Uno.UI.Media
{
	public class CGPathGeometry : Geometry
	{
		CGPath _path;
		public CGPathGeometry(CGPath path)
		{
			_path = path;
		}

		public override void Dispose()
		{
			_path.Dispose();
		}

		public override CGPath ToCGPath()
		{
			return _path;
		}

		public override UIImage ToNativeImage()
		{
			throw new NotImplementedException();
		}

		public override UIImage ToNativeImage(CGSize targetSize, UIColor color = null, Thickness margin = default(Thickness))
		{
			throw new NotImplementedException();
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UIKit;

namespace Uno.UI
{
    internal static class UIImageHelper
    {
		public static UIImage FromUri(Uri uri)
		{
			if (uri == null)
			{
				return null;
			}

			var bundleName = Path.GetFileName(uri.AbsolutePath);
			var bundlePath = uri.PathAndQuery.TrimStart(new[] { '/' });

			var image = UIImage.FromFile(bundleName) ?? UIImage.FromFile(bundlePath); 
			return image;
		}
	}
}

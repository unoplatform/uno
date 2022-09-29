#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Foundation;
using Uno.UI;

namespace Uno.Extensions
{
	internal static class NSIndexPathExtensions
	{
		public static NSIndexPath ToNSIndexPath(this IndexPath indexPath)
		{
			return NSIndexPath.FromRowSection(indexPath.Row, indexPath.Section);
		}

		public static IndexPath ToIndexPath(this NSIndexPath nsIndexPath)
		{
			return IndexPath.FromNSIndexPath(nsIndexPath);
		}
	}
}

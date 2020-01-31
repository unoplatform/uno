using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI
{
	/// <summary>
	/// An index to an entry in a grouped items source.
	/// </summary>
	public partial struct IndexPath
	{
		internal static IndexPath FromNSIndexPath(Foundation.NSIndexPath indexPath)
		{
			return new IndexPath((int)indexPath.Item, (int)indexPath.Section);
		}
	}
}

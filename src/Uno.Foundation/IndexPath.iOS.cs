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
		internal static IndexPath FromNSIndexPath(global::Foundation.NSIndexPath indexPath)
		{
			return new IndexPath(indexPath.Row, indexPath.Section);
		}
	}
}

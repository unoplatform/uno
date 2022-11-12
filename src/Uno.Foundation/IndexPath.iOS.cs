using Foundation;

namespace Uno.UI;

/// <summary>
/// An index to an entry in a grouped items source.
/// </summary>
public partial struct IndexPath
{
    internal static IndexPath FromNSIndexPath(NSIndexPath indexPath) => new IndexPath(indexPath.Row, indexPath.Section);
}

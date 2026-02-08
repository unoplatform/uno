#nullable enable
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class GroupItem
	{
		/// <summary>
		/// Minimal compatibility shim used by automation peers to retrieve the templated ItemsControl inside a GroupItem.
		/// Returns null when not available.
		/// </summary>
		public ItemsControl? GetTemplatedItemsControl()
		{
			// Uno's GroupItem doesn't currently expose this helper; callers that need the templated items
			// control should fall back to visual-tree based lookup. Returning null forces callers to
			// use the fallback path which is safe for compilation and runtime.
			return null;
		}
	}
}

using System.Collections.Generic;

namespace Uno.UI.Helpers.WinUI
{
	internal static class CollectionHelper
	{
		internal static void unique_push_back<T>(IList<T> items, T newItem)
		{
			if (!items.Contains(newItem))
			{
				items.Add(newItem);
			}
		}
	}
}

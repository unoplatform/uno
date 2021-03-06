#nullable enable

using System;
using System.Threading.Tasks;

namespace Windows.Storage.Internal
{
	internal static class StorageItemNameGenerator
	{
		public static async Task<string> FindAvailableNumberedNameAsync(string desiredName, StorageFolder parent, Func<int, string> numberedNameBuilder)
		{
			var plainFile = await parent.TryGetItemAsync(desiredName);
			if (plainFile == null)
			{
				return desiredName;
			}

			int number = 1;
			IStorageItem? item;
			string itemName = desiredName;
			do
			{
				itemName = numberedNameBuilder(++number);
				item = await parent.TryGetItemAsync(itemName);
			} while (item != null);

			return itemName;
		}
	}
}

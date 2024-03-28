using System.Collections.Generic;

namespace Windows.UI.Xaml.Controls
{
	internal interface DefinitionCollectionBase
	{
		IEnumerable<DefinitionBase> GetItems();
		int Count { get; }
		DefinitionBase GetItem(int index);
		internal void Lock();
		internal void Unlock();
	}
}

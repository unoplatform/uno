using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls
{
	internal interface DefinitionCollectionBase
	{
		int Count { get; }
		DefinitionBase GetItem(int index);
		internal void Lock();
		internal void Unlock();
	}
}

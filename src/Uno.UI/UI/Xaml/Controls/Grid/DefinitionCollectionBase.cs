using System.Collections.Generic;

namespace Windows.UI.Xaml.Controls
{
	internal interface DefinitionCollectionBase : IList<DefinitionBase>
	{
		DefinitionBase GetItem(int index);
		internal void Lock();
		internal void Unlock();
	}
}

using System;
using System.Linq;

namespace Microsoft.UI.Xaml.Controls
{
	internal interface ILayoutDataInfoProvider
	{
		int GetTotalItemCount();

		int GetTotalGroupCount();

		void GetGroupInformationFromItemIndex(int itemIndex, out int indexOfGroup, out int indexInsideGroup, out int itemCountInGroup);

		void GetGroupInformationFromGroupIndex(int groupIndex, out int startItemIndex, out int itemCountInGroup);
	}
}

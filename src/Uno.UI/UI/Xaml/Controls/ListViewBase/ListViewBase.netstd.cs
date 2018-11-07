#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;

namespace Windows.UI.Xaml.Controls
{
	public partial class ListViewBase
	{

		private int PageSize => throw new NotImplementedException();

		private void Refresh()
		{
			//TODO
		}

		private void AddItems(int firstItem, int count, int section) => throw new NotImplementedException();

		private void RemoveItems(int firstItem, int count, int section) => throw new NotImplementedException();

		private void AddGroup(int groupIndexInView) => throw new NotImplementedException();

		private void RemoveGroup(int groupIndexInView) => throw new NotImplementedException();

		private void ReplaceGroup(int groupIndexInView) => throw new NotImplementedException();

		private ContentControl ContainerFromGroupIndex(int groupIndex) => throw new NotImplementedException();

		private void TryLoadMoreItems()
		{
			//TODO: ISupportIncrementalLoading
		}
	}
}

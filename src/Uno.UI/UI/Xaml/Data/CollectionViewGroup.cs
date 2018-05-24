using System;
using System.Collections.Generic;
using System.Text;
using Windows.Foundation.Collections;

namespace Windows.UI.Xaml.Data
{
	internal class CollectionViewGroup : ICollectionViewGroup
	{
		public CollectionViewGroup(object group)
		{
			Group = group;

			GroupItems = ObservableVectorWrapper.Create(group);
		}
		public object Group { get; }

		public IObservableVector<object> GroupItems { get; }
	}
}

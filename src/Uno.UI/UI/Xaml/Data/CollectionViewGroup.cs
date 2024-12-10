using System;
using System.Collections.Generic;
using System.Text;
using Uno.UI.DataBinding;
using Windows.Foundation.Collections;

namespace Windows.UI.Xaml.Data
{
	internal class CollectionViewGroup : ICollectionViewGroup
	{
		private readonly BindingPath _bindingPath;

		public CollectionViewGroup(object group, PropertyPath itemsPath)
		{
			Group = group;

			if (itemsPath != null)
			{
				_bindingPath = new BindingPath(itemsPath.Path, null);
				_bindingPath.DataContext = group;

				GroupItems = ObservableVectorWrapper.Create(_bindingPath.Value);
			}
			else
			{
				GroupItems = ObservableVectorWrapper.Create(group);
			}
		}
		public object Group { get; }


		public IObservableVector<object> GroupItems { get; }
	}
}

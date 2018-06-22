using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = "Items")]
	public partial class NativePivotPresenter : Control
	{
		public NativePivotPresenter()
		{
			Initialize();
		}

		public ItemCollection Items { get; private set; }

		private void Initialize()
		{
			Items = new ItemCollection();
			Items.VectorChanged += Items_VectorChanged;
			InitializePartial();
		}

		private void Items_VectorChanged(Foundation.Collections.IObservableVector<object> sender, Foundation.Collections.IVectorChangedEventArgs @event)
		{
			if (@event.CollectionChange == Foundation.Collections.CollectionChange.ItemInserted)
			{
				UpdateItems();
			}
		}

		partial void InitializePartial();

		partial void UpdateItems();
	}
}

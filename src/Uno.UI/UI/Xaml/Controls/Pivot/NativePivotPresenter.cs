using System;
using System.Text;
using Windows.UI.Xaml.Markup;
using Windows.Foundation.Collections;

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

		private void Items_VectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
		{
			if (@event.CollectionChange == CollectionChange.ItemInserted)
			{
				UpdateItems();
			}
		}

		partial void InitializePartial();

		partial void UpdateItems();
	}
}

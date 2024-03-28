using System;

namespace Windows.UI.Xaml.Controls
{
	public class ItemSelectedEventArgs : EventArgs
	{
		public ItemSelectedEventArgs(object item)
		{
			Item = item;
		}

		new public static readonly ItemSelectedEventArgs Empty = new ItemSelectedEventArgs(null);

		public object Item
		{
			get;
			private set;
		}
	}
}


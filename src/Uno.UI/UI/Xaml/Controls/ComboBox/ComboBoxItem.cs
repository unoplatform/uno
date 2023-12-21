using Windows.System;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ComboBoxItem : SelectorItem
	{
		public ComboBoxItem()
		{
			DefaultStyleKey = typeof(ComboBoxItem);
		}

		protected override void OnKeyDown(KeyRoutedEventArgs args)
		{
			if (ItemsControl.ItemsControlFromItemContainer(this) is ComboBox comboBox)
			{
				if (args.Key == VirtualKey.Enter && comboBox.IsDropDownOpen)
				{
					var item = comboBox.ItemFromContainer(this);
					if (item != null)
					{
						comboBox.SelectedItem = item;
						comboBox.IsDropDownOpen = false;
						args.Handled = true;
					}
				}

				if (!args.Handled)
				{
					// Fallback to combobox keydown handling
					args.Handled = comboBox.TryHandleKeyDown(args, this);
				}
			}

			base.OnKeyDown(args);
		}
	}
}

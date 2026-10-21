using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Extensions;
using Uno.UI.Samples.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ComboBox
{
	[Sample("ComboBox", Name = "ComboBox_ComboBoxItem_Selection")]
	public sealed partial class ComboBox_ComboBoxItem_Selection : UserControl
	{
		public ComboBox_ComboBoxItem_Selection()
		{
			this.InitializeComponent();

			_combo.SelectionChanged += SelectionChanged;

			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			var items = _combo.Items;
			var item = items.First() as FrameworkElement;
			item.Loaded += (snd, evt) =>
			{
				_combo1Txt.Text = GetVisualTree(item);
			};

			var items2 = _combo2.Items;
			var item2 = items2.First() as FrameworkElement;
			item2.Loaded += (snd, evt) =>
			{
				_combo2Txt.Text = GetVisualTree(item2);
			};
		}

		private void SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var items = _combo.Items;
			var item = _combo.SelectedItem;
			var item2 = e.AddedItems.ToArray();
		}

		private string GetVisualTree(FrameworkElement element)
		{
			IEnumerable<string> GetElements()
			{
				foreach (var o in element.GetAllParents())
				{
					var e = o as FrameworkElement;
					yield return $"{o.GetType().Name}[{e?.Name}]-{o}";
					if (e is Microsoft.UI.Xaml.Controls.ComboBox)
					{
						yield break;
					}
				}
			}

			return string.Join("\n->", GetElements());
		}

		private void ChangeSelectionButton_Click(object sender, RoutedEventArgs e)
		{
			var newSelectedIndex = _combo2.SelectedIndex;
			newSelectedIndex++;
			if (newSelectedIndex >= _combo2.Items.Count)
			{
				newSelectedIndex = 0;
			}
			_combo2.SelectedIndex = newSelectedIndex;
		}
	}
}

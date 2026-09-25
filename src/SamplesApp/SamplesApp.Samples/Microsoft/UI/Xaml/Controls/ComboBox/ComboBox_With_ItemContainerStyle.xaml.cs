using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Wasm.Windows_UI_Xaml_Controls.ComboBox
{
	[Sample("ComboBox", Name = "ComboBox_With_ItemContainerStyle")]
	public sealed partial class ComboBox_With_ItemContainerStyle : UserControl
	{
		public ComboBox_With_ItemContainerStyle()
		{
			this.InitializeComponent();
			this.DataContext = this;

			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			this.Loaded -= OnLoaded;

			InitList();
		}

		public List<string> Lst { get; set; }

		private void InitList()
		{
			Lst = new List<string>();
			Lst.Add("Item 1");
			Lst.Add("Item 2");
			Lst.Add("Item 3");
			Lst.Add("Item 4");
			Lst.Add("Item 5");
			Lst.Add("Item 6");
			Lst.Add("Item 7");
			Lst.Add("Item 8");
			Lst.Add("Item 9");
			Lst.Add("Item 10");

			Box.ItemsSource = Lst.ToArray();

			void handleSelection(object sender, object args)
			{
				var item = (string)Box.SelectedItem;
				Txt.Text = "Current selection : " + item;
			}

			Box.Loaded += (s, e) => Box.SelectionChanged += handleSelection;
			Box.Unloaded += (s, e) => Box.SelectionChanged -= handleSelection;
		}
	}
}

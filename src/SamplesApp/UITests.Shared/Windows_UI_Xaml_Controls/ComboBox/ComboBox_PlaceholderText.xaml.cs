using Microsoft.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;

namespace SamplesApp.Wasm.Windows_UI_Xaml_Controls.ComboBox
{
	[Sample("ComboBox", Name = "ComboBox_PlaceholderText", ViewModelType = typeof(ListViewViewModel))]
	public sealed partial class ComboBox_PlaceholderText : UserControl
	{
		public ComboBox_PlaceholderText()
		{
			this.InitializeComponent();
		}

		void ResetSelection(object sender, RoutedEventArgs e)
		{
			TestBox.SelectedItem = null;
			TestBox2.SelectedItem = null;
		}
	}
}

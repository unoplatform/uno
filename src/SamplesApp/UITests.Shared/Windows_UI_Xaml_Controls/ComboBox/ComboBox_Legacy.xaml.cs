using Microsoft.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Wasm.Windows_UI_Xaml_Controls.ComboBox
{
	[Sample("ComboBox", Name = "ComboBox_Legacy", ViewModelType = typeof(ListViewViewModel))]
	public sealed partial class ComboBox_Legacy : UserControl
	{
		public ComboBox_Legacy()
		{
			this.InitializeComponent();
		}
	}
}

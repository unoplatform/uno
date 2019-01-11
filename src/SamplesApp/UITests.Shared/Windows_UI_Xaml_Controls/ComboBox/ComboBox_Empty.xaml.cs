using Windows.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.ComboBox.Models;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Wasm.Windows_UI_Xaml_Controls.ComboBox
{
	[SampleControlInfo("ComboBox", "ComboBox_Empty", typeof(ListViewViewModel))]
	public sealed partial class ComboBox_Empty : UserControl
	{
		public ComboBox_Empty()
		{
			this.InitializeComponent();
		}
	}
}

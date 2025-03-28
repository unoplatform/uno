using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListView_TextBox", typeof(ListViewViewModel), description: "On iOS, Textbox crashes with TextWrapping or AcceptsReturn")]
	public sealed partial class ListView_TextBox : UserControl
	{
		public ListView_TextBox()
		{
			this.InitializeComponent();
		}
	}
}

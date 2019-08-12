using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListView_Changing_Text", typeof(ListViewViewModel), description: "On Android, the text doesn't always change when toggling the CheckBox.")]
	public sealed partial class ListView_Changing_Text : UserControl
	{
		public ListView_Changing_Text()
		{
			this.InitializeComponent();
		}
	}
}

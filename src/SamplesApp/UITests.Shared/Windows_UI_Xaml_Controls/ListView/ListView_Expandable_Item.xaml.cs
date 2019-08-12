using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListView_Expandable_Item", typeof(ListViewViewModel), description: "ListView with items that can be expanded using the toggle buttons.")]
	public sealed partial class ListView_Expandable_Item : UserControl
	{
		public ListView_Expandable_Item()
		{
			this.InitializeComponent();
		}
	}
}

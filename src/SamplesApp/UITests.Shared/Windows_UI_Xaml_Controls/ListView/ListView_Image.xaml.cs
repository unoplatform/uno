using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListView_Image", typeof(ListViewViewModel), description: "On Android, images don't appear until items are recycled.")]
	public sealed partial class ListView_Image : UserControl
	{
		public ListView_Image()
		{
			this.InitializeComponent();
		}
	}
}

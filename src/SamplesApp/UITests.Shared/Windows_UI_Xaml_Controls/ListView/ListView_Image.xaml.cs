using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", Name = "ListView_Image", ViewModelType = typeof(ListViewViewModel), Description = "On Android, images don't appear until items are recycled.")]
	public sealed partial class ListView_Image : UserControl
	{
		public ListView_Image()
		{
			this.InitializeComponent();
		}
	}
}

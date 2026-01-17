using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SamplesApp.Windows_UI_Xaml_Controls.Models;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", "ListViewChangeHeight", typeof(ListViewViewModel), Description: "ListView with variable-height items whose available height can be changed. This shouldn't screw up the sizes of the items.")]
	public sealed partial class ListViewChangeHeight : UserControl
	{
		public ListViewChangeHeight()
		{
			this.InitializeComponent();
		}

		private void ToggleHeight(object sender, RoutedEventArgs e)
		{
			const double small = 200;
			const double tall = 300;
			if (FixedHeightContainer.Height == tall)
			{
				FixedHeightContainer.Height = small;
			}
			else
			{
				FixedHeightContainer.Height = tall;
			}
		}
	}
}

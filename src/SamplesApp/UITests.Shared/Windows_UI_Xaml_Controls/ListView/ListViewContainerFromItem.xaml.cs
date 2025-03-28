using System.Linq;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[SampleControlInfo("ListView", "ListViewContainerFromItem", description: "ListView where ContainerFromItem+TransformToVisual is used to align indicator when SelectedItem changes")]
	public sealed partial class ListViewContainerFromItem : UserControl
	{
		public ListViewContainerFromItem()
		{
			this.InitializeComponent();
		}

		private void TargetListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = e.AddedItems.First();
			var list = sender as Windows.UI.Xaml.Controls.ListView;
			var container = list.ContainerFromItem(item);

			if (container != null)
			{
				var transform = (container as Windows.UI.Xaml.UIElement).TransformToVisual(list);
				var offset = transform.TransformPoint(new Point());
				Windows.UI.Xaml.Controls.Canvas.SetTop(TrackerView, offset.Y);
			}
		}
	}
}

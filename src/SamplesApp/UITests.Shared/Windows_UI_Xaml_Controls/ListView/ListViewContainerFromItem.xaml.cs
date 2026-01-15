using System.Linq;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", Name = "ListViewContainerFromItem", Description = "ListView where ContainerFromItem+TransformToVisual is used to align indicator when SelectedItem changes")]
	public sealed partial class ListViewContainerFromItem : UserControl
	{
		public ListViewContainerFromItem()
		{
			this.InitializeComponent();
		}

		private void TargetListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var item = e.AddedItems.First();
			var list = sender as Microsoft.UI.Xaml.Controls.ListView;
			var container = list.ContainerFromItem(item);

			if (container != null)
			{
				var transform = (container as Microsoft.UI.Xaml.UIElement).TransformToVisual(list);
				var offset = transform.TransformPoint(new Point());
				Microsoft.UI.Xaml.Controls.Canvas.SetTop(TrackerView, offset.Y);
			}
		}
	}
}

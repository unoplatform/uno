using System.Collections.Generic;
using Uno.UI.Extensions;
using Uno.UI.Samples.Controls;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", Name = "ListView_ChangeView", Description = "ListView sample demonstating the ChangeView function works when the style changes. When changing the style, a blue rectangle should appear instead of the list. when clicking on \"Change View\", the blue rectangle should change to a pink rectangle.")]
	public sealed partial class ListView_ChangeView : UserControl
	{
		public ListView_ChangeView()
		{
			this.InitializeComponent();
		}

		private void ChangeViewButtonClick(object sender, RoutedEventArgs e)
		{
			var scrollViewer = MyListView.FindFirstChild<ScrollViewer>();

			scrollViewer.ChangeView(null, 1020, null);
		}

		private void ChangeStyleButtonClick(object sender, RoutedEventArgs e)
		{
			MyListView.Style = Resources["MyStyle"] as Style;

			var scrollViewer = MyListView.FindFirstChild<ScrollViewer>();

			MyListView.ItemsSource = CreateItems();
		}

		private Rectangle[] CreateItems()
		{
			var items = new List<Rectangle>();
			var rectangle1 = new Rectangle();
			rectangle1.Height = 1000;
			rectangle1.Width = 1000;
			rectangle1.Fill = new SolidColorBrush(Colors.Blue);
			var rectangle2 = new Rectangle();
			rectangle2.Height = 1000;
			rectangle2.Width = 1000;
			rectangle2.Fill = new SolidColorBrush(Colors.Pink);

			items.Add(rectangle1);
			items.Add(rectangle2);

			return items.ToArray();
		}
	}
}

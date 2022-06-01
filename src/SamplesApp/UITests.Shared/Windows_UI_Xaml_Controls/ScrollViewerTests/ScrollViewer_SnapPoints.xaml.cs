using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ScrollViewerTests
{
	[Sample("Scrolling")]
	public sealed partial class ScrollViewer_SnapPoints : Page
	{
		public ScrollViewer_SnapPoints()
		{
			this.InitializeComponent();

			panel.VerticalSnapPointsChanged += SnapPointsChanged;
		}

		private void SnapPointsChanged(object sender, object e)
		{
			if (panel.AreVerticalSnapPointsRegular)
			{
				points.Text = "Regular";
			}
			else
			{
				var near = string.Join(", ", panel.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Near));
				var center = string.Join(", ", panel.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Center));
				var far = string.Join(", ", panel.GetIrregularSnapPoints(Orientation.Vertical, SnapPointsAlignment.Far));
				points.Text = $"near: {near}\ncenter: {center}\nfar: {far}";
			}

		}
	}
}

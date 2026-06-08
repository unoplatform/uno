using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace SamplesApp.Wasm.Windows_UI_Xaml_Media.Transform
{
	[Sample("Transform", Name = "TransformToVisual_Simple", Description = "The ellipse should track the y-position of the text inside the scroll viewer, using TransformToVisual")]
	public sealed partial class TransformToVisual_Simple : UserControl
	{
		public TransformToVisual_Simple()
		{
			this.InitializeComponent();
		}

		private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			var transform = TargetView.TransformToVisual(EnclosingView);
			var targetY = transform.TransformPoint(new Point(0, 0)).Y;
			var tracker = TrackerView;
			Microsoft.UI.Xaml.Controls.Canvas.SetTop(tracker, targetY);
		}
	}
}

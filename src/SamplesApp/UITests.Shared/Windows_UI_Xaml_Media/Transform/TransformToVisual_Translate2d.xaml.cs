using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using System.Threading.Tasks;
using UITests.Shared.Helpers;

namespace SamplesApp.Wasm.Windows_UI_Xaml_Media.Transform
{
	[SampleControlInfo("Transform", "TransformToVisual_Translate2d", description: "The ellipses should track the x- and y-positions of the duck inside the scroll viewer, using TransformToVisual")]
	public sealed partial class TransformToVisual_Translate2d : UserControl, IWaitableSample
	{
		private TaskCompletionSource _tcs = new();
		public TransformToVisual_Translate2d()
		{
			this.InitializeComponent();
			image1.ImageOpened += (_, _) => _tcs.SetResult();
			image1.ImageFailed += (_, _) => _tcs.SetResult();
		}

		public Task SamplePreparedTask => _tcs.Task;

		private void ScrollViewer_ViewChanged(object sender, ScrollViewerViewChangedEventArgs e)
		{
			var transform = TargetView.TransformToVisual(EnclosingView);
			var target = transform.TransformPoint(new Point(0, 0));
			Microsoft.UI.Xaml.Controls.Canvas.SetLeft(TrackerViewX, target.X);
			Microsoft.UI.Xaml.Controls.Canvas.SetTop(TrackerViewY, target.Y);

			OffsetTextBlock.Text = target.ToString();
		}
	}
}

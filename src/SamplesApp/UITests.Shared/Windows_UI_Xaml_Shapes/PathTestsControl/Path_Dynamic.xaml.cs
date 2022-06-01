using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using static System.Math;

namespace SamplesApp.Windows_UI_Xaml_Shapes.PathTestsControl
{
	[SampleControlInfo("Path", "Path_Dynamic")]
	public sealed partial class Path_Dynamic : UserControl
	{
		public Path_Dynamic()
		{
			this.InitializeComponent();
		}

		private void Value_Changed(object sender, RangeBaseValueChangedEventArgs e)
		{
			var percentage = e.NewValue / 100d;
			var angle = (2d * PI * percentage) % (PI * 2d);
			var thickness = 10;
			var sweepDirection = SweepDirection.Clockwise;
			var size = new Size(45, 45);
			var translationFactor = Max(thickness / 2.0, 0.0);

			double x = (Sin(angle) * size.Width) + size.Width + translationFactor;
			double y = (((Cos(angle) * size.Height) - size.Height) * -1) + translationFactor;

			ArcSegment.Size = size;
			ArcSegment.SweepDirection = sweepDirection;
			ArcSegment.IsLargeArc = angle >= PI;
			ArcSegment.Point = new Point(x, y);
		}
	}
}

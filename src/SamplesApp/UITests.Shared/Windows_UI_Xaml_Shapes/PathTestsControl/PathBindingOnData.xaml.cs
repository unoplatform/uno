using System;
using Uno.UI.Samples.Controls;

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SamplesApp.Windows_UI_Xaml_Shapes.PathTestsControl
{
	[SampleControlInfo("Path", "PathBindingOnData", description: "Path with Data property bound to string")]
	public sealed partial class PathBindingOnData : UserControl
	{
		private static readonly Random Random = new Random(1204);
		private const int PathWidth = 100;

		public PathBindingOnData()
		{
			this.InitializeComponent();
			TargetPath.DataContext = null;
		}

		private void ModifyPath(object sender, RoutedEventArgs e)
		{
			var halfWidth = PathWidth / 2;
			var topLeft = new Point(Random.Next(0, halfWidth), Random.Next(0, halfWidth));
			var topRight = new Point(Random.Next(halfWidth, PathWidth), Random.Next(0, halfWidth));
			var bottomRight = new Point(Random.Next(halfWidth, PathWidth), Random.Next(halfWidth, PathWidth));
			var bottomLeft = new Point(Random.Next(0, halfWidth), Random.Next(halfWidth, PathWidth));

			var path = $"M{topLeft.X},{topLeft.Y}";

			void Append(Point p)
			{
				path += $" L{p.X},{p.Y}";
			}

			Append(topLeft);
			Append(topRight);
			Append(bottomRight);
			Append(bottomLeft);

			TargetPath.DataContext = path;
		}
	}
}

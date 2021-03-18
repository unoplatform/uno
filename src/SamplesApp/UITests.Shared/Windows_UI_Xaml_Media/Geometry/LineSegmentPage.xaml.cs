using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace SamplesApp.Windows_UI_Xaml_Media.Geometry
{
	[SampleControlInfo("Geometry", "LineSegment")]
	public sealed partial class LineSegmentPage : Page
	{
		public LineSegmentPage()
		{
			this.InitializeComponent();
		}

		public void MovePointClick(object sender, RoutedEventArgs args)
		{
			LineToChange.Point = new Point(LineToChange.Point.X - 20, LineToChange.Point.Y - 20);
		}
	}
}

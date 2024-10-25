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

namespace SamplesApp.Windows_UI_Xaml_Shapes
{
	[SampleControlInfo("Shapes", "PolylinePage")]
	public sealed partial class PolylinePage : Page
	{
		public PolylinePage()
		{
			this.InitializeComponent();
		}

		public void Change_Shape(object sender, RoutedEventArgs e)
		{
			var points = new PointCollection();
			points.Add(new Point(10, 200));
			points.Add(new Point(60, 140));
			points.Add(new Point(130, 140));
			points.Add(new Point(180, 200));
			DPolyline.Points = points;
		}

		public void Clear_Shape(object sender, RoutedEventArgs e)
		{
			DPolyline.Points.Clear();
		}
	}
}

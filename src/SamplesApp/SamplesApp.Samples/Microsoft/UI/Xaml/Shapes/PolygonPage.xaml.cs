using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace SamplesApp.Windows_UI_Xaml_Shapes
{
	[Sample("Shapes", Name = "PolygonPage")]
	public sealed partial class PolygonPage : Page
	{
		public PolygonPage()
		{
			this.InitializeComponent();
		}

		public void Change_Shape(object sender, RoutedEventArgs e)
		{
			var points = new PointCollection();
			points.Add(new Point(10, 180));
			points.Add(new Point(60, 140));
			points.Add(new Point(130, 140));
			points.Add(new Point(180, 220));
			DPolygon.Points = points;
		}

		public void Clear_Shape(object sender, RoutedEventArgs e)
		{
			DPolygon.Points.Clear();
		}
	}
}

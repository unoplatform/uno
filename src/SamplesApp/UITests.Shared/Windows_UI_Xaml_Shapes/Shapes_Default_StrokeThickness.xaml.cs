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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Shapes
{
	[Sample("Shapes")]
	public sealed partial class Shapes_Default_StrokeThickness : UserControl
	{
		public double MyStrokeThickness { get; set; } = 0d;

		public Shapes_Default_StrokeThickness()
		{
			this.InitializeComponent();
			StrokeThicknessButton.Click += StrokeThicknessButton_Click;
		}

		private void StrokeThicknessButton_Click(object sender, RoutedEventArgs e)
		{
			if (MyLineSelector.IsChecked ?? false)
			{
				MyLine.StrokeThickness = MyStrokeThickness;
			}
			if (MyRectSelector.IsChecked ?? false)
			{
				MyRect.StrokeThickness = MyStrokeThickness;
			}
			if (MyPolylineSelector.IsChecked ?? false)
			{
				MyPolyline.StrokeThickness = MyStrokeThickness;
			}
			if (MyPolygonSelector.IsChecked ?? false)
			{
				MyPolygon.StrokeThickness = MyStrokeThickness;
			}
			if (MyEllipseSelector.IsChecked ?? false)
			{
				MyEllipse.StrokeThickness = MyStrokeThickness;
			}
			if (MyPathSelector.IsChecked ?? false)
			{
				MyPath.StrokeThickness = MyStrokeThickness;
			}
		}
	}
}

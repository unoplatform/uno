using System;
using System.Numerics;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Composition;

[Sample("Windows.UI.Composition")]
public sealed partial class VisualRotationSample : Page
{
	private readonly Visual _borderVisual;

	public VisualRotationSample()
	{
		this.InitializeComponent();
		_borderVisual = ElementCompositionPreview.GetElementVisual(myBorder);
	}

	private void Button1_Click(object sender, RoutedEventArgs args)
	{
		_borderVisual.RotationAngleInDegrees = 45;
		_borderVisual.CenterPoint = new Vector3((float)myBorder.ActualWidth / 2, (float)myBorder.ActualHeight / 2, 0);
	}

	private void Button2_Click(object sender, RoutedEventArgs args)
	{
		_borderVisual.RotationAngleInDegrees = 45;
		_borderVisual.CenterPoint = new Vector3(0, 0, 0);
	}
}

using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Composition;

[Sample("Microsoft.UI.Composition")]
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

	private void Button4_Click(object sender, RoutedEventArgs args)
	{
		_borderVisual.RotationAngleInDegrees = 45;
		_borderVisual.CenterPoint = new Vector3(0, 0, 0);
	}
}

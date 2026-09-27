using System;
using System.Numerics;
using Uno.UI.Samples.Controls;
using Windows.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Shapes;

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition",
		Name = "GetAlphaMask",
		Description = "Demonstrates GetAlphaMask() on Shape, TextBlock, and Image using CompositionMaskBrush.",
		IsManualTest = true,
		IgnoreInSnapshotTests = true)]
	public sealed partial class AlphaMaskTests : UserControl
	{
		public AlphaMaskTests()
		{
			this.InitializeComponent();
			this.Loaded += AlphaMaskTests_Loaded;
		}

		private void AlphaMaskTests_Loaded(object sender, RoutedEventArgs e)
		{
			var compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;

			// Row 1: Ellipse alpha mask with solid red source
			SetupMaskBrush(compositor, ellipse.GetAlphaMask(), compositor.CreateColorBrush(Colors.Red), ellipseResultCanvas, new Vector2(120, 120));

			// Row 2: TextBlock alpha mask with solid red source
			SetupMaskBrush(compositor, textBlock.GetAlphaMask(), compositor.CreateColorBrush(Colors.Red), textResultCanvas, new Vector2(240, 50));

			// Row 3: Image alpha mask — deferred until image source is loaded
			img.ImageOpened += (s, args) =>
			{
				SetupMaskBrush(compositor, img.GetAlphaMask(), compositor.CreateColorBrush(Colors.Red), imageResultCanvas, new Vector2(100, 100));
			};

			// Row 4: Ellipse alpha mask with gradient source
			var gradient = compositor.CreateLinearGradientBrush();
			gradient.ColorStops.Add(compositor.CreateColorGradientStop(0.0f, Colors.Red));
			gradient.ColorStops.Add(compositor.CreateColorGradientStop(1.0f, Colors.Blue));
			gradient.StartPoint = new Vector2(0, 0);
			gradient.EndPoint = new Vector2(1, 1);
			SetupMaskBrush(compositor, ellipseGradient.GetAlphaMask(), gradient, gradientResultCanvas, new Vector2(120, 120));
		}

		private static void SetupMaskBrush(Compositor compositor, CompositionBrush mask, CompositionBrush source, Canvas canvas, Vector2 size)
		{
			var maskBrush = compositor.CreateMaskBrush();
			maskBrush.Source = source;
			maskBrush.Mask = mask;

			var spriteVisual = compositor.CreateSpriteVisual();
			spriteVisual.Brush = maskBrush;
			spriteVisual.Size = size;

			ElementCompositionPreview.SetElementChildVisual(canvas, spriteVisual);
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Windows.UI.Composition",
		Name = "CompositionMaskBrush",
		Description = "Paints a SpriteVisual with a CompositionBrush with an opacity mask applied to it.",
		IsManualTest = true,
		IgnoreInSnapshotTests = true)]
	public sealed partial class MaskBrushTests : UserControl
	{
		public MaskBrushTests()
		{
			this.InitializeComponent();
			this.Loaded += MaskBrushTests_Loaded;
		}

		private void MaskBrushTests_Loaded(object sender, RoutedEventArgs e)
		{
			var compositor = Windows.UI.Xaml.Window.Current.Compositor;

			var visualSurface = compositor.CreateVisualSurface();
			visualSurface.SourceVisual = ElementCompositionPreview.GetElementVisual(img);
			visualSurface.SourceSize = new(100, 100);

			var sourceBrush = compositor.CreateSurfaceBrush(visualSurface);
			var mask = compositor.CreateLinearGradientBrush();
			var maskBrush = compositor.CreateMaskBrush();

			mask.ColorStops.Add(compositor.CreateColorGradientStop(0.0f, Colors.Black));
			mask.ColorStops.Add(compositor.CreateColorGradientStop(1.0f, Colors.Transparent));
			mask.StartPoint = new(0.0f, 0.0f);
			mask.EndPoint = new(0.0f, 100.0f);

			maskBrush.Source = sourceBrush;
			maskBrush.Mask = mask;

			var spriteVisual = compositor.CreateSpriteVisual();
			spriteVisual.Brush = maskBrush;
			spriteVisual.Size = new(100, 100);

			ElementCompositionPreview.SetElementChildVisual(canvas, spriteVisual);

			var visualSurface2 = compositor.CreateVisualSurface();
			visualSurface2.SourceVisual = ElementCompositionPreview.GetElementVisual(player);
			visualSurface2.SourceSize = new(200, 200);

			var sourceBrush2 = compositor.CreateSurfaceBrush(visualSurface2);
			var mask2 = compositor.CreateRadialGradientBrush();
			var maskBrush2 = compositor.CreateMaskBrush();

			mask2.ColorStops.Add(compositor.CreateColorGradientStop(0.0f, Colors.Black));
			mask2.ColorStops.Add(compositor.CreateColorGradientStop(1.0f, Colors.Transparent));
			mask2.GradientOriginOffset = new(0.0f, 0.0f);
			mask2.EllipseCenter = new(100.0f, 100.0f);
			mask2.EllipseRadius = new(100.0f, 100.0f);

			maskBrush2.Source = sourceBrush2;
			maskBrush2.Mask = mask2;

			var spriteVisual2 = compositor.CreateSpriteVisual();
			spriteVisual2.Brush = maskBrush2;
			spriteVisual2.Size = new(200, 200);

			ElementCompositionPreview.SetElementChildVisual(canvas2, spriteVisual2);

			var maskSprite1 = compositor.CreateSpriteVisual();
			var maskSprite2 = compositor.CreateSpriteVisual();

			maskSprite1.Brush = mask;
			maskSprite2.Brush = mask2;

			maskSprite1.Size = new(100, 100);
			maskSprite2.Size = new(200, 200);

			ElementCompositionPreview.SetElementChildVisual(maskCanvas, maskSprite1);
			ElementCompositionPreview.SetElementChildVisual(maskCanvas2, maskSprite2);
		}
	}
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", Name = "NineGrid", Description = "Paints a SpriteVisual with a CompositionBrush after applying Nine-Grid Stretching to the contents of the Source brush. ", IsManualTest = true)]
	public sealed partial class NineGridTests : UserControl
	{
		public NineGridTests()
		{
			this.InitializeComponent();
			this.Loaded += NineGridTests_Loaded;
		}

		private void NineGridTests_Loaded(object sender, RoutedEventArgs e)
		{
			var compositor = Microsoft.UI.Xaml.Window.Current.Compositor;
			var visualSurface = compositor.CreateVisualSurface();
			visualSurface.SourceVisual = ElementCompositionPreview.GetElementVisual(img);
			visualSurface.SourceSize = new(100, 100);

			var brush = compositor.CreateSurfaceBrush(visualSurface);
			var nineGridBrush = compositor.CreateNineGridBrush();
			nineGridBrush.Source = brush;
			nineGridBrush.SetInsets(40);

			var spriteVisual = compositor.CreateSpriteVisual();
			spriteVisual.Brush = nineGridBrush;
			spriteVisual.Size = new(200, 200);

			var brush2 = compositor.CreateColorBrush(Colors.Red);

			var spriteVisual1 = compositor.CreateSpriteVisual();
			spriteVisual1.Brush = brush2;
			spriteVisual1.Size = new(200, 200);

			var nineGridBrush2 = compositor.CreateNineGridBrush();
			nineGridBrush2.Source = brush2;
			nineGridBrush2.IsCenterHollow = true;
			nineGridBrush2.SetInsets(20);

			var spriteVisual2 = compositor.CreateSpriteVisual();
			spriteVisual2.Brush = nineGridBrush2;
			spriteVisual2.Size = new(200, 200);

			ElementCompositionPreview.SetElementChildVisual(canvas, spriteVisual);
			ElementCompositionPreview.SetElementChildVisual(canvas1, spriteVisual1);
			ElementCompositionPreview.SetElementChildVisual(canvas2, spriteVisual2);
		}
	}
}

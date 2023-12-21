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
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Microsoft.UI.Composition", Name = "CompositionVisualSurface", Description = "Represents a visual tree as an ICompositionSurface that can be used to paint a Visual using a CompositionBrush.", IsManualTest = true)]
	public sealed partial class VisualSurfaceTests : UserControl
	{
		public VisualSurfaceTests()
		{
			this.InitializeComponent();
			this.Loaded += VisualSurfaceTests_Loaded;
		}

		private void VisualSurfaceTests_Loaded(object sender, RoutedEventArgs e)
		{
			var compositor = Microsoft.UI.Xaml.Window.Current.Compositor;
			var visualSurface = compositor.CreateVisualSurface();
			visualSurface.SourceVisual = ElementCompositionPreview.GetElementVisual(player0);
			visualSurface.SourceSize = new(200, 100);
			visualSurface.SourceOffset = new(0, 100);

			var brush = compositor.CreateSurfaceBrush(visualSurface);
			var spriteVisual = compositor.CreateSpriteVisual();
			spriteVisual.Brush = brush;
			spriteVisual.Size = new(200, 100);

			ElementCompositionPreview.SetElementChildVisual(canvas, spriteVisual);

			var visualSurface2 = compositor.CreateVisualSurface();
			visualSurface2.SourceVisual = ElementCompositionPreview.GetElementVisual(player);
			visualSurface2.SourceSize = new(200, 200);

			var brush2 = compositor.CreateSurfaceBrush(visualSurface2);
			var spriteVisual2 = compositor.CreateSpriteVisual();
			spriteVisual2.Brush = brush2;
			spriteVisual2.Size = new(200, 200);

			ElementCompositionPreview.SetElementChildVisual(canvas2, spriteVisual2);
		}
	}
}

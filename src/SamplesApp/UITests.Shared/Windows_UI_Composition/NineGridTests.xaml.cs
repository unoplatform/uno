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
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Composition
{
	[Sample("Windows.UI.Composition", Name = "NineGrid", Description = "Paints a SpriteVisual with a CompositionBrush after applying Nine-Grid Stretching to the contents of the Source brush. ", IsManualTest = true)]
	public sealed partial class NineGridTests : UserControl
	{
		public NineGridTests()
		{
			this.InitializeComponent();
			this.Loaded += NineGridTests_Loaded;
		}

		private void NineGridTests_Loaded(object sender, RoutedEventArgs e)
		{
			var compositor = Window.Current.Compositor;
			var visualSurface = compositor.CreateVisualSurface();
			visualSurface.SourceVisual = ElementCompositionPreview.GetElementVisual(img);
			visualSurface.SourceSize = new(100, 100);

			var brush = compositor.CreateSurfaceBrush(visualSurface);
			var nineGridBrush = compositor.CreateNineGridBrush();
			nineGridBrush.Source = brush;
			nineGridBrush.SetInsets(40);

			var spriteVisual = compositor.CreateSpriteVisual();
			spriteVisual.Brush = nineGridBrush;
			spriteVisual.Size = new(100, 100);

			ElementCompositionPreview.SetElementChildVisual(canvas, spriteVisual);
		}
	}
}

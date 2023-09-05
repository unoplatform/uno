using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Media.LoadedImageSurface
{
	[Sample("Windows.UI.Xaml.Media", Name = "LoadedImageSurface", Description = "Represents a composition surface that an image can be downloaded, decoded and loaded onto. You can load an image using a URI that references an image source file, or supplying a IRandomAccessStream.", IsManualTest = true)]
	public sealed partial class LoadedImageSurfaceTests : UserControl
	{
		public LoadedImageSurfaceTests()
		{
			this.InitializeComponent();
		}

		private void UserControl_Loaded(object sender, RoutedEventArgs e)
		{
			testGrid.Background = new TestBrush();
		}
	}

	class TestBrush : Windows.UI.Xaml.Media.XamlCompositionBrushBase
	{
		protected override void OnConnected()
		{
			var surface = Windows.UI.Xaml.Media.LoadedImageSurface.StartLoadFromUri(new Uri("https://avatars.githubusercontent.com/u/52228309?s=200&v=4"));
			surface.LoadCompleted += (s, o) =>
			{
				if (o.Status == Windows.UI.Xaml.Media.LoadedImageSourceLoadStatus.Success)
				{
					var compositor = Windows.UI.Xaml.Window.Current.Compositor;
					var brush = compositor.CreateSurfaceBrush(surface);

					CompositionBrush = brush;
				}
			};
		}
	}
}

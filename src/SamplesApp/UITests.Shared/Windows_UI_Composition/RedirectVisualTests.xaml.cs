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
	[Sample("Microsoft.UI.Composition", Name = "RedirectVisual", Description = "Represents a visual that gets its content from another visual.", IsManualTest = true)]
	public sealed partial class RedirectVisualTests : UserControl
	{
		public RedirectVisualTests()
		{
			this.InitializeComponent();
			this.Loaded += RedirectVisualTests_Loaded;
		}

		private void RedirectVisualTests_Loaded(object sender, RoutedEventArgs e)
		{
			var compositor = Microsoft.UI.Xaml.Window.Current.Compositor;
			var redirectVisual = compositor.CreateRedirectVisual(ElementCompositionPreview.GetElementVisual(img));
			ElementCompositionPreview.SetElementChildVisual(canvas, redirectVisual);

			redirectVisual.Size = new(100, 100);

			var redirectVisual2 = compositor.CreateRedirectVisual(ElementCompositionPreview.GetElementVisual(player));
			ElementCompositionPreview.SetElementChildVisual(canvas2, redirectVisual2);

			redirectVisual2.Size = new(200, 200);
		}
	}
}

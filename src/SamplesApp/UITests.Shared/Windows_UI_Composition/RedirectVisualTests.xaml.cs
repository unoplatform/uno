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
	[Sample("Windows.UI.Composition",
		Name = "RedirectVisual",
		Description = "Represents a visual that gets its content from another visual.",
		IsManualTest = true,
		IgnoreInSnapshotTests = true)]
	public sealed partial class RedirectVisualTests : UserControl
	{
		public RedirectVisualTests()
		{
			this.InitializeComponent();
			this.Loaded += RedirectVisualTests_Loaded;
		}

		private void RedirectVisualTests_Loaded(object sender, RoutedEventArgs e)
		{
			var compositor = Windows.UI.Xaml.Window.Current.Compositor;
			var redirectVisual = compositor.CreateRedirectVisual(ElementCompositionPreview.GetElementVisual(img));
			ElementCompositionPreview.SetElementChildVisual(canvas, redirectVisual);

			redirectVisual.Size = new(100, 100);

			var redirectVisual2 = compositor.CreateRedirectVisual(ElementCompositionPreview.GetElementVisual(player));
			ElementCompositionPreview.SetElementChildVisual(canvas2, redirectVisual2);

			redirectVisual2.Size = new(200, 200);
		}
	}
}

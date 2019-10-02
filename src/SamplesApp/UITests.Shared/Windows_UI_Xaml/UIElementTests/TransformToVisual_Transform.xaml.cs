using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Shared.Windows_UI_Xaml.UIElementTests
{
	[SampleControlInfo("UIElement", "TransformToVisual_Transform")]
    public sealed partial class TransformToVisual_Transform : UserControl
    {
        public TransformToVisual_Transform()
        {
            this.InitializeComponent();

			Loaded += TransformToVisual_Transform_Loaded;
        }

		private async void TransformToVisual_Transform_Loaded(object sender, RoutedEventArgs e)
		{
			await Task.Yield();
			var tr1 = Border1.TransformToVisual(null) as MatrixTransform;
			var tr2 = Border2.TransformToVisual(null) as MatrixTransform;

			var windowBounds = Windows.UI.Xaml.Window.Current.Bounds;
			WindowWidth.Text = windowBounds.Width.ToString();
			WindowHeight.Text = windowBounds.Height.ToString();
			Border1TransformNullX.Text = tr1.Matrix.OffsetX.ToString();
			Border1TransformNullY.Text = tr1.Matrix.OffsetY.ToString();
			Border2TransformNullX.Text = tr2.Matrix.OffsetX.ToString();
			Border2TransformNullY.Text = tr2.Matrix.OffsetY.ToString();
			IsLoadedText.Text = "Loaded";
		}
	}
}

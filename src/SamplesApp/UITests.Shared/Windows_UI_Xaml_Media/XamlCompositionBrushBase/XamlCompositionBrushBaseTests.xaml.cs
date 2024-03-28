using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml_Media.XamlCompositionBrushBase
{
	[Sample("Windows.UI.Xaml.Media", Name = "XamlCompositionBrushBase", Description = "Provides a base class used to create XAML brushes that paint an area with a CompositionBrush.", IsManualTest = true)]
	public sealed partial class XamlCompositionBrushBaseTests : UserControl
	{
		public XamlCompositionBrushBaseTests()
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
			var compositor = Windows.UI.Xaml.Window.Current.Compositor;
			var brush = compositor.CreateLinearGradientBrush();
			brush.ColorStops.Add(compositor.CreateColorGradientStop(0.0f, Colors.Black));
			brush.ColorStops.Add(compositor.CreateColorGradientStop(1.0f, Colors.White));
			brush.StartPoint = new();
			brush.EndPoint = new(200, 200);

			CompositionBrush = brush;
		}
	}
}

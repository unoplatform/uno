using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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
	[SampleControlInfo("UIElement", "TransformToVisual_Simple")]
	public sealed partial class TransformToVisual_Simple : UserControl
	{
		private Grid _grid;
		private Border _border;

		public TransformToVisual_Simple()
		{
			this.InitializeComponent();

			_grid = new Grid() { Width = 100, Height = 100 };
			_border = new Border()
			{
				Width = 10,
				Height = 10,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Background = new SolidColorBrush(Colors.Red)
			};

			_grid.Children.Add(_border);

			root.Content = _grid;

			Loaded += OnControlLoaded;
		}

		private async void OnControlLoaded(object sender, RoutedEventArgs e)
		{
			await Task.Yield();
			var r = _grid.TransformToVisual(_border) as MatrixTransform;

			result.Text = $"{r.Matrix.OffsetX};{r.Matrix.OffsetY}";
		}
	}
}

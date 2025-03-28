using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Uno.Extensions;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Shapes
{
	[Sample("Shapes")]
	public sealed partial class Offscreen_Shapes : Page
	{
		public Offscreen_Shapes()
		{
			this.InitializeComponent();

			Loaded += OnLoaded;
		}

		private async void OnLoaded(object sender, RoutedEventArgs e)
		{
			Loaded -= OnLoaded;

			await Task.Delay(200);

			var sw = xamlShape1.Width;
			var sh = xamlShape1.Height;
			var sf = xamlShape1.Fill;
			var ss = xamlShape1.Stroke;
			var sst = xamlShape1.StrokeThickness;

			await SetShape(
				1,
				new Ellipse
				{
					Width = sw,
					Height = sh,
					Fill = sf,
					Stroke = ss,
					StrokeThickness = sst
				});

			await SetShape(
				2,
				new Line
				{
					Fill = sf,
					Stroke = ss,
					StrokeThickness = sst,
					X2 = xamlShape2.X2,
					Y2 = xamlShape2.Y2
				});

			var p = await SetShape(
				3,
				new Path
				{
					Width = sw,
					Height = sh,
					Fill = sf,
					Stroke = ss,
					StrokeThickness = sst,
				});

#if WINAPPSDK
			var geometry = XamlReader.Load("<Geometry xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>M0,0L60,40 0,40 60,0Z</Geometry>") as Geometry;
#else
			var geometry = (Geometry)"M0,0L60,40 0,40 60,0Z";
#endif
			p.Data = geometry;

			var polygon = await SetShape(
				4,
				new Polygon
				{
					Width = sw,
					Height = sh,
					Fill = sf,
					Stroke = ss,
					StrokeThickness = sst
				});

			polygon.Points.AddRange(xamlShape4.Points);

			var polyline = await SetShape(
				5,
				new Polyline
				{
					Width = sw,
					Height = sh,
					Fill = sf,
					Stroke = ss,
					StrokeThickness = sst,
				});

			polyline.Points.AddRange(xamlShape5.Points);

			await SetShape(
				6,
				new Rectangle
				{
					Width = sw,
					Height = sh,
					Fill = sf,
					Stroke = ss,
					StrokeThickness = sst
				});

			async Task<T> SetShape<T>(int row, T element)
				where T : FrameworkElement
			{
				await Task.Yield();
				element.Measure(new Size(100, 100));
				element.Arrange(new Rect(0, 0, 200, 200));
				await Task.Yield();
				Grid.SetColumn(element, 2);
				Grid.SetRow(element, row);
				element.Name = $"deferredShape{row}";

				grid.Children.Add(element);

				return element;
			}
		}
	}
}

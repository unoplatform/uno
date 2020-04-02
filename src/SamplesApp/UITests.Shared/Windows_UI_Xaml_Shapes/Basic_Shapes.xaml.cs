using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Shapes
{
	[Sample("Shapes")]
	public sealed partial class Basic_Shapes : Page
	{
		public Basic_Shapes()
		{
			this.InitializeComponent();

			Update();
		}

		private void SettingsUpdated(object sender, object e) => Update();

		private void Update()
		{
			if (_root == null)
			{
				return;
			}
			var shapes = GetShapes().ToList();
			var sizes = GetSizes().ToList();
			var stretches = GetStretches().ToList();

			var shapesPanel = new StackPanel { Orientation = Orientation.Vertical };
			foreach (var shape in shapes)
			{
				var sizePanel = new StackPanel { Orientation = Orientation.Vertical };
				foreach (var size in sizes)
				{
					var stretchPanel = new StackPanel { Orientation = Orientation.Horizontal };
					foreach (var stretch in stretches)
					{
						var items = BuildHoriVertStretchGrid(
							() =>
							{
								var sut = shape();
								size.Alter(sut);
								stretch.Alter(sut);
								return sut;
							},
							$"[{shape().GetType().Name.ToUpperInvariant()}]\r\nSize: {size.Name}\r\nShape stretch: {stretch.Name}");
						items.BorderBrush = new SolidColorBrush(Colors.HotPink);
						items.BorderThickness = new Thickness(5);
						stretchPanel.Children.Add(items);
					}
					sizePanel.Children.Add(stretchPanel);
				}
				shapesPanel.Children.Add(sizePanel);
			}

			_root.Content = shapesPanel;


			IEnumerable<Generator> GetShapes()
			{
				if (_shapeRectangle.IsOn) yield return () => new Rectangle
				{
					Fill = new SolidColorBrush(Color.FromArgb(160, 255, 0,0)),
					Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)),
					StrokeThickness = 6
				};

				if (_shapeEllipse.IsOn) yield return () => new Ellipse
				{
					Fill = new SolidColorBrush(Color.FromArgb(160, 255, 128, 0)),
					Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 128, 0)),
					StrokeThickness = 6
				};

				if (_shapeLine.IsOn) yield return () => new Line
				{
					Fill = new SolidColorBrush(Color.FromArgb(160, 255, 255, 0)),
					Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0)),
					StrokeThickness = 6,
					X1 = 50,
					Y1 = 25,
					X2 = 125,
					Y2 = 100
				};

				if (_shapePath.IsOn) yield return () => new Windows.UI.Xaml.Shapes.Path
				{
					Fill = new SolidColorBrush(Color.FromArgb(160, 0, 128, 0)),
					Stroke = new SolidColorBrush(Color.FromArgb(255, 0, 128, 0)),
					StrokeThickness = 6,
					Data = new PathGeometry
					{
						Figures =
						{
							new PathFigure
							{
								IsClosed = true,
								StartPoint = new Point(25, 25),
								Segments =
								{
									new LineSegment{Point = new Point(25,125)},
									new LineSegment{Point = new Point(75,125)},
									new BezierSegment{Point1 = new Point(125,125), Point2 = new Point(125,25), Point3 = new Point(75, 25)},
									new LineSegment{Point=new Point(25,25)}
								}
							}
						}
					}
				};

				if (_shapePolygon.IsOn) yield return () => new Polygon
				{
					Fill = new SolidColorBrush(Color.FromArgb(160, 0, 0, 255)),
					Stroke = new SolidColorBrush(Color.FromArgb(160, 0, 0, 255)),
					StrokeThickness = 6,
					Points = { new Point(25, 25), new Point(25, 125), new Point(125, 125) }
				};

				if (_shapePolyline.IsOn) yield return () => new Polyline
				{
					Fill = new SolidColorBrush(Color.FromArgb(160, 160, 0, 192)),
					Stroke = new SolidColorBrush(Color.FromArgb(255, 160, 0, 192)),
					StrokeThickness = 6,
					Points = { new Point(25, 25), new Point(25, 125), new Point(125, 125) }
				};
			}

			IEnumerable<IAlterator> GetSizes()
			{
				if (_sizeUnconstrained.IsOn) yield return new NullAlterator("Unconstrained");
				if (_sizeFixedSmall.IsOn) yield return new UpSizeAlterator();
				if (_sizeFixedLarge.IsOn) yield return new DownSizeAlterator();
			}

			IEnumerable<IAlterator> GetStretches()
			{
				if (_stretchDefault.IsOn) yield return new NullAlterator();
				if (_stretchNone.IsOn) yield return new NoneAlterator();
				if (_stretchFill.IsOn) yield return new FillAlterator();
				if (_stretchUniform.IsOn) yield return new UniformAlterator();
				if (_stretchUniformToFill.IsOn) yield return new UniformToFillAlterator();
			}
		}

		private Panel BuildHoriVertStretchGrid(Func<FrameworkElement> template, string title, int itemSize = 150)
		{
			var horizontalAlignments = new[] { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right, HorizontalAlignment.Stretch };
			var verticalAlignments = new[] { VerticalAlignment.Top, VerticalAlignment.Center, VerticalAlignment.Bottom, VerticalAlignment.Stretch };
			var grid = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition {Height = GridLength.Auto},
					new RowDefinition {Height = GridLength.Auto},
				},
				ColumnDefinitions =
				{
					new ColumnDefinition {Width = new GridLength(75)},
				},
				Children = { GetLabel(title).GridColumnSpan(horizontalAlignments.Length + 1) }
			};

			for (var x = 0; x < horizontalAlignments.Length; x++)
			{
				grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(itemSize + 2) });
				grid.Children.Add(GetLabel(horizontalAlignments[x]).GridRow(1).GridColumn(x + 1));
			}
			for (var y = 0; y < verticalAlignments.Length; y++)
			{
				grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(itemSize + 20 + 2) });
				grid.Children.Add(GetLabel(verticalAlignments[y]).GridRow(y + 2).GridColumn(0));
			}

			for (var x = 0; x < horizontalAlignments.Length; x++)
			for (var y = 0; y < verticalAlignments.Length; y++)
			{
				var item = template();
				var container = new Border
				{
					Width = itemSize + 2,
					Height = itemSize + 20 + 2,
					BorderBrush = new SolidColorBrush(Colors.HotPink),
					BorderThickness = new Thickness(1, 1, 1, 1),
					Child = item
				};

				item.HorizontalAlignment = horizontalAlignments[x];
				item.VerticalAlignment = verticalAlignments[y];

				grid.Children.Add(container.GridColumn(x + 1).GridRow(y + 2));

				var sizeOverlay = new TextBlock
				{
					VerticalAlignment = VerticalAlignment.Top,
					HorizontalAlignment = HorizontalAlignment.Left,
					Foreground = new SolidColorBrush(Colors.Gray),
				};
				item.Loaded += (snd, e) => sizeOverlay.Text = $"d: {item.DesiredSize.Width}x{item.DesiredSize.Height}\r\na: {item.ActualWidth}x{item.ActualHeight}";
				item.SizeChanged += (snd, e) => sizeOverlay.Text = $"d: {item.DesiredSize.Width}x{item.DesiredSize.Height}\r\na: {item.ActualWidth}x{item.ActualHeight}";
				grid.Children.Add(sizeOverlay.GridColumn(x + 1).GridRow(y + 2));
			}

			return grid;

			TextBlock GetLabel(object text)
				=> new TextBlock
				{
					Text = text?.ToString() ?? "N/A",
					VerticalAlignment = VerticalAlignment.Center,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					TextAlignment = TextAlignment.Center
				};
		}

		private delegate Shape Generator();
		private interface IAlterator
		{
			string Name { get; }
			void Alter(Shape shape);
		}

		private class NullAlterator : IAlterator
		{
			public NullAlterator(string name = "Default") => Name = name;

			public string Name { get; }

			public void Alter(Shape shape) { }
		}

		private class UpSizeAlterator : IAlterator
		{
			public string Name => "Fixed UP (shape smaller than container)";

			public void Alter(Shape shape)
			{
				shape.Width = 100;
				shape.Height = 75;
			}
		}
		private class DownSizeAlterator : IAlterator
		{
			public string Name => "Fixed DOWN (shape larger than container)";

			public void Alter(Shape shape)
			{
				shape.Width = 300;
				shape.Height = 225;
			}
		}

		private class NoneAlterator : IAlterator
		{
			public string Name => "None";
			public void Alter(Shape shape) => shape.Stretch = Stretch.None;
		}
		private class FillAlterator : IAlterator
		{
			public string Name => "Fill";
			public void Alter(Shape shape) => shape.Stretch = Stretch.Fill;
		}
		private class UniformAlterator : IAlterator
		{
			public string Name => "Uniform";
			public void Alter(Shape shape) => shape.Stretch = Stretch.Uniform;
		}
		private class UniformToFillAlterator : IAlterator
		{
			public string Name => "UniformToFill";
			public void Alter(Shape shape) => shape.Stretch = Stretch.UniformToFill;
		}
	}
}

#pragma warning disable
#if !DEBUG
#error remove disable
#endif





using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;
using Uno.Extensions;
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
				const int smallWidth = 100, smallHeight = 75, largeWidth = 300, largeHeight = 225;

				if (_sizeUnconstrained.IsOn) yield return new NullAlterator("Unconstrained");
				if (_sizeFixedSmall.IsOn) yield return new SizeSmall();
				if (_sizeFixedLarge.IsOn) yield return new SizeLarge();
				if (_sizeFixedLarge.IsOn) yield return new GenericAlterator(
					"Min width only - Large", s =>
					{
						s.MinWidth = largeWidth;
					});
			}

			IEnumerable<IAlterator> GetSizesDef()
			{
				const int smallWidth = 100, smallHeight = 75, largeWidth = 300, largeHeight = 225;

				yield return new NullAlterator("Unconstrained");
				yield return new SizeSmall();
				yield return new SizeLarge();
				yield return new GenericAlterator("Min width only - Small", s => s.MinWidth = smallWidth);
				yield return new GenericAlterator("Min height only - Small", s => s.MinHeight = smallHeight);
				yield return new GenericAlterator("Min width only - Large", s => s.MinWidth = largeWidth);
				yield return new GenericAlterator("Min height only - Large", s => s.MinHeight = largeHeight);
			}

			IEnumerable<IAlterator> GetStretches()
			{
				if (_stretchDefault.IsOn) yield return new NullAlterator();
				if (_stretchNone.IsOn) yield return new StretchNone();
				if (_stretchFill.IsOn) yield return new StretchFill();
				if (_stretchUniform.IsOn) yield return new StretchUniform();
				if (_stretchUniformToFill.IsOn) yield return new StretchUniformToFill();
			}
		}

		private Grid BuildHoriVertStretchGrid(Func<FrameworkElement> template, string title, int itemSize = 150)
		{
			var isLabelEnabled = title != null;
			var horizontalAlignments = new[] {HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right, HorizontalAlignment.Stretch};
			var verticalAlignments = new[] {VerticalAlignment.Top, VerticalAlignment.Center, VerticalAlignment.Bottom, VerticalAlignment.Stretch};
			var grid = new Grid();
			var itemDimensions = new Size(itemSize + 2, itemSize + 20 + 2);
			(int x, int y) labels;
			if (isLabelEnabled)
			{
				grid.RowDefinitions.Add(new RowDefinition {Height = GridLength.Auto});
				grid.RowDefinitions.Add(new RowDefinition {Height = GridLength.Auto});
				grid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(75)});

				labels = (1, 2);

				grid.Children.Add(GetLabel(title).GridColumnSpan(horizontalAlignments.Length + 1));
				grid.Children.AddRange(horizontalAlignments.Select((h, x) => GetLabel(h).GridRow(1).GridColumn(x + labels.x)));
				grid.Children.AddRange(verticalAlignments.Select((v, y) => GetLabel(v).GridRow(y + labels.y).GridColumn(0)));
			}
			else
			{
				labels = (0, 0);
			}

			grid.ColumnDefinitions.AddRange(horizontalAlignments.Select(_ => new ColumnDefinition {Width = new GridLength(itemDimensions.Width)}));
			grid.RowDefinitions.AddRange(verticalAlignments.Select(_ => new RowDefinition {Height = new GridLength(itemDimensions.Height)}));

			for (var x = 0; x < horizontalAlignments.Length; x++)
			for (var y = 0; y < verticalAlignments.Length; y++)
			{
				var item = template();
				var container = new Border
				{
					Width = itemDimensions.Width,
					Height = itemDimensions.Height,
					BorderBrush = new SolidColorBrush(Colors.HotPink),
					BorderThickness = new Thickness(1, 1, 1, 1),
					Child = item
				};

				item.HorizontalAlignment = horizontalAlignments[x];
				item.VerticalAlignment = verticalAlignments[y];

				grid.Children.Add(container.GridColumn(x + labels.x).GridRow(y + labels.y));

				if (isLabelEnabled)
				{ 
					var sizeOverlay = new TextBlock
					{
						VerticalAlignment = VerticalAlignment.Top,
						HorizontalAlignment = HorizontalAlignment.Left,
						Foreground = new SolidColorBrush(Colors.Gray),
					};
					item.Loaded += (snd, e) => sizeOverlay.Text = $"d: {item.DesiredSize.Width}x{item.DesiredSize.Height}\r\na: {item.ActualWidth}x{item.ActualHeight}";
					item.SizeChanged += (snd, e) => sizeOverlay.Text = $"d: {item.DesiredSize.Width}x{item.DesiredSize.Height}\r\na: {item.ActualWidth}x{item.ActualHeight}";
					grid.Children.Add(sizeOverlay.GridColumn(x + labels.x).GridRow(y + labels.y));
				}
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

		private class SizeSmall : IAlterator
		{
			public string Name => "Fixed UP (shape smaller than container)";
			public void Alter(Shape shape)
			{
				shape.Width = 100;
				shape.Height = 75;
			}
		}

		private class SizeLarge : IAlterator
		{
			public string Name => "Fixed DOWN (shape larger than container)";
			public void Alter(Shape shape)
			{
				shape.Width = 300;
				shape.Height = 225;
			}
		}

		private class GenericAlterator : IAlterator
		{
			private readonly Action<Shape> _alter;

			public GenericAlterator(string name, Action<Shape> alter)
			{
				_alter = alter;
				Name = name;
			}

			public string Name { get; }
			public void Alter(Shape shape) => _alter(shape);
		}

		private class StretchNone : IAlterator
		{
			public string Name => "None";
			public void Alter(Shape shape) => shape.Stretch = Stretch.None;
		}
		private class StretchFill : IAlterator
		{
			public string Name => "Fill";
			public void Alter(Shape shape) => shape.Stretch = Stretch.Fill;
		}
		private class StretchUniform : IAlterator
		{
			public string Name => "Uniform";
			public void Alter(Shape shape) => shape.Stretch = Stretch.Uniform;
		}
		private class StretchUniformToFill : IAlterator
		{
			public string Name => "UniformToFill";
			public void Alter(Shape shape) => shape.Stretch = Stretch.UniformToFill;
		}

		private async void GenerateScreenshots(object sender, RoutedEventArgs e)
		{
#if WINDOWS_UWP
			var folder = await new FolderPicker{FileTypeFilter = { "*" }}.PickSingleFolderAsync();

			Func<Shape> shape = () => new Rectangle
			{
				Fill = new SolidColorBrush(Color.FromArgb(160, 255, 0, 0)),
				Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)),
				StrokeThickness = 6
			};
			var alterators = new IAlterator[]
			{
				new StretchUniform(),
				new SizeLarge()
			};

			var fileName = string.Join("_", alterators.Select(a => Regex.Replace(a.Name, @"[^\w]|[ ]", ""))) + ".png";
			var grid = BuildHoriVertStretchGrid(
				() => alterators.Aggregate(shape(), (s, a) =>
				{
					a.Alter(s);
					return s;
				}),
				null,
				150);

			_root.Content = grid;
			await Task.Yield();

			var renderer = new RenderTargetBitmap();
			await renderer.RenderAsync(grid);

			var file = await folder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);
			using (var output = await file.OpenAsync(FileAccessMode.ReadWrite))
			{
				var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, output);
				encoder.SetSoftwareBitmap(SoftwareBitmap.CreateCopyFromBuffer(await renderer.GetPixelsAsync(), BitmapPixelFormat.Bgra8, renderer.PixelWidth, renderer.PixelHeight));
				await encoder.FlushAsync();
				await output.FlushAsync();
			}
#endif
		}
	}

#if !DEBUG
#error multi target this file instead of copy it
#endif
#if WINDOWS_UWP
	internal static class GridExtensions
	{
		/// <summary>
		/// Sets the row for the specified control, when included in a Grid control. 
		/// </summary>
		/// <param name="row">The row to be set for the control</param>
		/// <returns>The view to be used in a fluent expression.</returns>
		public static T GridRow<T>(this T view, int row)
			where T : FrameworkElement
		{
			Grid.SetRow(view, row);

			return view;
		}

		/// <summary>
		/// Sets the row for the specified control, when included in a Grid control. 
		/// </summary>
		/// <param name="rowSpan">The row to be set for the control</param>
		/// <returns>The view to be used in a fluent expression.</returns>
		public static T GridRowSpan<T>(this T view, int rowSpan)
			where T : FrameworkElement
		{
			Grid.SetRowSpan(view, rowSpan);

			return view;
		}

		/// <summary>
		/// Sets the column for the specified control, when included in a Grid control. 
		/// </summary>
		/// <param name="column">The column to be set for the control</param>
		/// <returns>The view to be used in a fluent expression.</returns>
		public static T GridColumn<T>(this T view, int column)
			where T : FrameworkElement
		{
			Grid.SetColumn(view, column);

			return view;
		}


		/// <summary>
		/// Sets the column span for the specified control, when included in a Grid control. 
		/// </summary>
		/// <param name="columnSpan">The column to be set for the control</param>
		/// <returns>The view to be used in a fluent expression.</returns>
		public static T GridColumnSpan<T>(this T view, int columnSpan)
			where T : FrameworkElement
		{
			Grid.SetColumnSpan(view, columnSpan);

			return view;
		}

		/// <summary>
		/// Sets the column and row for the specified control, when included in a Grid control. 
		/// </summary>
		/// <param name="column">The column to be set for the control</param>
		/// <returns>The view to be used in a fluent expression.</returns>
		public static T GridPosition<T>(this T view, int row, int column)
			where T : FrameworkElement
		{
			Grid.SetColumn(view, column);
			Grid.SetRow(view, row);

			return view;
		}
	}
#endif
}

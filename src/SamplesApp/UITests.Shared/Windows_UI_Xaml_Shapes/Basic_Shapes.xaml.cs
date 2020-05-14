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
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
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
		#region Shapes
		private readonly Factory[] _shapes = new []
		{
			Factory.New(() => new Rectangle
			{
				Fill = new SolidColorBrush(Color.FromArgb(160, 255, 0, 0)),
				Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)),
				StrokeThickness = 6
			}),

			Factory.New(() => new Ellipse
			{
				Fill = new SolidColorBrush(Color.FromArgb(160, 255, 128, 0)),
				Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 128, 0)),
				StrokeThickness = 6
			}),

			Factory.New(() => new Line
			{
				Fill = new SolidColorBrush(Color.FromArgb(160, 255, 255, 0)),
				Stroke = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0)),
				StrokeThickness = 6,
				X1 = 50,
				Y1 = 25,
				X2 = 125,
				Y2 = 100
			}),

			Factory.New(() => new Windows.UI.Xaml.Shapes.Path
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
			}),

			Factory.New(() => new Polygon
			{
				Fill = new SolidColorBrush(Color.FromArgb(160, 0, 0, 255)),
				Stroke = new SolidColorBrush(Color.FromArgb(160, 0, 0, 255)),
				StrokeThickness = 6,
				Points = { new Point(25, 25), new Point(25, 125), new Point(125, 125) }
			}),

			Factory.New(() => new Polyline
			{
				Fill = new SolidColorBrush(Color.FromArgb(160, 160, 0, 192)),
				Stroke = new SolidColorBrush(Color.FromArgb(255, 160, 0, 192)),
				StrokeThickness = 6,
				Points = { new Point(25, 25), new Point(25, 125), new Point(125, 125) }
			})
		};
		#endregion

		#region Stretches
		private readonly Alterator[] _stretches = new[]
		{
			new Alterator("Default"),
			new Alterator("None", shape => shape.Stretch = Stretch.None),
			new Alterator("Fill", shape => shape.Stretch = Stretch.Fill),
			new Alterator("Uniform", shape => shape.Stretch = Stretch.Uniform),
			new Alterator("UniformToFill", shape => shape.Stretch = Stretch.UniformToFill),
		};
		#endregion

		#region Sizes
		const int smallWidth = 100, smallHeight = 75, largeWidth = 300, largeHeight = 225;

		private readonly Alterator[] _sizes = new[]
		{
			new Alterator("Unconstrained"),

			new Alterator("Fixed small", "scaled up as shape smaller than container", shape =>
			{
				shape.Width = smallWidth;
				shape.Height = smallHeight;
			}),
			new Alterator("Fixed large", "scaled down as shape bigger than container", shape =>
			{
				shape.Width = largeWidth;
				shape.Height = largeHeight;
			}),
			new Alterator("Fixed width small", "scaled up as shape less large than container", shape => shape.Width = smallWidth, false),
			new Alterator("Fixed width large", "scaled down as shape larger than container", shape => shape.Width = largeWidth, false),
			new Alterator("Fixed height small", "scaled up as shape less tall than container", shape => shape.Height = smallHeight, false),
			new Alterator("Fixed height large", "scaled down as shape taller than container", shape => shape.Height = largeHeight, false),

			new Alterator("Min small", "scaled up as shape smaller than container", shape =>
			{
				shape.MinWidth = smallWidth;
				shape.MinHeight = smallHeight;
			}, false),
			new Alterator("Min large", "scaled down as shape bigger than container", shape =>
			{
				shape.MinWidth = largeWidth;
				shape.MinHeight = largeHeight;
			}, false),
			new Alterator("Min width small", "scaled up as shape less large than container", shape => shape.MinWidth = smallWidth, false),
			new Alterator("Min width large", "scaled down as shape larger than container", shape => shape.MinWidth = largeWidth, false),
			new Alterator("Min height small", "scaled up as shape less tall than container", shape => shape.MinHeight = smallHeight, false),
			new Alterator("Min height large", "scaled down as shape taller than container", shape => shape.MinHeight = largeHeight, false),

			new Alterator("Max small", shape =>
			{
				shape.MaxWidth = smallWidth;
				shape.MaxHeight = smallHeight;
			}, false),
			new Alterator("Max large", shape =>
			{
				shape.MaxWidth = largeWidth;
				shape.MaxHeight = largeHeight;
			}, false),
			new Alterator("Max width small", "scaled up as shape less large than container", shape => shape.MaxWidth = smallWidth, false),
			new Alterator("Max width large", "scaled down as shape larger than container", shape => shape.MaxWidth = largeWidth, false),
			new Alterator("Max height small", "scaled up as shape less tall than container", shape => shape.MaxHeight = smallHeight, false),
			new Alterator("Max height large", "scaled down as shape taller than container", shape => shape.MaxHeight = largeHeight, false),
		};
		#endregion

		public Basic_Shapes()
		{
			this.InitializeComponent();

			_shapesConfig.Children.AddRange(_shapes.Select(s => s.Option));
			_sizesConfig.Children.AddRange(_sizes.Select(s => s.Option));
			_stretchesConfig.Children.AddRange(_stretches.Select(s => s.Option));

			Update();
		}

		private void SettingsUpdated(object sender, object e) => Update();

		private void Update()
		{
			if (_root == null)
			{
				return;
			}

			var shapes = _shapes.Where(s => s.Option.IsOn).ToList();
			var sizes = _sizes.Where(s => s.Option.IsOn).ToList();
			var stretches = _stretches.Where(s => s.Option.IsOn).ToList();

			var shapesPanel = new StackPanel { Orientation = Orientation.Vertical };
			foreach (var shape in shapes)
			{
				var sizePanel = new StackPanel { Orientation = Orientation.Vertical };
				foreach (var size in sizes)
				{
					var stretchPanel = new StackPanel { Orientation = Orientation.Horizontal };
					foreach (var stretch in stretches)
					{
						var items = BuildHoriVertTestGrid(
							() =>
							{
								var sut = shape.Create();
								size.Alter(sut);
								stretch.Alter(sut);
								return sut;
							},
							$"[{shape.Name.ToUpperInvariant()}]\r\nSize: {size.Name}\r\nShape stretch: {stretch.Name}");
						items.BorderBrush = new SolidColorBrush(Colors.HotPink);
						items.BorderThickness = new Thickness(5);
						stretchPanel.Children.Add(items);
					}
					sizePanel.Children.Add(stretchPanel);
				}
				shapesPanel.Children.Add(sizePanel);
			}

			_root.Visibility = Visibility.Visible;
			_testZone.Child = null;
			_root.Content = shapesPanel;
		}

		private async void GenerateScreenshots(object sender, RoutedEventArgs e)
		{

#if WINDOWS_UWP
			var folder = await new FolderPicker { FileTypeFilter = { "*" } }.PickSingleFolderAsync();
			var alteratorsMap = _stretches.SelectMany(stretch => _sizes.Select(size => new[] { stretch, size })).ToArray();

			foreach (var shape in _shapes)
			foreach (var alterators in alteratorsMap)
			{
				var fileName = shape.Name + "_" + string.Join("_", alterators.Select(a => a.Id)) + ".png";
				var grid = RenderHoriVertGridForScreenshot(shape, alterators);
				await Task.Yield();

				var renderer = new RenderTargetBitmap();
				await renderer.RenderAsync(grid);

				var file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
				using (var output = await file.OpenAsync(FileAccessMode.ReadWrite))
				{
					var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, output);
					encoder.SetSoftwareBitmap(SoftwareBitmap.CreateCopyFromBuffer(await renderer.GetPixelsAsync(), BitmapPixelFormat.Bgra8, renderer.PixelWidth, renderer.PixelHeight));
					await encoder.FlushAsync();
					await output.FlushAsync();
				}
			}

			_root.Visibility = Visibility.Visible;
			_testZone.Child = null;
#endif
		}

		private void OnIdInputChanged(object sender, KeyRoutedEventArgs e)
		{
			if (((TextBox)sender).Text.EndsWith("."))
			{
				RenderById(null, null);
			}
		}

		private void RenderById(object sender, RoutedEventArgs e)
		{
			var parsedId = Regex.Match(_idInput.Text, @"(?<shape>[a-zA-Z]+)(_(?<alteratorId>[a-zA-Z]+))+");

			if (!parsedId.Success)
			{
				_testZone.Child = new TextBlock
				{
					Text = $"Failed to parse {parsedId}",
					Foreground = new SolidColorBrush(Colors.Red)
				};
				return;
			}

			try
			{
				var shapeName = parsedId.Groups["shape"].Value;
				var shape = _shapes.Single(s => s.Name == shapeName);

				var alteratorIds = parsedId.Groups["alteratorId"].Captures.Cast<Capture>().Select(c => c.Value);
				var alterators = alteratorIds.Select(id => _stretches.Concat(_sizes).Single(a => a.Id == id)).ToArray();

				RenderHoriVertGridForScreenshot(shape, alterators);

				_idInput.Text = ""; // Clear for next automated test!
			}
			catch (Exception error)
			{
				_testZone.Child = new TextBlock
				{
					Text = $"Failed to render {parsedId}: {error.Message}",
					Foreground = new SolidColorBrush(Colors.Red)
				};
			}
		}

		private Grid RenderHoriVertGridForScreenshot(Factory shape, Alterator[] alterators)
		{
			_root.Visibility = Visibility.Collapsed;

			var grid = BuildHoriVertTestGrid(
				() => alterators.Aggregate(shape.Create(), (s, a) =>
				{
					a.Alter(s);
					return s;
				}),
				null,
				150);

			grid.Background = new SolidColorBrush(Colors.White); // Much easier for screenshot comparison :)
			grid.VerticalAlignment = VerticalAlignment.Top;
			grid.HorizontalAlignment = HorizontalAlignment.Left;

			// We ste clip and add wrapping container so the rendering engine won't generate screenshots with a transparent padding.
			grid.Clip = new RectangleGeometry { Rect = new Rect(0, 0, grid.Width, grid.Height) };
			var containerGrid = new Grid
			{
				Width = grid.Width,
				Height = grid.Height,
				Children = { grid }
			};

			_testZone.Child = containerGrid;

			return containerGrid;
		}

		private Grid BuildHoriVertTestGrid(Func<FrameworkElement> template, string title, int itemSize = 150)
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

				// We hard-code width and height for screenshots:
				// as content might overflow a bit, the rendering engine will append transparent padding in screenshots.
				grid.Width = horizontalAlignments.Length * itemDimensions.Width;
				grid.Height = verticalAlignments.Length * itemDimensions.Height;
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
					TextAlignment = Windows.UI.Xaml.TextAlignment.Center
				};
		}

		private class Factory
		{
			private readonly Func<Shape> _factory;

			public static Factory New<T>(Func<T> factory)
				where T : Shape
				=> new Factory(typeof(T).Name, factory);

			private Factory(string name, Func<Shape> factory)
			{
				_factory = factory;
				Name = name;

				Option = new ToggleSwitch { OnContent = Name, OffContent = Name, IsOn = true };
			}

			public string Name { get; }

			public ToggleSwitch Option { get; }

			public Shape Create() => _factory();
		}

		private class Alterator
		{
			private readonly Action<Shape> _alter;

			public Alterator(string name)
				: this(name, null, _ => { })
			{
			}

			public Alterator(string name, Action<Shape> alter, bool isEnabled = true)
				: this(name, null, alter, isEnabled)
			{
			}

			public Alterator(string name, string details, Action<Shape> alter, bool isEnabled = true)
			{
				_alter = alter;
				Name = name;
				Id = Regex.Replace(name, @"([^\w]|[ ])(?<first>[a-z])", m => m.Groups["first"].Value.ToUpperInvariant());

				var desc = details?.HasValue() ?? false ? $"{name} ({details})" : name;
				Option = new ToggleSwitch { OnContent = desc, OffContent = desc, IsOn = isEnabled };
			}

			public string Id { get; set; }
			public string Name { get; }
			public ToggleSwitch Option { get; set; }
			public void Alter(Shape shape) => _alter(shape);
		}
	}

#if WINDOWS_UWP
	// This is a clone of src\Uno.UI\UI\Xaml\Controls\Grid\GridExtensions.cs,
	// but we prefer to not multi target this to not conflict with other efforts for fluent declaration
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

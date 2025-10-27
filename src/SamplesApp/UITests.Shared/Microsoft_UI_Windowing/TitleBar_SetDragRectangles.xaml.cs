using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;
using Windows.Foundation;
using Windows.Graphics;

namespace UITests.Microsoft_UI_Windowing;

[Sample("Windowing", Name = "TitleBar.SetDragRectangles", IsManualTest = true, Description = "Currently only applies to Win32. Generate rectangles in a grid and apply their bounds as AppWindowTitleBar drag rectangles. Dragging on the red areas should drag the window. Dragging outside these areas should not.")]
public sealed partial class TitleBar_SetDragRectangles : Page
{
	private readonly Random _rand = new Random();

	public TitleBar_SetDragRectangles()
	{
		InitializeComponent();
	}

	private void OnGenerateClick(object sender, RoutedEventArgs e)
	{
		GenerateRandomGridContent();
	}

	private void OnApplyClick(object sender, RoutedEventArgs e)
	{
		try
		{
#if HAS_UNO_WINUI // AppWindow APIs only in WinUI flavor
			var rects = GetRectanglesBoundsInWindowCoordinates();
			SamplesApp.App.MainWindow.AppWindow.TitleBar.SetDragRectangles(rects);
			StatusText.Text = $"Applied {rects.Length} drag rectangles.";
#else
			StatusText.Text = "AppWindow APIs not available on this head.";
#endif
		}
		catch (Exception ex)
		{
			StatusText.Text = $"Failed: {ex.Message}";
		}
	}

	private void OnResetClick(object sender, RoutedEventArgs e)
	{
		try
		{
#if HAS_UNO_WINUI
			SamplesApp.App.MainWindow.AppWindow.TitleBar.SetDragRectangles(null);
			StatusText.Text = "Reset to default drag regions.";
#else
			StatusText.Text = "AppWindow APIs not available on this head.";
#endif
		}
		catch (Exception ex)
		{
			StatusText.Text = $"Failed: {ex.Message}";
		}
	}

	private void GenerateRandomGridContent()
	{
		DynamicGrid.Children.Clear();
		DynamicGrid.RowDefinitions.Clear();
		DynamicGrid.ColumnDefinitions.Clear();

		int rows = _rand.Next(1, 16); //1..16
		int cols = _rand.Next(1, 16); //1..16

		for (int r = 0; r < rows; r++)
		{
			DynamicGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(_rand.NextDouble() + 0.5, GridUnitType.Star) });
		}
		for (int c = 0; c < cols; c++)
		{
			DynamicGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(_rand.NextDouble() + 0.5, GridUnitType.Star) });
		}

		// Random number of rectangles
		int rectCount = _rand.Next(1, rows * cols + 1);
		var usedCells = new HashSet<(int r, int c)>();
		for (int i = 0; i < rectCount; i++)
		{
			int r, c;
			int guard = 0;
			do
			{
				r = _rand.Next(0, rows);
				c = _rand.Next(0, cols);
				guard++;
			}
			while (usedCells.Contains((r, c)) && guard < 50);
			usedCells.Add((r, c));

			var rect = new Rectangle
			{
				Fill = new SolidColorBrush(Microsoft.UI.Colors.Red),
				Margin = new Thickness(_rand.Next(0, 8)),
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch,
				Tag = "DragRect"
			};

			Grid.SetRow(rect, r);
			Grid.SetColumn(rect, c);
			DynamicGrid.Children.Add(rect);
		}

		StatusText.Text = $"Generated {rows}x{cols} grid with {rectCount} rectangles.";
	}

	private RectInt32[] GetRectanglesBoundsInWindowCoordinates()
	{
		var list = new List<RectInt32>();

		var scale = XamlRoot.RasterizationScale;

		foreach (var child in DynamicGrid.Children.OfType<Rectangle>())
		{
			if (child.Visibility != Visibility.Visible)
			{
				continue;
			}

			var childTransform = child.TransformToVisual(null);
			var topLeft = childTransform.TransformPoint(new Point(0, 0));
			var bottomRight = childTransform.TransformPoint(new Point(child.ActualWidth, child.ActualHeight));

			var leftPx = (int)Math.Floor(topLeft.X * scale);
			var topPx = (int)Math.Floor(topLeft.Y * scale);
			var rightPx = (int)Math.Ceiling(bottomRight.X * scale);
			var bottomPx = (int)Math.Ceiling(bottomRight.Y * scale);

			// Clamp to non-negative and cast to int
			int x = Math.Max(0, leftPx);
			int y = Math.Max(0, topPx);
			int w = Math.Max(0, rightPx - x);
			int h = Math.Max(0, bottomPx - y);

			if (w > 0 && h > 0)
			{
				list.Add(new RectInt32(x, y, w, h));
			}
		}

		return list.ToArray();
	}
}

#if __SKIA__
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using SkiaSharp;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Private.Infrastructure;
using WinColor = Windows.UI.Color;
using WinColors = Microsoft.UI.Colors;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Shapes;

// Skia version of the (native-only, currently [Ignore]d) Basics_Shapes_Tests UI test. For each
// shape x stretch x size, it builds the same 4x4 HorizontalAlignment x VerticalAlignment grid that
// produced the WinUI golden screenshots, renders it via RenderTargetBitmap at the same logical size,
// and compares against the embedded WinUI golden. Guards #18265 (UniformToFill vertical alignment).
[TestClass]
[RunsOnUIThread]
public class Given_Basic_Shapes_Parity
{
	private const int SmallWidth = 100, SmallHeight = 75, LargeWidth = 300, LargeHeight = 225;

	// A pixel "differs" when any channel (composited on white) is off by more than this.
	private const int ColorThreshold = 80;
	// A config "matches" the golden when fewer than this fraction of pixels differ. A correct render
	// measures < 0.7%; the #18265 bug (shape in the wrong place) measures > 20%.
	private const double MaxMismatchFraction = 0.03;

	private static Shape NewRectangle() => new Rectangle
	{
		Fill = new SolidColorBrush(WinColor.FromArgb(160, 255, 0, 0)),
		Stroke = new SolidColorBrush(WinColor.FromArgb(255, 255, 0, 0)),
		StrokeThickness = 6
	};

	private static Shape NewEllipse() => new Ellipse
	{
		Fill = new SolidColorBrush(WinColor.FromArgb(160, 255, 128, 0)),
		Stroke = new SolidColorBrush(WinColor.FromArgb(255, 255, 128, 0)),
		StrokeThickness = 6
	};

	private static readonly (string id, Action<Shape> alter)[] _stretches =
	{
		("Default", _ => { }),
		("None", s => s.Stretch = Stretch.None),
		("Fill", s => s.Stretch = Stretch.Fill),
		("Uniform", s => s.Stretch = Stretch.Uniform),
		("UniformToFill", s => s.Stretch = Stretch.UniformToFill),
	};

	private static readonly (string id, Action<Shape> alter)[] _sizes =
	{
		("Unconstrained", _ => { }),
		("FixedSmall", s => { s.Width = SmallWidth; s.Height = SmallHeight; }),
		("FixedLarge", s => { s.Width = LargeWidth; s.Height = LargeHeight; }),
		("FixedWidthSmall", s => s.Width = SmallWidth),
		("FixedWidthLarge", s => s.Width = LargeWidth),
		("FixedHeightSmall", s => s.Height = SmallHeight),
		("FixedHeightLarge", s => s.Height = LargeHeight),
		("MinSmall", s => { s.MinWidth = SmallWidth; s.MinHeight = SmallHeight; }),
		("MinLarge", s => { s.MinWidth = LargeWidth; s.MinHeight = LargeHeight; }),
		("MinWidthSmall", s => s.MinWidth = SmallWidth),
		("MinWidthLarge", s => s.MinWidth = LargeWidth),
		("MinHeightSmall", s => s.MinHeight = SmallHeight),
		("MinHeightLarge", s => s.MinHeight = LargeHeight),
		("MaxSmall", s => { s.MaxWidth = SmallWidth; s.MaxHeight = SmallHeight; }),
		("MaxLarge", s => { s.MaxWidth = LargeWidth; s.MaxHeight = LargeHeight; }),
		("MaxWidthSmall", s => s.MaxWidth = SmallWidth),
		("MaxWidthLarge", s => s.MaxWidth = LargeWidth),
		("MaxHeightSmall", s => s.MaxHeight = SmallHeight),
		("MaxHeightLarge", s => s.MaxHeight = LargeHeight),
	};

	[TestMethod]
	public async Task When_Ellipse() => await ValidateShape("Ellipse", NewEllipse);

	[TestMethod]
	public async Task When_Rectangle() => await ValidateShape("Rectangle", NewRectangle);

	private static async Task ValidateShape(string shapeName, Func<Shape> create)
	{
		var host = new Border { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
		await UITestHelper.Load(host, x => x.IsLoaded);

		var failures = new List<string>();
		var compared = 0;

		foreach (var (stretchId, stretchAlter) in _stretches)
		{
			foreach (var (sizeId, sizeAlter) in _sizes)
			{
				var id = $"{shapeName}_{stretchId}_{sizeId}";
				var golden = TryLoadGolden(id);
				if (golden is null)
				{
					continue; // only compare configs that have a captured WinUI golden
				}

				var grid = BuildHoriVertTestGridForScreenshot(() =>
				{
					var s = create();
					sizeAlter(s);
					stretchAlter(s);
					return s;
				});

				host.Child = grid;
				await TestServices.WindowHelper.WaitForLoaded(grid);
				await TestServices.WindowHelper.WaitForIdle();

				var rtb = new RenderTargetBitmap();
				await rtb.RenderAsync(grid, (int)grid.Width, (int)grid.Height);
				var rendered = System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeBufferExtensions.ToArray(await rtb.GetPixelsAsync());

				var fraction = MismatchFraction(rendered, rtb.PixelWidth, rtb.PixelHeight, golden);
				compared++;
				if (fraction > MaxMismatchFraction)
				{
					failures.Add($"{id}: {fraction:P2} of pixels differ from WinUI");
				}

				host.Child = null;
				golden.Dispose();
			}
		}

		Assert.IsTrue(compared > 0, $"No goldens found for {shapeName} (embedded resources missing?)");
		Assert.IsTrue(
			failures.Count == 0,
			$"{failures.Count}/{compared} {shapeName} configurations differ from WinUI:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
	}

	private static SKBitmap TryLoadGolden(string id)
	{
		var assembly = typeof(Given_Basic_Shapes_Parity).Assembly;
		var name = assembly.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith($".{id}.png", StringComparison.Ordinal));
		if (name is null)
		{
			return null;
		}

		using var stream = assembly.GetManifestResourceStream(name);
		return SKBitmap.Decode(stream);
	}

	private static double MismatchFraction(byte[] renderedBgraPremul, int rw, int rh, SKBitmap golden)
	{
		var gp = golden.Pixels; // straight SKColor[]
		var w = Math.Min(rw, golden.Width);
		var h = Math.Min(rh, golden.Height);

		var bad = 0;
		for (var y = 0; y < h; y++)
		{
			for (var x = 0; x < w; x++)
			{
				var i = (y * rw + x) * 4; // BGRA, premultiplied
				var ra = renderedBgraPremul[i + 3] / 255.0;
				var rR = renderedBgraPremul[i + 2] + 255 * (1 - ra);
				var rG = renderedBgraPremul[i + 1] + 255 * (1 - ra);
				var rB = renderedBgraPremul[i + 0] + 255 * (1 - ra);

				var g = gp[y * golden.Width + x];
				var ga = g.Alpha / 255.0;
				var gR = g.Red * ga + 255 * (1 - ga);
				var gG = g.Green * ga + 255 * (1 - ga);
				var gB = g.Blue * ga + 255 * (1 - ga);

				var d = Math.Max(Math.Abs(rR - gR), Math.Max(Math.Abs(rG - gG), Math.Abs(rB - gB)));
				if (d > ColorThreshold)
				{
					bad++;
				}
			}
		}

		return (double)bad / (w * h);
	}

	private static FrameworkElement BuildHoriVertTestGridForScreenshot(Func<Shape> template)
	{
		var grid = BuildHoriVertTestGrid(template, 150);
		grid.Background = new SolidColorBrush(WinColors.White);
		grid.VerticalAlignment = VerticalAlignment.Top;
		grid.HorizontalAlignment = HorizontalAlignment.Left;
		grid.Clip = new RectangleGeometry { Rect = new Rect(0, 0, grid.Width, grid.Height) };
		return new Grid { Width = grid.Width, Height = grid.Height, Children = { grid } };
	}

	private static Grid BuildHoriVertTestGrid(Func<FrameworkElement> template, int itemSize)
	{
		var horizontalAlignments = new[] { HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right, HorizontalAlignment.Stretch };
		var verticalAlignments = new[] { VerticalAlignment.Top, VerticalAlignment.Center, VerticalAlignment.Bottom, VerticalAlignment.Stretch };
		var grid = new Grid();
		var itemDimensions = new Size(itemSize + 2, itemSize + 20 + 2);

		grid.Width = horizontalAlignments.Length * itemDimensions.Width;
		grid.Height = verticalAlignments.Length * itemDimensions.Height;

		grid.ColumnDefinitions.AddRange(horizontalAlignments.Select(_ => new ColumnDefinition { Width = new GridLength(itemDimensions.Width) }));
		grid.RowDefinitions.AddRange(verticalAlignments.Select(_ => new RowDefinition { Height = new GridLength(itemDimensions.Height) }));

		for (var x = 0; x < horizontalAlignments.Length; x++)
			for (var y = 0; y < verticalAlignments.Length; y++)
			{
				var item = template();
				item.HorizontalAlignment = horizontalAlignments[x];
				item.VerticalAlignment = verticalAlignments[y];
				var container = new Border
				{
					Width = itemDimensions.Width,
					Height = itemDimensions.Height,
					BorderBrush = new SolidColorBrush(WinColors.HotPink),
					BorderThickness = new Thickness(1),
					Child = item
				};
				Grid.SetColumn(container, x);
				Grid.SetRow(container, y);
				grid.Children.Add(container);
			}

		return grid;
	}
}
#endif

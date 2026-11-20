#if __SKIA__
#nullable enable
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Composition.Drawing;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_NativeElementHosting;

// A native element is clipped to the area XAML doesn't paint over it; what paints over it must only cut out what
// it actually paints, not its whole bounds.
[TestClass]
[RunsOnUIThread]
public class Given_NativeElementClip
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public async Task When_Outline_Border_Over_Native_Host()
	{
		var overlay = new Border { BorderBrush = new SolidColorBrush(Colors.Red), BorderThickness = new Thickness(4) };

		await AssertNativeClip(overlay, (bounds, isVisible) =>
		{
			Assert.IsTrue(isVisible(bounds.X + 100, bounds.Y + 100), "The page must show inside the outline.");
			Assert.IsFalse(isVisible(bounds.X + 2, bounds.Y + 100), "The outline must still cover the native element.");
		});
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public async Task When_Rounded_Filled_Border_Over_Native_Host()
	{
		var overlay = new Border { Background = new SolidColorBrush(Colors.Red), CornerRadius = new CornerRadius(60) };

		await AssertNativeClip(overlay, (bounds, isVisible) =>
		{
			Assert.IsFalse(isVisible(bounds.X + 100, bounds.Y + 100), "The fill must cover the native element.");
			Assert.IsTrue(isVisible(bounds.X + 3, bounds.Y + 3), "Outside the rounded corner, the native element must show.");
		});
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public async Task When_Ellipse_Over_Native_Host()
	{
		var overlay = new Ellipse { Fill = new SolidColorBrush(Colors.Red) };

		await AssertNativeClip(overlay, (bounds, isVisible) =>
		{
			Assert.IsFalse(isVisible(bounds.X + 100, bounds.Y + 100), "The ellipse must cover the native element.");
			Assert.IsTrue(isVisible(bounds.X + 3, bounds.Y + 3), "Outside the ellipse, the native element must show.");
		});
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public async Task When_Stroked_Ellipse_With_Transparent_Fill_Over_Native_Host()
	{
		var overlay = new Ellipse
		{
			Fill = new SolidColorBrush(Colors.Transparent),
			Stroke = new SolidColorBrush(Colors.Red),
			StrokeThickness = 4,
		};

		await AssertNativeClip(overlay, (bounds, isVisible) =>
		{
			Assert.IsTrue(isVisible(bounds.X + 100, bounds.Y + 100), "A transparent fill must not cover the native element.");
			Assert.IsFalse(isVisible(bounds.X + 2, bounds.Y + 100), "The stroke must still cover the native element.");
		});
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26278")]
	public async Task When_Border_With_Zero_Thickness_Over_Native_Host()
	{
		var overlay = new Border { BorderBrush = new SolidColorBrush(Colors.Red), BorderThickness = new Thickness(0) };

		await AssertNativeClip(overlay, (bounds, isVisible) =>
		{
			Assert.IsTrue(isVisible(bounds.X + 100, bounds.Y + 100), "A border without thickness paints nothing.");
			Assert.IsTrue(isVisible(bounds.X + 3, bounds.Y + 100), "A border without thickness paints nothing.");
		});
	}

	private static async Task AssertNativeClip(FrameworkElement overlay, System.Action<Rect, System.Func<double, double, bool>> assert)
	{
		var host = new Border { Width = 200, Height = 200 };
		overlay.Width = 200;
		overlay.Height = 200;
		overlay.IsHitTestVisible = false;
		var root = new Grid { Children = { host, overlay } };

		await UITestHelper.Load(root);
		host.Visual.SetAsNativeHostVisual(true);
		try
		{
			await UITestHelper.WaitForIdle();

			var rootVisual = WindowHelper.XamlRoot!.VisualTree.RootElement!.Visual;
			using var parentClip = GeometryFactory.Current.CreateRectangleGeometry(new Rect(0, 0, 10000, 10000));
			var seed = GeometryFactory.Current.CreateRectangleGeometry(default);
			var clip = rootVisual.GetNativeViewPathAndZOrder(parentClip, seed, new List<Visual>());
			try
			{
				assert(host.GetAbsoluteBoundsRect(), (x, y) => clip.FillContains(new Vector2((float)x, (float)y)));
			}
			finally
			{
				clip.Release();
				if (!ReferenceEquals(clip, seed))
				{
					seed.Release();
				}
			}
		}
		finally
		{
			host.Visual.SetAsNativeHostVisual(null);
		}
	}
}
#endif

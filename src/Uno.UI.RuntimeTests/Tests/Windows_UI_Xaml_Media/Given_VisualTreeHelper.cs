using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media.VisualTreeHelperPages;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_VisualTreeHelper
	{
#if !NETFX_CORE // Testing internal Uno methods
#if __SKIA__
		[Ignore("https://github.com/unoplatform/uno/issues/7271")]
#endif
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_Nested_In_Native_View()
		{
			var page = new Native_View_Page();
			WindowHelper.WindowContent = page;
			await WindowHelper.WaitForLoaded(page);

			var pageBounds = page.GetOnScreenBounds();
			var statusBarHeight = pageBounds.Y; // Non-zero on Android
			var sut = page.SUT;
			var bounds = sut.GetOnScreenBounds();
			bounds.Y -= statusBarHeight; // Status bar height is included in TransformToVisual on Android, but shouldn't be included in VisualTreeHelper.HitTest call
			var expected = new Rect(25, 205, 80, 40);
			RectAssert.AreEqual(expected, bounds);

			GetHitTestability getHitTestability = null;
			getHitTestability = element => (element as FrameworkElement)?.Background != null ? (element.GetHitTestVisibility(), getHitTestability) : (HitTestability.Invisible, getHitTestability);

			foreach (var point in GetPointsInside(bounds, perimeterOffset: 5))
			{
				var hitTest = VisualTreeHelper.HitTest(point, getHitTestability);
				Assert.AreEqual(sut, hitTest.element);
			}

			foreach (var point in GetPointsOutside(bounds, perimeterOffset: 5))
			{
				var hitTest = VisualTreeHelper.HitTest(point);
				Assert.IsNotNull(hitTest.element);
				Assert.AreNotEqual(sut, hitTest.element);
			}
		}

		private static IEnumerable<Point> GetPointsInside(Rect rect, double perimeterOffset)
		{
			if (perimeterOffset >= rect.Width || perimeterOffset >= rect.Height)
			{
				throw new ArgumentException($"Offset {perimeterOffset} is too large to fit inside Rect {rect}");
			}

			yield return rect.GetCenter();

			var interiorXs = new[] { rect.Left + perimeterOffset, rect.Right - perimeterOffset };
			var interiorYs = new[] { rect.Top + perimeterOffset, rect.Bottom - perimeterOffset };
			foreach (var x in interiorXs)
			{
				foreach (var y in interiorYs)
				{
					yield return new Point(x, y);
				}
			}
		}
#endif

		private static IEnumerable<Point> GetPointsOutside(Rect rect, double perimeterOffset)
		{
			var exteriorXs = new[] { rect.Left - perimeterOffset, rect.Left, rect.Right, rect.Right + perimeterOffset };
			var exteriorYs = new[] { rect.Top - perimeterOffset, rect.Bottom + perimeterOffset };
			foreach (var x in exteriorXs)
			{
				foreach (var y in exteriorYs)
				{
					yield return new Point(x, y);
				}
			}

			var remainingXs = new[] { rect.Left - perimeterOffset, rect.Right + perimeterOffset };
			var remainingYs = new[] { rect.Top, rect.Bottom };
			foreach (var x in remainingXs)
			{
				foreach (var y in remainingYs)
				{
					yield return new Point(x, y);
				}
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media.VisualTreeHelperPages;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using FluentAssertions;
using Uno.Extensions;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_VisualTreeHelper
	{
		[TestMethod]
		public void OpenPopups_Flyouts_Unique()
		{
			var button = new Microsoft.UI.Xaml.Controls.Button();
			var flyout = new Flyout();
			FlyoutBase.SetAttachedFlyout(button, flyout);
			WindowHelper.WindowContent = button;
			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);
			FlyoutBase.ShowAttachedFlyout(button);
			Assert.AreEqual(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);
			flyout.Hide();
			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);
		}

		[TestMethod]
		public void OpenPopups_Popups_Unique()
		{
			var popup = new Popup();
			popup.XamlRoot = WindowHelper.XamlRoot;
			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);
			popup.IsOpen = true;
			Assert.AreEqual(1, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);
			popup.IsOpen = false;
			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).Count);
		}

		[TestMethod]
		public void OpenPopups_Popups_Include_Instance()
		{
			var popup = new Popup();
			popup.XamlRoot = WindowHelper.XamlRoot;
			popup.IsOpen = true;
			CollectionAssert.Contains(VisualTreeHelper.GetOpenPopupsForXamlRoot(WindowHelper.XamlRoot).ToArray(), popup);
			popup.IsOpen = false;
		}

#if !WINAPPSDK // Testing internal Uno methods
		[TestMethod]
		[RequiresFullWindow]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
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
				var hitTest = VisualTreeHelper.HitTest(point, WindowHelper.WindowContent.XamlRoot, getHitTestability);
				Assert.AreEqual(sut, hitTest.element);
			}

			foreach (var point in GetPointsOutside(bounds, perimeterOffset: 5))
			{
				var hitTest = VisualTreeHelper.HitTest(point, WindowHelper.XamlRoot);
				Assert.IsNotNull(hitTest.element);
				Assert.AreNotEqual(sut, hitTest.element);
			}
		}

		[TestMethod]
		[RunsOnUIThread]
#if !UNO_HAS_MANAGED_POINTERS
		[Ignore("Root visual tree elements are not configured properly to use managed hit testing.")]
#endif
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_HitTestTranslatedElement()
		{
			Border root, transformed, nested;
			root = new Border
			{
				Name = "Root",
				Width = 512,
				Height = 512,
				Background = new SolidColorBrush(Colors.DeepSkyBlue),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Child = transformed = new Border
				{
					Name = "Transformed",
					Width = 128,
					Height = 128,
					Background = new SolidColorBrush(Colors.DeepPink),
					RenderTransform = new TranslateTransform { X = 128, Y = 128 },
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Child = nested = new Border
					{
						Name = "Nested",
						Width = 64,
						Height = 64,
						Background = new SolidColorBrush(Colors.Chartreuse),
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center,
					}
				}
			};

			var position = (await UITestHelper.Load(root)).Location;

			AssertName(VisualTreeHelper.HitTest(position.Offset(256), root.XamlRoot).element!, "Root");
			AssertName(VisualTreeHelper.HitTest(position.Offset(256 + 65), root.XamlRoot).element!, "Transformed");
			AssertName(VisualTreeHelper.HitTest(position.Offset(256 + 128), root.XamlRoot).element!, "Nested");
		}

		[TestMethod]
		[RunsOnUIThread]
#if !UNO_HAS_MANAGED_POINTERS
		[Ignore("Root visual tree elements are not configured properly to use managed hit testing.")]
#endif
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_HitTestScaledElement()
		{
			Border root, transformed, nested;
			root = new Border
			{
				Name = "Root",
				Width = 512,
				Height = 512,
				Background = new SolidColorBrush(Colors.DeepSkyBlue),
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Top,
				Child = transformed = new Border
				{
					Name = "Transformed",
					Width = 128,
					Height = 128,
					Background = new SolidColorBrush(Colors.DeepPink),
					RenderTransform = new ScaleTransform { ScaleX = 2, ScaleY = 2 },
					RenderTransformOrigin = new Point(.5, .5),
					HorizontalAlignment = HorizontalAlignment.Center,
					VerticalAlignment = VerticalAlignment.Center,
					Child = nested = new Border
					{
						Name = "Nested",
						Width = 64,
						Height = 64,
						Background = new SolidColorBrush(Colors.Chartreuse),
						HorizontalAlignment = HorizontalAlignment.Center,
						VerticalAlignment = VerticalAlignment.Center,
					}
				}
			};

			var position = (await UITestHelper.Load(root)).Location;

			AssertName(VisualTreeHelper.HitTest(position.Offset(128 - 5), root.XamlRoot).element!, "Root");
			AssertName(VisualTreeHelper.HitTest(position.Offset(128 + 5), root.XamlRoot).element!, "Transformed");
			AssertName(VisualTreeHelper.HitTest(position.Offset(256 - 60), root.XamlRoot).element!, "Nested");
		}

		private static void AssertName(UIElement element, string expectedName)
		{
			((FrameworkElement)element).Name!.Should().Be(expectedName);
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

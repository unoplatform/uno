using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
[RunsOnUIThread]
public class Given_VisualTreeHelper_FindElementsInHostCoordinates
{
	[TestMethod]
	public async Task When_Overlapping_Siblings_Then_Front_To_Back_Without_Duplicates()
	{
		var back = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };
		var front = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Blue) };
		var root = new Grid { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Green), Children = { back, front } };

		try
		{
			await UITestHelper.Load(root);

			var result = VisualTreeHelper.FindElementsInHostCoordinates(ToHost(root, 50, 50), root).ToArray();

			CollectionAssert.AreEqual(new UIElement[] { front, back, root }, result);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Child_Outside_Of_Parent_Then_Parent_Chain_Included()
	{
		var child = new Border { Width = 20, Height = 20, Background = new SolidColorBrush(Colors.Red) };
		Canvas.SetLeft(child, 50);
		Canvas.SetTop(child, 50);
		var canvas = new Canvas
		{
			Width = 10,
			Height = 10,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Children = { child }
		};
		var root = new Grid { Width = 100, Height = 100, Children = { canvas } };

		try
		{
			await UITestHelper.Load(root, x => x.IsLoaded);

			var result = VisualTreeHelper.FindElementsInHostCoordinates(ToHost(child, 10, 10), root).ToArray();

			CollectionAssert.AreEqual(new UIElement[] { child, canvas, root }, result);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Clipped_By_Ancestor_Then_Not_Found()
	{
		var child = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };
		var clipper = new Grid
		{
			Width = 100,
			Height = 100,
			Background = new SolidColorBrush(Colors.Green),
			Clip = new RectangleGeometry { Rect = new Rect(0, 0, 50, 50) },
			Children = { child }
		};
		var root = new Grid { Width = 100, Height = 100, Children = { clipper } };

		try
		{
			await UITestHelper.Load(root, x => x.IsLoaded);

			var clippedOut = VisualTreeHelper.FindElementsInHostCoordinates(ToHost(child, 75, 75), root).ToArray();
			var inside = VisualTreeHelper.FindElementsInHostCoordinates(ToHost(child, 25, 25), root).ToArray();

			Assert.IsEmpty(clippedOut);
			CollectionAssert.AreEqual(new UIElement[] { child, clipper, root }, inside);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Not_HitTestVisible_Then_Excluded_Even_With_IncludeAllElements()
	{
		var sut = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red), IsHitTestVisible = false };
		var root = new Grid { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Green), Children = { sut } };

		try
		{
			await UITestHelper.Load(root);

			var result = VisualTreeHelper.FindElementsInHostCoordinates(ToHost(root, 50, 50), root, includeAllElements: true).ToArray();

			CollectionAssert.AreEqual(new UIElement[] { root }, result);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_No_Background_Then_Found_Only_With_IncludeAllElements()
	{
		var transparent = new Border { Width = 100, Height = 100 };
		var root = new Grid { Width = 100, Height = 100, Children = { transparent } };

		try
		{
			await UITestHelper.Load(root, x => x.IsLoaded);

			var visibleOnly = VisualTreeHelper.FindElementsInHostCoordinates(ToHost(root, 50, 50), root).ToArray();
			var all = VisualTreeHelper.FindElementsInHostCoordinates(ToHost(root, 50, 50), root, includeAllElements: true).ToArray();

			Assert.IsEmpty(visibleOnly);
			CollectionAssert.AreEqual(new UIElement[] { transparent, root }, all);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_Subtree_Is_Null_Then_Throws()
		=> Assert.ThrowsExactly<ArgumentException>(() => VisualTreeHelper.FindElementsInHostCoordinates(new Point(1, 1), null).ToArray());

	[TestMethod]
	public async Task When_Popup_Is_In_Subtree_Then_Popup_Content_Found_First()
	{
		var popupChild = new Border { Width = 50, Height = 50, Background = new SolidColorBrush(Colors.Red) };
		var popup = new Popup { Child = popupChild };
		var underlay = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Green) };
		var root = new Grid { Width = 100, Height = 100, Children = { underlay, popup } };

		try
		{
			await UITestHelper.Load(root);
			popup.IsOpen = true;
			await WindowHelper.WaitForLoaded(popupChild);

			var result = VisualTreeHelper.FindElementsInHostCoordinates(ToHost(popupChild, 25, 25), root).ToArray();

			CollectionAssert.AreEqual(new UIElement[] { popupChild, popup, underlay, root }, result);
		}
		finally
		{
			popup.IsOpen = false;
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Shape_Has_No_Fill_Then_IncludeAllElements_Uses_Its_Geometry()
	{
		var ellipse = new Ellipse { Width = 100, Height = 100 };
		var root = new Grid { Width = 100, Height = 100, Children = { ellipse } };

		try
		{
			await UITestHelper.Load(root, x => x.IsLoaded);

			var center = VisualTreeHelper.FindElementsInHostCoordinates(ToHost(root, 50, 50), root, includeAllElements: true).ToArray();
			var corner = VisualTreeHelper.FindElementsInHostCoordinates(ToHost(root, 5, 5), root, includeAllElements: true).ToArray();

			// The fill is tested as if it were solid, so the transparent corners of the ellipse are not hit.
			CollectionAssert.AreEqual(new UIElement[] { ellipse, root }, center);
			CollectionAssert.AreEqual(new UIElement[] { root }, corner);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Rounded_Border_Then_Children_Clipped_At_Corners()
	{
		var child = new Border { Background = new SolidColorBrush(Colors.Blue) };
		var root = new Border { Width = 100, Height = 100, CornerRadius = new CornerRadius(50), Background = new SolidColorBrush(Colors.Red), Child = child };

		try
		{
			await UITestHelper.Load(root);

			var center = VisualTreeHelper.FindElementsInHostCoordinates(ToHost(root, 50, 50), root).ToArray();
			var corner = VisualTreeHelper.FindElementsInHostCoordinates(ToHost(root, 3, 3), root).ToArray();

			CollectionAssert.AreEqual(new UIElement[] { child, root }, center);
			Assert.IsEmpty(corner);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Added_Before_Loaded_Then_Found()
	{
		var root = new Grid { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Green) };

		try
		{
			await UITestHelper.Load(root);

			// The API updates the layout itself, it does not wait for Loaded.
			var sut = new Border { Width = 100, Height = 100, Background = new SolidColorBrush(Colors.Red) };
			root.Children.Add(sut);
			var result = VisualTreeHelper.FindElementsInHostCoordinates(ToHost(root, 50, 50), root).ToArray();

			CollectionAssert.AreEqual(new UIElement[] { sut, root }, result);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_IsLightDismissEnabled_Changes_While_Open_Then_Value_At_Open_Is_Used()
	{
		var popupChild = new Border { Width = 50, Height = 50, Background = new SolidColorBrush(Colors.Red) };
		var popup = new Popup { Child = popupChild, IsLightDismissEnabled = false };
		var root = new Grid { Width = 200, Height = 200, Children = { popup } };

		try
		{
			await UITestHelper.Load(root);
			popup.IsOpen = true;
			await WindowHelper.WaitForLoaded(popupChild);

			popup.IsLightDismissEnabled = true;

			// Outside of the popup content, only a light-dismiss popup makes the popup root hit.
			var result = VisualTreeHelper.FindElementsInHostCoordinates(ToHost(root, 150, 150), root).ToArray();

			Assert.IsFalse(result.Any(e => e.GetType().Name == "PopupRoot"));
		}
		finally
		{
			popup.IsOpen = false;
			WindowHelper.WindowContent = null;
		}
	}

	private static Point ToHost(UIElement element, double x, double y)
		=> element.TransformToVisual(null).TransformPoint(new Point(x, y));
}

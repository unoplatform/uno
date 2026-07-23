using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_Popup_HVAlign_UITest
{
	// A Popup with no PlacementTarget arranges itself with zero size, so its own
	// HorizontalAlignment/VerticalAlignment only move its (zero-size) anchor point within
	// its logical parent's bounds; the open content then renders anchored at that point.
	[TestMethod]
	[RunsOnUIThread]
	[DataRow(HorizontalAlignment.Stretch, VerticalAlignment.Stretch, 0d, 0d)]
	[DataRow(HorizontalAlignment.Left, VerticalAlignment.Top, 0d, 0d)]
	[DataRow(HorizontalAlignment.Center, VerticalAlignment.Center, 0.5d, 0.5d)]
	[DataRow(HorizontalAlignment.Right, VerticalAlignment.Bottom, 1d, 1d)]
	public async Task When_Popup_Opens_Without_PlacementTarget(HorizontalAlignment hAlign, VerticalAlignment vAlign, double xMul, double yMul)
	{
		var popupContent = new Border
		{
			Width = 312,
			Background = new SolidColorBrush(Windows.UI.Colors.Pink),
			Child = new TextBlock { Margin = new Thickness(16), Text = "Asd" },
		};

		var popup = new Popup
		{
			HorizontalAlignment = hAlign,
			VerticalAlignment = vAlign,
			IsLightDismissEnabled = true,
			Child = popupContent,
		};

		// The "zone" the popup anchors within: same idea as the Grid used in the legacy
		// UITest sample (Popup_HVAlignments) to visualize the popup's available placement area.
		var zone = new Grid
		{
			Width = 80,
			Height = 80,
			Margin = new Thickness(40, 60, 0, 0),
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Background = new SolidColorBrush(Windows.UI.Colors.SkyBlue),
			Children = { popup },
		};

		try
		{
			await UITestHelper.Load(zone);

			popup.IsOpen = true;
			await WindowHelper.WaitForIdle();

			var zoneRect = zone.GetAbsoluteBounds();
			var popupRect = popupContent.GetAbsoluteBounds();

			var expectedX = zoneRect.X + (zoneRect.Width * xMul);
			var expectedY = zoneRect.Y + (zoneRect.Height * yMul);

			Assert.AreEqual(expectedX, popupRect.X, 1d, "Popup X placement");
			Assert.AreEqual(expectedY, popupRect.Y, 1d, "Popup Y placement");
		}
		finally
		{
			popup.IsOpen = false;
			WindowHelper.WindowContent = null;
		}
	}
}

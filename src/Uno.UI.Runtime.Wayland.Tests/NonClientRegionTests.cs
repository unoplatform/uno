#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Wayland;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class NonClientRegionTests
{
	private const uint Top = 1, Bottom = 2, Left = 4, Right = 8;

	[TestMethod]
	public void DefaultCaptionCoversTitleBarHeight()
	{
		var regions = new WaylandNonClientRegions();
		Assert.AreEqual((WaylandNonClientArea.Caption, 0u), regions.HitTest(10, 0, 32));
		Assert.AreEqual((WaylandNonClientArea.Caption, 0u), regions.HitTest(500, 31.5, 32));
		Assert.AreEqual((WaylandNonClientArea.Client, 0u), regions.HitTest(10, 32, 32));
		// A collapsed title bar has no default caption.
		Assert.AreEqual((WaylandNonClientArea.Client, 0u), regions.HitTest(10, 0, 0));
	}

	[TestMethod]
	public void DeclaredCaptionReplacesDefault()
	{
		var regions = new WaylandNonClientRegions();
		regions.Set(WaylandNonClientRegionKind.Caption, [new(100, 0, 200, 40)]);
		Assert.AreEqual((WaylandNonClientArea.Client, 0u), regions.HitTest(10, 10, 32));
		Assert.AreEqual((WaylandNonClientArea.Caption, 0u), regions.HitTest(100, 39, 32));
		Assert.AreEqual((WaylandNonClientArea.Client, 0u), regions.HitTest(300, 10, 32));
		regions.Set(WaylandNonClientRegionKind.Caption, []);
		Assert.AreEqual((WaylandNonClientArea.Caption, 0u), regions.HitTest(10, 10, 32));
	}

	[TestMethod]
	public void PassthroughAndCaptionButtonsWinOverCaption()
	{
		var regions = new WaylandNonClientRegions();
		regions.Set(WaylandNonClientRegionKind.Caption, [new(0, 0, 800, 40)]);
		regions.Set(WaylandNonClientRegionKind.Passthrough, [new(0, 0, 300, 40)]);
		regions.Set(WaylandNonClientRegionKind.Close, [new(754, 0, 46, 40)]);
		regions.Set(WaylandNonClientRegionKind.LeftBorder, [new(0, 0, 5, 40)]);
		Assert.AreEqual((WaylandNonClientArea.Client, 0u), regions.HitTest(2, 10, 32));
		Assert.AreEqual((WaylandNonClientArea.Client, 0u), regions.HitTest(780, 10, 32));
		Assert.AreEqual((WaylandNonClientArea.Caption, 0u), regions.HitTest(400, 10, 32));
	}

	[TestMethod]
	public void BordersCombineIntoResizeEdges()
	{
		var regions = new WaylandNonClientRegions();
		regions.Set(WaylandNonClientRegionKind.Caption, [new(0, 0, 800, 40)]);
		regions.Set(WaylandNonClientRegionKind.TopBorder, [new(0, 0, 800, 4)]);
		regions.Set(WaylandNonClientRegionKind.LeftBorder, [new(0, 0, 4, 600)]);
		regions.Set(WaylandNonClientRegionKind.RightBorder, [new(796, 0, 4, 600)]);
		regions.Set(WaylandNonClientRegionKind.BottomBorder, [new(0, 0, 800, 600)]);
		regions.Set(WaylandNonClientRegionKind.Icon, [new(8, 8, 16, 16)]);
		// Top and bottom overlap everywhere here, so only the horizontal edge is left.
		Assert.AreEqual((WaylandNonClientArea.Resize, Left), regions.HitTest(1, 1, 32));
		regions.Set(WaylandNonClientRegionKind.BottomBorder, []);
		Assert.AreEqual((WaylandNonClientArea.Resize, Top | Left), regions.HitTest(1, 1, 32));
		Assert.AreEqual((WaylandNonClientArea.Resize, Top | Right), regions.HitTest(799, 0, 32));
		Assert.AreEqual((WaylandNonClientArea.Resize, Right), regions.HitTest(799, 300, 32));
		Assert.AreEqual((WaylandNonClientArea.Icon, 0u), regions.HitTest(10, 10, 32));
		Assert.AreEqual((WaylandNonClientArea.Caption, 0u), regions.HitTest(100, 10, 32));
		regions.Clear();
		Assert.AreEqual(0, regions.Get(WaylandNonClientRegionKind.TopBorder).Length);
		Assert.AreEqual((WaylandNonClientArea.Client, 0u), regions.HitTest(100, 100, 32));
	}
}

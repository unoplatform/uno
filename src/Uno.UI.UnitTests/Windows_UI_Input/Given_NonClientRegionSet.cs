using Microsoft.UI.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.UI.Input.Internal;
using Windows.Graphics;

namespace Uno.UI.Tests.Windows_UI_Input;

[UITestClass]
public class Given_NonClientRegionSet
{
	[TestMethod]
	public void When_Passthrough_Registered_After_Caption_Passthrough_Wins()
	{
		var sut = new NonClientRegionSet();

		// The order SetTitleBar + TitleBar use: the whole bar as Caption, then interactive holes as Passthrough.
		sut.Set(NonClientRegionKind.Caption, [new RectInt32(0, 0, 600, 160)]);
		sut.Set(NonClientRegionKind.Passthrough, [new RectInt32(100, 60, 150, 60)]);

		Assert.AreEqual(NonClientRegionKind.Passthrough, sut.HitTest(new PointInt32(150, 90)));
		Assert.AreEqual(NonClientRegionKind.Caption, sut.HitTest(new PointInt32(400, 90)));
	}

	[TestMethod]
	public void When_Icon_Overlaps_Caption_Icon_Wins()
	{
		var sut = new NonClientRegionSet();

		sut.Set(NonClientRegionKind.Icon, [new RectInt32(10, 10, 20, 20)]);
		sut.Set(NonClientRegionKind.Caption, [new RectInt32(0, 0, 600, 48)]);

		Assert.AreEqual(NonClientRegionKind.Icon, sut.HitTest(new PointInt32(15, 15)));
	}

	[TestMethod]
	public void When_Point_Outside_All_Regions_Returns_Null()
	{
		var sut = new NonClientRegionSet();
		sut.Set(NonClientRegionKind.Caption, [new RectInt32(0, 0, 600, 48)]);

		Assert.IsNull(sut.HitTest(new PointInt32(300, 48)));
		Assert.IsNull(sut.HitTest(new PointInt32(600, 10)));
	}

	[TestMethod]
	public void When_Rect_Edges_Left_And_Top_Inclusive()
	{
		var sut = new NonClientRegionSet();
		sut.Set(NonClientRegionKind.Caption, [new RectInt32(10, 20, 30, 40)]);

		Assert.AreEqual(NonClientRegionKind.Caption, sut.HitTest(new PointInt32(10, 20)));
		Assert.AreEqual(NonClientRegionKind.Caption, sut.HitTest(new PointInt32(39, 59)));
		Assert.IsNull(sut.HitTest(new PointInt32(40, 59)));
	}

	[TestMethod]
	public void When_Set_Empty_Region_Is_Removed()
	{
		var sut = new NonClientRegionSet();
		sut.Set(NonClientRegionKind.Caption, [new RectInt32(0, 0, 600, 48)]);

		sut.Set(NonClientRegionKind.Caption, []);

		Assert.IsFalse(sut.HasRects(NonClientRegionKind.Caption));
		Assert.AreEqual(0, sut.Get(NonClientRegionKind.Caption).Length);
		Assert.IsNull(sut.HitTest(new PointInt32(10, 10)));
	}

	[TestMethod]
	public void When_ClearAll_No_Region_Remains()
	{
		var sut = new NonClientRegionSet();
		sut.Set(NonClientRegionKind.Caption, [new RectInt32(0, 0, 600, 48)]);
		sut.Set(NonClientRegionKind.Passthrough, [new RectInt32(0, 0, 10, 10)]);

		sut.ClearAll();

		Assert.IsNull(sut.HitTest(new PointInt32(5, 5)));
	}
}

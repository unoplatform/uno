using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.X11;
using Windows.Graphics;

namespace Uno.UI.Tests.Windows_UI_Input;

[TestClass]
public class Given_X11MonitorMath
{
	private static readonly RectInt32 _left = new(0, 0, 1920, 1080);
	private static readonly RectInt32 _right = new(1920, 0, 1280, 1024);

	[TestMethod]
	public void When_Point_Is_On_A_Monitor_Then_It_Is_That_One()
	{
		Assert.IsTrue(X11MonitorMath.TryFindNearest(new[] { _left, _right }, new PointInt32(2000, 10), out var monitor));
		Assert.AreEqual(_right, monitor);
	}

	[TestMethod]
	public void When_Point_Is_On_The_Edge_Then_The_Monitor_Starting_There_Wins()
	{
		Assert.IsTrue(X11MonitorMath.TryFindNearest(new[] { _left, _right }, new PointInt32(1920, 10), out var monitor));
		Assert.AreEqual(_right, monitor);
	}

	[TestMethod]
	public void When_Point_Is_Between_Monitors_Then_The_Nearest_Wins()
	{
		// Under the shorter right monitor, closer to the left one.
		Assert.IsTrue(X11MonitorMath.TryFindNearest(new[] { _left, _right }, new PointInt32(1925, 1500), out var monitor));
		Assert.AreEqual(_right, monitor);

		Assert.IsTrue(X11MonitorMath.TryFindNearest(new[] { _left, _right }, new PointInt32(1900, 1500), out monitor));
		Assert.AreEqual(_left, monitor);
	}

	[TestMethod]
	public void When_No_Monitor_Has_Size_Then_None_Is_Found()
		=> Assert.IsFalse(X11MonitorMath.TryFindNearest(new[] { new RectInt32(0, 0, 0, 0) }, default, out _));

	[TestMethod]
	public void When_Work_Area_Spans_All_Monitors_Then_It_Is_Clipped_To_The_Monitor()
	{
		var workArea = new RectInt32(0, 30, 3200, 1050);

		Assert.AreEqual(new RectInt32(1920, 30, 1280, 994), X11MonitorMath.GetWorkArea(_right, new[] { workArea }));
	}

	[TestMethod]
	public void When_Work_Areas_Are_Per_Monitor_Then_The_Overlapping_One_Wins()
	{
		var left = new RectInt32(0, 30, 1920, 1050);
		var right = new RectInt32(1920, 0, 1280, 984);

		Assert.AreEqual(right, X11MonitorMath.GetWorkArea(_right, new[] { left, right }));
	}

	[TestMethod]
	public void When_Work_Area_Is_Unknown_Then_It_Is_The_Monitor()
	{
		Assert.AreEqual(_right, X11MonitorMath.GetWorkArea(_right, System.Array.Empty<RectInt32>()));
		Assert.AreEqual(_right, X11MonitorMath.GetWorkArea(_right, new[] { new RectInt32(0, 0, 100, 100) }));
	}
}

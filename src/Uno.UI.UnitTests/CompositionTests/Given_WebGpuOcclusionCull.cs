#nullable enable

using System.Collections.Generic;
using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Composition.WebGpu;

namespace Uno.UI.Tests.CompositionTests;

// The occlusion cull drops ops that a later opaque op paints over. On a partial repaint the pass is scissored to the
// damage, so only what lies inside the damage counts, both for the ops and for what covers them.
[UITestClass]
public class Given_WebGpuOcclusionCull
{
	private static readonly Vector4 Window = new(0, 0, 1000, 800);
	private static readonly Vector4 Damage = new(100, 100, 200, 200);
	private static readonly Vector4 Unbounded = new(float.MinValue, float.MinValue, float.MaxValue, float.MaxValue);

	[TestMethod]
	public void When_Partial_Repaint_Then_Background_Under_Another_Is_Culled()
	{
		var ops = new List<DrawOp> { Opaque(Window, id: 1), Opaque(Window, id: 2) };

		WebGpuFrame.CullOccluded(ops, Damage);

		CollectionAssert.AreEqual(new[] { 2 }, Ids(ops), "The app background under the page background was drawn.");
	}

	[TestMethod]
	public void When_Partial_Repaint_Then_Op_Outside_The_Damage_Is_Culled()
	{
		var ops = new List<DrawOp> { Opaque(Window, id: 1), Translucent(new Vector4(500, 500, 600, 600), id: 2) };

		WebGpuFrame.CullOccluded(ops, Damage);

		CollectionAssert.AreEqual(new[] { 1 }, Ids(ops), "An op outside the damage was drawn.");
	}

	// An op touching any pixel of the damage, even by a fraction, must still be drawn; one ending on its edge draws nothing.
	[TestMethod]
	public void When_Op_Reaches_Into_The_Damage_By_A_Fraction_Then_It_Is_Kept()
	{
		var ops = new List<DrawOp>
		{
			Translucent(new Vector4(199.6f, 150, 260, 170), id: 1),   // within half a pixel of column 199's centre
			Translucent(new Vector4(200, 150, 260, 170), id: 2),      // starts on the damage's right edge
			Translucent(new Vector4(150, 40, 170, 100.4f), id: 3),    // within half a pixel of row 100's centre
			Translucent(new Vector4(150, 40, 170, 100), id: 4),       // ends on the damage's top edge
		};

		WebGpuFrame.CullOccluded(ops, Damage);

		CollectionAssert.AreEqual(new[] { 1, 3 }, Ids(ops));
	}

	[TestMethod]
	public void When_Partial_Repaint_Then_Cover_Is_Only_What_Lies_In_The_Damage()
	{
		// The panel covers the damage but not the window: the background under it is hidden only within the damage.
		var panel = new Vector4(50, 50, 250, 250);

		var bounded = new List<DrawOp> { Opaque(Window, id: 1), Opaque(panel, id: 2) };
		WebGpuFrame.CullOccluded(bounded, Damage);
		CollectionAssert.AreEqual(new[] { 2 }, Ids(bounded));

		var unbounded = new List<DrawOp> { Opaque(Window, id: 1), Opaque(panel, id: 2) };
		WebGpuFrame.CullOccluded(unbounded, Unbounded);
		CollectionAssert.AreEqual(new[] { 1, 2 }, Ids(unbounded));
	}

	[TestMethod]
	public void When_Full_Repaint_Then_Only_Fully_Covered_Ops_Are_Culled()
	{
		var ops = new List<DrawOp>
		{
			Opaque(Window, id: 1),
			Translucent(new Vector4(500, 500, 600, 600), id: 2),
			Opaque(Window, id: 3),
			Translucent(new Vector4(10, 10, 20, 20), id: 4),
		};

		WebGpuFrame.CullOccluded(ops, Unbounded);

		CollectionAssert.AreEqual(new[] { 3, 4 }, Ids(ops));
	}

	// Ops are told apart by their depth, which the cull never reads.
	private static DrawOp Opaque(Vector4 rect, int id)
		=> new() { Kind = DrawKind.Solid, Bounds = rect, Cover = rect, Clip = ClipData.None, Depth = id };

	private static DrawOp Translucent(Vector4 rect, int id)
		=> new() { Kind = DrawKind.Solid, Bounds = rect, Clip = ClipData.None, Depth = id };

	private static int[] Ids(List<DrawOp> ops) => ops.ConvertAll(op => (int)op.Depth).ToArray();
}

#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Wayland.Rendering;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class RenderScaleTests
{
	[TestMethod]
	[DataRow(true, 120u, 3, 1.0)]
	[DataRow(true, 180u, 1, 1.5)]
	[DataRow(true, 0u, 2, 2.0)]
	[DataRow(false, 180u, 2, 2.0)]
	[DataRow(false, 0u, 0, 1.0)]
	public void FractionalScaleNeedsAViewport(bool hasViewport, uint preferredScale, int integerScale, double expected)
		=> Assert.AreEqual(expected, WaylandRenderScale.Compute(hasViewport, preferredScale, integerScale));

	[TestMethod]
	[DataRow(100, 1.5, 150)]
	[DataRow(101, 1.5, 152)]
	[DataRow(103, 1.5, 155)]
	[DataRow(799, 1.25, 999)]
	[DataRow(1, 1.75, 2)]
	public void BufferSizeRoundsHalfwayUp(int logical, double scale, int expected)
		=> Assert.AreEqual(expected, WaylandRenderScale.ToBufferPixels(logical, scale));
}

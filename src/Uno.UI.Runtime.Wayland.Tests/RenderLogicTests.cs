#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Wayland;
using Uno.UI.Runtime.Wayland.Rendering;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class RenderLogicTests
{
	[TestMethod]
	public void EglConfigRejectsMultisamplingAndUnknownSampleCounts()
	{
		IntPtr[] configs = [new(1), new(2), new(3), new(4)];
		Assert.AreEqual(configs[3], WaylandEglConfig.Select(configs, _ => 0,
			config => config == configs[0] ? 1 : config == configs[2] ? null : 0,
			config => config == configs[1] ? 4 : 0));
		Assert.ThrowsExactly<NotSupportedException>(() => WaylandEglConfig.Select(configs, _ => 0, _ => 1, _ => 4));
	}

	[TestMethod]
	public void EglConfigPrefersNonblockingSwaps()
	{
		IntPtr[] configs = [new(1), new(2), new(3)];
		Assert.AreEqual(configs[2], WaylandEglConfig.Select(configs, config => config == configs[0] ? 1 : config == configs[1] ? null : 0, _ => 0, _ => 0));
	}

	[TestMethod]
	public void EglConfigRejectsClampedOrUnknownSwapIntervals()
	{
		IntPtr[] configs = [new(1), new(2)];
		Assert.ThrowsExactly<NotSupportedException>(() => WaylandEglConfig.Select(configs, config => config == configs[0] ? 1 : null, _ => 0, _ => 0));
		Assert.ThrowsExactly<NotSupportedException>(() => WaylandEglConfig.Select([], _ => 0, _ => 0, _ => 0));
	}

	[TestMethod]
	public void IntegerScaleFollowsTheLargestEnteredOutput()
	{
		var low = WaylandOutputInfo.Unknown with { Name = "A", Scale = 1 };
		var high = WaylandOutputInfo.Unknown with { Name = "B", Scale = 2 };
		Assert.AreEqual(2, WaylandOutput.GetIntegerScale([low, high], 1));
		Assert.AreEqual(1, WaylandOutput.GetIntegerScale([low], 2));
		Assert.AreEqual(2, WaylandOutput.GetIntegerScale([], 2), "Without outputs the previous scale is kept.");
	}

	[TestMethod]
	public void OutputDiagonalNeedsPhysicalSize()
	{
		Assert.IsNull(WaylandOutputInfo.Unknown.DiagonalSizeInInches);
		var output = WaylandOutputInfo.Unknown with { PhysicalWidthMm = 300, PhysicalHeightMm = 400 };
		Assert.AreEqual(500 / 25.4, output.DiagonalSizeInInches!.Value, 1e-9);
	}
}

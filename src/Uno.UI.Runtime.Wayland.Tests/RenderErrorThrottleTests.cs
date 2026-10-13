#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Wayland.Rendering;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class RenderErrorThrottleTests
{
	[TestMethod]
	public void FirstFailureIsReportedAndLaterOnesAreSummarized()
	{
		var throttle = new WaylandRenderErrorThrottle();
		Assert.IsTrue(throttle.ShouldReport(0, out var suppressed));
		Assert.AreEqual(0, suppressed);
		Assert.IsFalse(throttle.ShouldReport(16, out _));
		Assert.IsFalse(throttle.ShouldReport(32, out _));
		Assert.IsFalse(throttle.ShouldReport(WaylandRenderErrorThrottle.SummaryIntervalMilliseconds - 1, out _));

		Assert.IsTrue(throttle.ShouldReport(WaylandRenderErrorThrottle.SummaryIntervalMilliseconds, out suppressed));
		Assert.AreEqual(3, suppressed);

		Assert.IsFalse(throttle.ShouldReport(WaylandRenderErrorThrottle.SummaryIntervalMilliseconds + 1, out _));
		Assert.IsTrue(throttle.ShouldReport(2 * WaylandRenderErrorThrottle.SummaryIntervalMilliseconds, out suppressed));
		Assert.AreEqual(1, suppressed);
	}
}

#if __SKIA__
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media;

[TestClass]
public class Given_CompositionTarget_RenderScheduling
{
	// Drives the deterministic scenario for the Android lock/unlock freeze:
	// the render-cycle gate flags can land in a state where future frame requests
	// become silent no-ops. NotifyRenderingResumed must un-stick that state and
	// re-issue a frame request so the cycle resumes.
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_RenderCycle_Stuck_NotifyRenderingResumed_Recovers()
	{
		var rectangle = new Rectangle
		{
			Width = 50,
			Height = 50,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.Red)
		};

		await UITestHelper.Load(rectangle);

		var compositionTarget = (CompositionTarget)rectangle.Visual.CompositionTarget!;
		var type = typeof(CompositionTarget);
		var renderRequestedField = type.GetField("_renderRequested", BindingFlags.NonPublic | BindingFlags.Instance)!;
		var renderedAheadField = type.GetField("_renderedAheadOfTime", BindingFlags.NonPublic | BindingFlags.Instance)!;
		var renderRequestedAfterAheadField = type.GetField("_renderRequestedAfterAheadOfTimePaint", BindingFlags.NonPublic | BindingFlags.Instance)!;
		var shouldEnqueueField = type.GetField("_shouldEnqueueRenderOnNextNativePlatformFrameRequested", BindingFlags.NonPublic | BindingFlags.Instance)!;

		var frameCount = 0;
		Action handler = () => Interlocked.Increment(ref frameCount);
		compositionTarget.FrameRendered += handler;

		try
		{
			// Settle the cycle so any in-flight renders complete before we tamper.
			await TestServices.WindowHelper.WaitForIdle();
			await Task.Delay(100);

			// Force the broken configuration: RequestNewFrame becomes a no-op
			// (RenderRequested already true) AND OnNativePlatformFrameRequested
			// won't enqueue a render either (gate flag false).
			renderRequestedField.SetValue(compositionTarget, true);
			renderedAheadField.SetValue(compositionTarget, false);
			renderRequestedAfterAheadField.SetValue(compositionTarget, false);
			shouldEnqueueField.SetValue(compositionTarget, false);

			Interlocked.Exchange(ref frameCount, 0);

			// Mutate the visual; in steady state this would schedule a frame.
			rectangle.Fill = new SolidColorBrush(Microsoft.UI.Colors.Green);
			await Task.Delay(300);

			Assert.AreEqual(0, Volatile.Read(ref frameCount),
				"Cycle should be stuck: forced flag state must block render scheduling.");

			// Apply the recovery path under test.
			CompositionTarget.NotifyRenderingResumed();

			// Post-condition: gate flag re-armed, ahead-of-time flags cleared.
			Assert.IsTrue((bool)shouldEnqueueField.GetValue(compositionTarget)!,
				"NotifyRenderingResumed must re-arm _shouldEnqueueRenderOnNextNativePlatformFrameRequested.");
			Assert.IsFalse((bool)renderedAheadField.GetValue(compositionTarget)!,
				"NotifyRenderingResumed must clear _renderedAheadOfTime.");
			Assert.IsFalse((bool)renderRequestedAfterAheadField.GetValue(compositionTarget)!,
				"NotifyRenderingResumed must clear _renderRequestedAfterAheadOfTimePaint.");

			// And that a real frame fires.
			rectangle.Fill = new SolidColorBrush(Microsoft.UI.Colors.Blue);
			var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
			while (Volatile.Read(ref frameCount) == 0 && DateTime.UtcNow < deadline)
			{
				await Task.Delay(50);
			}

			Assert.IsTrue(Volatile.Read(ref frameCount) > 0,
				"After NotifyRenderingResumed, the cycle should produce at least one frame.");
		}
		finally
		{
			compositionTarget.FrameRendered -= handler;
		}
	}
}
#endif

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

	// End-to-end coverage of the Android lock/unlock fix. The platform-agnostic test above
	// only invokes the static recovery primitive; this one drives the same recovery through
	// the IUnoSkiaRenderView.OnResume entry point that ApplicationActivity calls on resume,
	// so a regression in either the Vulkan _paused path or the renderview-side wiring
	// (forgetting to call NotifyRenderingResumed / RequestRender / OnResume on the base
	// GLSurfaceView) is caught here. Compiled into the generic Skia test assembly that is
	// reused on Android Skia at runtime; gated by OperatingSystem.IsAndroid() so it skips
	// cleanly on Skia Desktop where the Uno.UI.Runtime.Skia.Android types aren't present.
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_AndroidRenderView_OnResume_Recovers_Stuck_RenderCycle()
	{
		if (!OperatingSystem.IsAndroid())
		{
			Assert.Inconclusive("Android-Skia-only: requires the Uno.UI.Runtime.Skia.Android render view.");
			return;
		}

		// IUnoSkiaRenderView and ApplicationActivity.RenderView are internal to
		// Uno.UI.Runtime.Skia.Android, so reach into them via reflection.
		var activityType = Type.GetType(
			"Microsoft.UI.Xaml.ApplicationActivity, Uno.UI.Runtime.Skia.Android",
			throwOnError: false);
		Assert.IsNotNull(activityType, "Microsoft.UI.Xaml.ApplicationActivity must be loadable on Android Skia.");

		var renderViewIface = activityType!.Assembly.GetType(
			"Uno.UI.Runtime.Skia.Android.IUnoSkiaRenderView", throwOnError: false);
		Assert.IsNotNull(renderViewIface, "IUnoSkiaRenderView must be present in the Android Skia assembly.");

		var renderViewProp = activityType.GetProperty(
			"RenderView", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(renderViewProp, "ApplicationActivity must expose an internal static RenderView accessor.");

		var renderView = renderViewProp!.GetValue(null);
		Assert.IsNotNull(renderView, "RenderView must be wired by the running activity.");

		var onPauseMethod = renderViewIface!.GetMethod("OnPause");
		var onResumeMethod = renderViewIface.GetMethod("OnResume");
		Assert.IsNotNull(onPauseMethod);
		Assert.IsNotNull(onResumeMethod);

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

		var resumed = false;

		try
		{
			await TestServices.WindowHelper.WaitForIdle();
			await Task.Delay(100);

			// Park the render view as if Android had paused the activity (this exercises
			// the Vulkan _paused volatile and the GLSurfaceView base.OnPause path).
			onPauseMethod!.Invoke(renderView, null);

			// Reproduce the broken cycle state we expect to see across pause/resume.
			renderRequestedField.SetValue(compositionTarget, true);
			renderedAheadField.SetValue(compositionTarget, false);
			renderRequestedAfterAheadField.SetValue(compositionTarget, false);
			shouldEnqueueField.SetValue(compositionTarget, false);

			Interlocked.Exchange(ref frameCount, 0);

			// Drive recovery the same way ApplicationActivity does on resume.
			onResumeMethod!.Invoke(renderView, null);
			resumed = true;

			Assert.IsTrue((bool)shouldEnqueueField.GetValue(compositionTarget)!,
				"After IUnoSkiaRenderView.OnResume the render-scheduling gate must be re-armed.");
			Assert.IsFalse((bool)renderedAheadField.GetValue(compositionTarget)!,
				"After IUnoSkiaRenderView.OnResume _renderedAheadOfTime must be cleared.");
			Assert.IsFalse((bool)renderRequestedAfterAheadField.GetValue(compositionTarget)!,
				"After IUnoSkiaRenderView.OnResume _renderRequestedAfterAheadOfTimePaint must be cleared.");

			rectangle.Fill = new SolidColorBrush(Microsoft.UI.Colors.Blue);
			var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
			while (Volatile.Read(ref frameCount) == 0 && DateTime.UtcNow < deadline)
			{
				await Task.Delay(50);
			}

			Assert.IsTrue(Volatile.Read(ref frameCount) > 0,
				"After IUnoSkiaRenderView.OnResume the render cycle must produce at least one frame.");
		}
		finally
		{
			// Don't leave the live render view parked if we fail before the resume call.
			if (!resumed)
			{
				try { onResumeMethod?.Invoke(renderView, null); } catch { }
			}
			compositionTarget.FrameRendered -= handler;
		}
	}

	// Verifies the activity-side wiring: ApplicationActivity.OnPause/OnResume must
	// (a) override the base lifecycle callbacks, (b) actually forward to
	// IUnoSkiaRenderView.OnPause/OnResume on the active render view, and (c) do so
	// BEFORE delegating to base.OnPause/OnResume. Reversing the order lets the framework
	// tear down the surface or raise Application.Resuming before the render thread is
	// parked/restarted, producing the freeze and dropped-first-frame regressions this PR
	// fixed. We can't safely invoke Activity.OnPause/OnResume from a runtime test (it
	// would disrupt the running activity), so we inspect the IL of the overrides instead.
	[TestMethod]
	public void When_ApplicationActivity_Lifecycle_Forwards_To_RenderView_Before_Base()
	{
		if (!OperatingSystem.IsAndroid())
		{
			Assert.Inconclusive("Android-Skia-only: requires the Uno.UI.Runtime.Skia.Android assembly.");
			return;
		}

		var activityType = Type.GetType(
			"Microsoft.UI.Xaml.ApplicationActivity, Uno.UI.Runtime.Skia.Android",
			throwOnError: false);
		Assert.IsNotNull(activityType, "Microsoft.UI.Xaml.ApplicationActivity must be loadable on Android Skia.");

		var renderViewIface = activityType!.Assembly.GetType(
			"Uno.UI.Runtime.Skia.Android.IUnoSkiaRenderView", throwOnError: false);
		Assert.IsNotNull(renderViewIface, "IUnoSkiaRenderView must be present in the Android Skia assembly.");

		AssertForwardsBeforeBase(activityType, renderViewIface!, "OnPause");
		AssertForwardsBeforeBase(activityType, renderViewIface!, "OnResume");
	}

	private static void AssertForwardsBeforeBase(Type activityType, Type renderViewIface, string lifecycleMethodName)
	{
		var lifecycleOverride = activityType.GetMethod(
			lifecycleMethodName, BindingFlags.NonPublic | BindingFlags.Instance);
		Assert.IsNotNull(lifecycleOverride,
			$"ApplicationActivity must declare an {lifecycleMethodName} override.");
		Assert.AreEqual(activityType, lifecycleOverride!.DeclaringType,
			$"ApplicationActivity must own the {lifecycleMethodName} override (got {lifecycleOverride.DeclaringType}).");

		var forwardMethod = renderViewIface.GetMethod(lifecycleMethodName);
		Assert.IsNotNull(forwardMethod,
			$"IUnoSkiaRenderView must declare {lifecycleMethodName}.");

		// Walk up the inheritance chain to find the closest base type that declares its
		// own lifecycle method; that's the target of base.OnPause()/base.OnResume() in IL.
		var baseToken = 0;
		var baseFound = false;
		for (var t = activityType.BaseType; t != null && !baseFound; t = t.BaseType)
		{
			var candidate = t.GetMethod(
				lifecycleMethodName,
				BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
			if (candidate != null)
			{
				baseToken = candidate.MetadataToken;
				baseFound = true;
			}
		}
		Assert.IsTrue(baseFound,
			$"Could not locate the base {lifecycleMethodName} that ApplicationActivity overrides.");

		var il = lifecycleOverride.GetMethodBody()?.GetILAsByteArray();
		Assert.IsNotNull(il,
			$"IL for ApplicationActivity.{lifecycleMethodName} must be available for inspection.");

		var forwardOffset = FindCallSiteToken(il!, forwardMethod!.MetadataToken);
		var baseOffset = FindCallSiteToken(il!, baseToken);

		Assert.IsTrue(forwardOffset >= 0,
			$"ApplicationActivity.{lifecycleMethodName} must invoke IUnoSkiaRenderView.{lifecycleMethodName} on the active render view.");
		Assert.IsTrue(baseOffset >= 0,
			$"ApplicationActivity.{lifecycleMethodName} must invoke base.{lifecycleMethodName}.");
		Assert.IsTrue(forwardOffset < baseOffset,
			$"IUnoSkiaRenderView.{lifecycleMethodName} must be invoked BEFORE base.{lifecycleMethodName}: " +
			$"reversed order causes surface-teardown races on pause and dropped first-frame on resume " +
			$"(forwardOffset=0x{forwardOffset:X}, baseOffset=0x{baseOffset:X}).");
	}

	// Scans an IL body for a `call` (0x28) or `callvirt` (0x6F) instruction whose
	// 4-byte metadata token matches the target, returning the instruction offset (or -1).
	// Restricting the scan to those two opcodes makes false positives essentially nil.
	private static int FindCallSiteToken(byte[] il, int targetToken)
	{
		for (var i = 0; i + 5 <= il.Length; i++)
		{
			var op = il[i];
			if (op != 0x28 && op != 0x6F)
			{
				continue;
			}

			var token = BitConverter.ToInt32(il, i + 1);
			if (token == targetToken)
			{
				return i;
			}
		}

		return -1;
	}
}
#endif

#nullable enable

#if HAS_UNO

using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.Dispatching;
using static Microsoft.VisualStudio.TestTools.UnitTesting.ConditionMode;
using static Microsoft.VisualStudio.TestTools.UnitTesting.RuntimeTestPlatforms;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Dispatching;

/// <summary>
/// Host-level counterpart to the deterministic Uno.UI.UnitTests coverage: exercises the real dispatcher on the
/// real windowing host, which is where the starvation was observed (macOS has no frame pacer, so a render
/// action is pending on nearly every turn).
/// </summary>
[TestClass]
[RunsOnUIThread]
public class Given_NativeDispatcher
{
	private const int TimeoutMs = 5000;

	[TestMethod]
	[PlatformCondition(Include, SkiaWin32 | SkiaX11 | SkiaMacOS)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24032")]
	public async Task When_Render_Requested_Continuously_Then_Idle_Work_Runs()
	{
		var dispatcher = NativeDispatcher.Main;
		var renderTarget = new object();
		var keepRendering = true;
		var raiseRenderingScheduled = false;

		// Models an unpaced host: the next frame is requested straight away, and each frame posts
		// CompositionTarget.RaiseRendering at High priority.
		void RequestNextFrame()
		{
			if (!keepRendering)
			{
				return;
			}

			if (!raiseRenderingScheduled)
			{
				raiseRenderingScheduled = true;
				dispatcher.Enqueue(() => raiseRenderingScheduled = false, NativeDispatcherPriority.High);
			}

			dispatcher.EnqueueRender(renderTarget, RequestNextFrame);
		}

		try
		{
			dispatcher.EnqueueRender(renderTarget, RequestNextFrame);

			var idleRan = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			_ = TestServices.WindowHelper.RootElementDispatcher.RunIdleAsync(_ => idleRan.TrySetResult(true));

			var completed = await Task.WhenAny(idleRan.Task, Task.Delay(TimeoutMs));

			Assert.AreSame(
				idleRan.Task,
				completed,
				$"Idle-priority work did not run within {TimeoutMs}ms while frames were continuously requested.");
		}
		finally
		{
			keepRendering = false;
			dispatcher.RemoveCompositionTargets(target => ReferenceEquals(target, renderTarget));
		}
	}
}

#endif

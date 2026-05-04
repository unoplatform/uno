#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Benchmarks;

/// <summary>
/// Helpers used by Category C (Render) benchmarks to set up a visual scene and
/// drive frames synchronously through <see cref="CompositionTarget.Rendering"/>.
/// </summary>
internal static class RenderBenchmarkHost
{
	/// <summary>
	/// Replaces the runtime-tests window content with the supplied scene, on the dispatcher.
	/// Returns once the scene's <c>Loaded</c> event has fired.
	/// </summary>
	public static Task<UIElement> SetupSceneAsync(Func<UIElement> sceneFactory)
	{
		var tcs = new TaskCompletionSource<UIElement>();
		_ = TestServices.WindowHelper.RootElementDispatcher.RunAsync(async () =>
		{
			try
			{
				var root = sceneFactory();
				TestServices.WindowHelper.WindowContent = root;
				if (root is FrameworkElement fe)
				{
					await TestServices.WindowHelper.WaitForLoaded(fe);
				}
				tcs.SetResult(root);
			}
			catch (Exception ex)
			{
				tcs.SetException(ex);
			}
		});
		return tcs.Task;
	}

	/// <summary>
	/// Awaits exactly one compositor frame.
	/// </summary>
	public static Task RenderFrameAsync()
	{
		var tcs = new TaskCompletionSource();
		EventHandler<object>? handler = null;
		handler = (_, _) =>
		{
			CompositionTarget.Rendering -= handler;
			tcs.TrySetResult();
		};
		CompositionTarget.Rendering += handler;
		return tcs.Task;
	}

	/// <summary>
	/// Clears window content and awaits a couple of idle/frame cycles so the next benchmark starts
	/// from a stable baseline.
	/// </summary>
	public static async Task ResetSceneAsync()
	{
		await TestServices.WindowHelper.RootElementDispatcher.RunAsync(() =>
		{
			TestServices.WindowHelper.WindowContent = null;
		});
		await TestServices.WindowHelper.WaitForIdle();
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		await TestServices.WindowHelper.WaitForIdle();
	}
}

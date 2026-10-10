#if HAS_UNO
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.Dispatching;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
public class Given_ApplicationActivity
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24598")]
	[DynamicDependency("GetHostForRoot", "Uno.UI.Hosting.XamlRootMap", "Uno.UI")]
	[DynamicDependency("get_Activity", "Uno.UI.Runtime.Android.AndroidSkiaXamlRootHost", "Uno.UI.Runtime.Android")]
	[DynamicDependency("Recreate", "Android.App.Activity", "Mono.Android")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "Both assemblies only exist on Android, the only platform this test runs on.")]
	public async Task When_Recreated_Dispatcher_Stays_Responsive()
	{
		var original = GetWindowActivity();
		Assert.IsNotNull(original);
		original.GetType().GetMethod("Recreate", Type.EmptyTypes)!.Invoke(original, null);

		await TestServices.WindowHelper.WaitFor(() => GetWindowActivity() is { } current && !ReferenceEquals(current, original), timeoutMS: 10000);
		await Task.Delay(1000);
		await TestServices.WindowHelper.WaitForIdle();

		// A recreated Activity whose window keeps cancelling its draw re-schedules a traversal every frame,
		// and that traversal's sync barrier limits the dispatcher to one item per frame.
		const int ItemCount = 300;
		var stopwatch = Stopwatch.StartNew();
		for (var i = 0; i < ItemCount; i++)
		{
			NativeDispatcher.Main.Enqueue(static () => { });
		}

		await TestServices.WindowHelper.WaitForIdle();
		stopwatch.Stop();

		Assert.IsTrue(
			stopwatch.ElapsedMilliseconds < 2000,
			$"Dispatching {ItemCount} empty items took {stopwatch.ElapsedMilliseconds}ms after the Activity was recreated.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24598")]
	[DynamicDependency("GetHostForRoot", "Uno.UI.Hosting.XamlRootMap", "Uno.UI")]
	[DynamicDependency("get_Activity", "Uno.UI.Runtime.Android.AndroidSkiaXamlRootHost", "Uno.UI.Runtime.Android")]
	[DynamicDependency("Recreate", "Android.App.Activity", "Mono.Android")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "Both assemblies only exist on Android, the only platform this test runs on.")]
	public async Task When_Recreated_Window_Stays_Open()
	{
		var window = TestServices.WindowHelper.CurrentTestWindow;
		Assert.IsNotNull(window);

		var closed = false;
		void OnClosed(object sender, Microsoft.UI.Xaml.WindowEventArgs args) => closed = true;
		window.Closed += OnClosed;

		try
		{
			var original = GetWindowActivity();
			Assert.IsNotNull(original);
			original.GetType().GetMethod("Recreate", Type.EmptyTypes)!.Invoke(original, null);

			await TestServices.WindowHelper.WaitFor(() => GetWindowActivity() is { } current && !ReferenceEquals(current, original), timeoutMS: 10000);
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(closed, "The window was closed by a configuration-driven Activity recreation.");
		}
		finally
		{
			window.Closed -= OnClosed;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
#if RUNTIME_NATIVE_AOT
	[Ignore("Reads internal wrapper state through reflection, which NativeAOT trims away.")]
#endif
	[DynamicDependency("GetHostForRoot", "Uno.UI.Hosting.XamlRootMap", "Uno.UI")]
	[DynamicDependency("get_Activity", "Uno.UI.Runtime.Android.AndroidSkiaXamlRootHost", "Uno.UI.Runtime.Android")]
	[DynamicDependency("Recreate", "Android.App.Activity", "Mono.Android")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "Both assemblies only exist on Android, the only platform this test runs on.")]
	public async Task When_Recreated_Then_Successor_Drives_The_Window()
	{
		var original = GetWindowActivity();
		Assert.IsNotNull(original);
		original.GetType().GetMethod("Recreate", Type.EmptyTypes)!.Invoke(original, null);

		object successor = null;
		await TestServices.WindowHelper.WaitFor(() => (successor = GetWindowActivity()) is { } current && !ReferenceEquals(current, original), timeoutMS: 10000);

		// The successor attaches its content before the wrapper re-subscribes to the attach event;
		// a missed attach keeps the pre-draw gate shut and the app renders black.
		var wrapper = GetMember(successor, "Wrapper");
		Assert.IsNotNull(wrapper);
		await TestServices.WindowHelper.WaitFor(() => GetField(wrapper, "_awaitingFirstFrame") is 0, timeoutMS: 10000);
		Assert.AreEqual(true, GetMember(successor, "IsContentViewAttachedToWindow"));

		Assert.AreSame(successor, GetMember(wrapper, "CurrentActivity"), "The window must be handed over to the re-created activity.");

		var contextHelper = Type.GetType("Uno.UI.ContextHelper, Uno.WinRT");
		Assert.IsNotNull(contextHelper);
		Assert.AreSame(
			successor,
			contextHelper.GetProperty("Current", BindingFlags.Static | BindingFlags.Public)!.GetValue(null),
			"ContextHelper.Current must not stay on the destroyed activity.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25177")]
	[DynamicDependency("GetHostForRoot", "Uno.UI.Hosting.XamlRootMap", "Uno.UI")]
	[DynamicDependency("InvalidateRender", "Uno.UI.Runtime.Android.ApplicationActivity", "Uno.UI.Runtime.Android")]
	[DynamicDependency("get_Activity", "Uno.UI.Runtime.Android.AndroidSkiaXamlRootHost", "Uno.UI.Runtime.Android")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "Both assemblies only exist on Android, the only platform this test runs on.")]
	public async Task When_Render_Requested_From_Dispatcher_Then_Dispatcher_Is_Not_Held_Until_Next_Frame()
	{
		var activity = GetWindowActivity();
		Assert.IsNotNull(activity);
		var invalidateRender = activity.GetType().GetMethod("InvalidateRender", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		Assert.IsNotNull(invalidateRender);

		await TestServices.WindowHelper.WaitForIdle();

		// Each item asks for a frame and queues the next, as a scroll's deferred ViewChanged does. Invalidating a
		// View there posts a traversal sync barrier, which would hold every following item until the next frame.
		const int ChainLength = 60;
		var remaining = ChainLength;
		var completion = new TaskCompletionSource();
		void Step()
		{
			invalidateRender.Invoke(activity, null);
			if (--remaining == 0)
			{
				completion.SetResult();
			}
			else
			{
				NativeDispatcher.Main.Enqueue(Step);
			}
		}

		var stopwatch = Stopwatch.StartNew();
		NativeDispatcher.Main.Enqueue(Step);
		await completion.Task;
		stopwatch.Stop();

		// One item per frame would take 500ms at 120Hz, 1s at 60Hz.
		Assert.IsTrue(
			stopwatch.ElapsedMilliseconds < 250,
			$"{ChainLength} chained render requests took {stopwatch.ElapsedMilliseconds}ms.");
	}

	private static object GetMember(object instance, string name)
		=> instance.GetType()
			.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			?.GetValue(instance);

	private static object GetField(object instance, string name)
		=> instance.GetType()
			.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
			?.GetValue(instance);

	// The activity driving the test window, resolved through its host so it follows re-creation.
	// The runtime tests don't reference Mono.Android or the Android host, hence the reflection.
	private static object GetWindowActivity()
	{
		if (TestServices.WindowHelper.XamlRoot is not { } xamlRoot)
		{
			return null;
		}

		var host = typeof(Microsoft.UI.Xaml.XamlRoot).Assembly.GetType("Uno.UI.Hosting.XamlRootMap")
			?.GetMethod("GetHostForRoot", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
			?.Invoke(null, new object[] { xamlRoot });

		return host?.GetType()
			.GetProperty("Activity", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
			?.GetValue(host);
	}
}
#endif

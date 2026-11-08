#if HAS_UNO
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.Dispatching;
using Uno.UI.RuntimeTests.Helpers;

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

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
#if RUNTIME_NATIVE_AOT
	[Ignore("Reaches the render view's Vulkan context through reflection, which NativeAOT trims away.")]
#endif
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24865")]
	[DynamicDependency("GetHostForRoot", "Uno.UI.Hosting.XamlRootMap", "Uno.UI")]
	[DynamicDependency("get_Activity", "Uno.UI.Runtime.Android.AndroidSkiaXamlRootHost", "Uno.UI.Runtime.Android")]
	[DynamicDependency("get_RenderView", "Uno.UI.Runtime.Android.ApplicationActivity", "Uno.UI.Runtime.Android")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "Both assemblies only exist on Android, the only platform this test runs on.")]
	public async Task When_Vulkan_Surface_Recreated_Then_Next_Frame_Repaints_Whole()
	{
		var border = new Microsoft.UI.Xaml.Controls.Border
		{
			Width = 100,
			Height = 100,
			Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
		};
		await UITestHelper.Load(border);

		var renderView = GetMember(GetWindowActivity(), "RenderView");
		if (renderView?.GetType().Name != "UnoVulkanView")
		{
			Assert.Inconclusive($"The window renders through {renderView?.GetType().Name ?? "nothing"}, not Vulkan.");
		}

		var vulkanContext = GetField(renderView, "_vulkanContext");
		Assert.IsNotNull(vulkanContext);
		var width = (int)GetField(renderView, "_width");
		var height = (int)GetField(renderView, "_height");

		var target = (Microsoft.UI.Xaml.Media.CompositionTarget)border.Visual.CompositionTarget!;
		Microsoft.UI.Xaml.Media.FramePresentedInfo? firstPresent = null;
		var recreated = false;
		EventHandler<Microsoft.UI.Xaml.Media.FramePresentedInfo> onPresented = (_, info) =>
		{
			if (Volatile.Read(ref recreated))
			{
				firstPresent ??= info;
			}
		};

		await TestServices.WindowHelper.WaitForIdle();
		target.FramePresented += onPresented;
		try
		{
			// A rotation can resize the XAML root a frame before SurfaceChanged swaps in the new-size render image;
			// the frame after the swap then has no layout change to report, yet starts from a blank image.
			vulkanContext.GetType().GetMethod("Resize")!.Invoke(vulkanContext, new object[] { width, height });
			Volatile.Write(ref recreated, true);

			// A small change, as a tap on a button would make: only its damage region would be repainted.
			border.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Blue);

			await TestServices.WindowHelper.WaitFor(() => firstPresent is not null, timeoutMS: 5000, message: "no frame was presented after the surface was recreated");
		}
		finally
		{
			target.FramePresented -= onPresented;
		}

		Assert.IsFalse(firstPresent!.Value.Unchanged, "The first frame on the recreated render image was presented as unchanged, leaving the window blank.");
		Assert.IsFalse(firstPresent!.Value.Partial, "The first frame on the recreated render image repainted only its damage region, leaving the rest of the window blank.");
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
#if RUNTIME_NATIVE_AOT
	[Ignore("Finishes and reopens the activity through reflection, which NativeAOT trims away.")]
#endif
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26212")]
	public async Task When_Finished_And_Reopened_Then_Window_Keeps_Its_Host_And_Presents()
	{
		var border = new Microsoft.UI.Xaml.Controls.Border
		{
			Width = 100,
			Height = 100,
			Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
		};
		await UITestHelper.Load(border);
		var host = GetWindowHost();
		Assert.IsNotNull(host);

		await FinishAndReopen();

		Assert.AreSame(host, GetWindowHost(), "The window outlives its activity, so it must keep its host.");

		var target = (Microsoft.UI.Xaml.Media.CompositionTarget)border.Visual.CompositionTarget!;
		var presented = 0;
		EventHandler<Microsoft.UI.Xaml.Media.FramePresentedInfo> onPresented = (_, _) => Interlocked.Increment(ref presented);
		target.FramePresented += onPresented;
		try
		{
			border.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Blue);
			await TestServices.WindowHelper.WaitFor(() => Volatile.Read(ref presented) > 0, timeoutMS: 5000, message: "no frame was presented after the app was reopened");
		}
		finally
		{
			target.FramePresented -= onPresented;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
#if RUNTIME_NATIVE_AOT
	[Ignore("Finishes and reopens the activity through reflection, which NativeAOT trims away.")]
#endif
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26212")]
	public async Task When_Finished_And_Reopened_Then_Frame_Drivers_Tick()
	{
		var border = new Microsoft.UI.Xaml.Controls.Border { Width = 100, Height = 100 };
		await UITestHelper.Load(border);

		await FinishAndReopen();

		// What a touch fling or a ScrollView's inertia subscribes to: a dropped driver leaves them frozen.
		var target = (Microsoft.UI.Xaml.Media.CompositionTarget)border.Visual.CompositionTarget!;
		var ticks = 0;
		EventHandler<long> driver = (_, _) => ticks++;
		target.FrameStarting += driver;
		try
		{
			await TestServices.WindowHelper.WaitFor(() => ticks > 1, timeoutMS: 5000, message: "a frame driver subscribed after the app was reopened never ticked");
		}
		finally
		{
			target.FrameStarting -= driver;
		}
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaAndroid)]
#if RUNTIME_NATIVE_AOT
	[Ignore("Finishes and reopens the activity through reflection, which NativeAOT trims away.")]
#endif
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26212")]
	public async Task When_Finished_And_Reopened_Then_Running_Animation_Continues()
	{
		var border = new Microsoft.UI.Xaml.Controls.Border
		{
			Width = 100,
			Height = 100,
			Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Red),
		};
		await UITestHelper.Load(border);

		var visual = border.Visual;
		var animation = visual.Compositor.CreateScalarKeyFrameAnimation();
		animation.InsertKeyFrame(0, 1);
		animation.InsertKeyFrame(1, 0.1f);
		animation.Duration = TimeSpan.FromSeconds(1);
		animation.IterationBehavior = Microsoft.UI.Composition.AnimationIterationBehavior.Forever;
		visual.StartAnimation(nameof(Microsoft.UI.Composition.Visual.Opacity), animation);
		try
		{
			await FinishAndReopen();

			var opacity = visual.Opacity;
			await TestServices.WindowHelper.WaitFor(() => visual.Opacity != opacity, timeoutMS: 5000, message: "an animation running when the activity finished did not continue after the app was reopened");
		}
		finally
		{
			visual.StopAnimation(nameof(Microsoft.UI.Composition.Visual.Opacity));
		}
	}

	/// <summary>
	/// Finishes the activity while the process and the window live on, as Back does on any Android below 12 and on a
	/// root activity that isn't the launcher one, then opens the app again.
	/// </summary>
	[DynamicDependency("GetHostForRoot", "Uno.UI.Hosting.XamlRootMap", "Uno.UI")]
	[DynamicDependency("get_Activity", "Uno.UI.Runtime.Android.AndroidSkiaXamlRootHost", "Uno.UI.Runtime.Android")]
	[DynamicDependency("get_Wrapper", "Uno.UI.Runtime.Android.ApplicationActivity", "Uno.UI.Runtime.Android")]
	[DynamicDependency("get_CurrentActivity", "Uno.UI.Xaml.Controls.NativeWindowWrapper", "Uno.UI.Runtime.Android")]
	[DynamicDependency("Finish", "Android.App.Activity", "Mono.Android")]
	[DynamicDependency("get_IsDestroyed", "Android.App.Activity", "Mono.Android")]
	[DynamicDependency("get_ApplicationContext", "Android.Content.ContextWrapper", "Mono.Android")]
	[DynamicDependency("get_PackageManager", "Android.Content.ContextWrapper", "Mono.Android")]
	[DynamicDependency("get_PackageName", "Android.Content.ContextWrapper", "Mono.Android")]
	[DynamicDependency("StartActivity(Android.Content.Intent)", "Android.Content.ContextWrapper", "Mono.Android")]
	[DynamicDependency("GetLaunchIntentForPackage", "Android.Content.PM.PackageManager", "Mono.Android")]
	[UnconditionalSuppressMessage("Trimming", "IL2035", Justification = "These assemblies only exist on Android, the only platform this helper runs on.")]
	private static async Task FinishAndReopen()
	{
		var original = GetWindowActivity();
		Assert.IsNotNull(original);
		var wrapper = GetMember(original, "Wrapper");
		var context = GetMember(original, "ApplicationContext");
		var packageManager = GetMember(original, "PackageManager");
		var launchIntent = packageManager.GetType()
			.GetMethod("GetLaunchIntentForPackage", new[] { typeof(string) })!
			.Invoke(packageManager, new[] { GetMember(original, "PackageName") });
		Assert.IsNotNull(launchIntent);

		original.GetType().GetMethod("Finish", Type.EmptyTypes)!.Invoke(original, null);

		// A successor started before this completes would take the window over instead of finding it without an activity.
		await TestServices.WindowHelper.WaitFor(() => GetMember(original, "IsDestroyed") is true, timeoutMS: 15000, message: "the activity should be destroyed");

		context.GetType().GetMethod("StartActivity", new[] { launchIntent.GetType() })!.Invoke(context, new[] { launchIntent });

		await TestServices.WindowHelper.WaitFor(() => !ReferenceEquals(GetMember(wrapper, "CurrentActivity"), original), timeoutMS: 15000, message: "the app should reopen");
		await TestServices.WindowHelper.WaitFor(() => GetField(wrapper, "_awaitingFirstFrame") is 0, timeoutMS: 15000, message: "the reopened window should present its first frame");
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
		var host = GetWindowHost();

		return host?.GetType()
			.GetProperty("Activity", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
			?.GetValue(host);
	}

	private static object GetWindowHost()
		=> TestServices.WindowHelper.XamlRoot is { } xamlRoot
			? typeof(Microsoft.UI.Xaml.XamlRoot).Assembly.GetType("Uno.UI.Hosting.XamlRootMap")
				?.GetMethod("GetHostForRoot", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
				?.Invoke(null, new object[] { xamlRoot })
			: null;
}
#endif

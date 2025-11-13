using System;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Input.Preview.Injection;
using Uno.UI.Toolkit.DevTools.Input;

#if HAS_UNO_WINUI || WINAPPSDK
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif
namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public partial class Given_InteractionTracker
{
	private static InteractionTracker SetupTracker(Compositor compositor)
	{
		var tracker = InteractionTracker.CreateWithOwner(compositor, new TrackerOwner());

		tracker.MinPosition = new(-100);
		tracker.MaxPosition = new(100);

		return tracker;
	}

	private async Task<string> WaitTrackerLogs(InteractionTracker tracker)
		=> await WaitTrackerLogs((TrackerOwner)tracker.Owner);

	private async Task<string> WaitTrackerLogs(TrackerOwner owner)
	{
		string logs = owner.GetLogs();
		while (true)
		{
			await Task.Delay(100);
			var currentLogs = owner.GetLogs();
			if (logs == currentLogs)
			{
				return logs;
			}

			logs = currentLogs;
		}
	}

	[TestMethod]
#if !HAS_COMPOSITION_API
	[Ignore("Composition APIs are not supported on this platform.")]
#endif
	public async Task When_TryUpdatePositionWithAdditionalVelocity_SingleCall()
	{
		var border = new Border()
		{
			Width = 50,
			Height = 50,
		};

		await UITestHelper.Load(border);

		var visual = ElementCompositionPreview.GetElementVisual(border);
		var tracker = SetupTracker(visual.Compositor);
		Assert.AreEqual(Vector3.Zero, tracker.Position);
		tracker.TryUpdatePositionWithAdditionalVelocity(new Vector3(0, 200, 0));

		string logs = await WaitTrackerLogs(tracker);
		var helper = new TrackerAssertHelper(logs);

		var finalPosition =
#if HAS_UNO
			56.7474f
#else
			56.747395f
#endif
			;

		Assert.AreEqual(
			TrackerLogsConstructingHelper.GetInertiaStateEntered(
				trackerPosition: Vector3.Zero,
				requestId: 1,
				naturalRestingPosition: new(0.0f, finalPosition, 0.0f),
				modifiedRestingPosition: new(0.0f, finalPosition, 0.0f),
				positionVelocityInPixelsPerSecond: new(0.0f, 200.0f, 0.0f)),
			helper.Current);

		helper.Advance();
		var linesSkipped = helper.SkipLines(current => current.StartsWith("ValuesChanged:", StringComparison.Ordinal));
		Assert.IsTrue(linesSkipped >= 2);
		helper.Back();

		Assert.AreEqual(
			TrackerLogsConstructingHelper.GetValuesChanged(
				trackerPosition: new(0.0f, finalPosition, 0.0f),
				requestId: 1,
				argsPosition: new(0.0f, finalPosition, 0.0f)),
			helper.Current);

		helper.Advance();

		Assert.AreEqual(
			TrackerLogsConstructingHelper.GetIdleStateEntered(
				trackerPosition: new(0.0f, finalPosition, 0.0f),
				requestId: 1),
			helper.Current);

		helper.Advance();

		Assert.IsTrue(helper.IsDone);
	}

	[TestMethod]
#if !HAS_COMPOSITION_API
	[Ignore("Composition APIs are not supported on this platform.")]
#endif
	public async Task When_TryUpdatePositionWithAdditionalVelocity_TwoCalls()
	{
		var border = new Border()
		{
			Width = 50,
			Height = 50,
		};

		await UITestHelper.Load(border);

		var visual = ElementCompositionPreview.GetElementVisual(border);
		var tracker = SetupTracker(visual.Compositor);
		Assert.AreEqual(Vector3.Zero, tracker.Position);
		tracker.TryUpdatePositionWithAdditionalVelocity(new Vector3(0, 100, 0));
		tracker.TryUpdatePositionWithAdditionalVelocity(new Vector3(0, 100, 0));

		string logs = await WaitTrackerLogs(tracker);
		var helper = new TrackerAssertHelper(logs);

		var finalPosition =
#if HAS_UNO
			56.7474f
#else
			56.747395f
#endif
			;

		Assert.AreEqual(
			TrackerLogsConstructingHelper.GetInertiaStateEntered(
				trackerPosition: Vector3.Zero,
				requestId: 2,
				naturalRestingPosition: new(0.0f, finalPosition, 0.0f),
				modifiedRestingPosition: new(0.0f, finalPosition, 0.0f),
				positionVelocityInPixelsPerSecond: new(0.0f, 200.0f, 0.0f)),
			helper.Current);

		helper.Advance();
		var linesSkipped = helper.SkipLines(current => current.StartsWith("ValuesChanged:", StringComparison.Ordinal));
		Assert.IsTrue(linesSkipped >= 2);
		helper.Back();

		Assert.AreEqual(
			TrackerLogsConstructingHelper.GetValuesChanged(
				trackerPosition: new(0.0f, finalPosition, 0.0f),
				requestId: 2,
				argsPosition: new(0.0f, finalPosition, 0.0f)),
			helper.Current);

		helper.Advance();

		Assert.AreEqual(
			TrackerLogsConstructingHelper.GetIdleStateEntered(
				trackerPosition: new(0.0f, finalPosition, 0.0f),
				requestId: 2),
			helper.Current);

		helper.Advance();

		Assert.IsTrue(helper.IsDone);
	}

	[RequiresFullWindow]
#if !HAS_COMPOSITION_API
	[Ignore("Composition APIs are not supported on this platform.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#elif !HAS_UNO
	[Ignore("Test fails on Windows. For some reason, Drag isn't doing what we expect it to for an unknown reason.")]
#endif
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaWasm | RuntimeTestPlatforms.SkiaUIKit)]
	public async Task When_UserInteraction()
	{
		var border = new Border()
		{
			Width = 200,
			Height = 200,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
		};

		var captureLostRaised = false;
		border.PointerCaptureLost += (_, _) => captureLostRaised = true;

		var position = await UITestHelper.Load(border);

		var visual = ElementCompositionPreview.GetElementVisual(border);
		var tracker = SetupTracker(visual.Compositor);

		Assert.AreEqual(Vector3.Zero, tracker.Position);

		var vis = VisualInteractionSource.Create(visual);
		vis.PositionXSourceMode = InteractionSourceMode.EnabledWithInertia;
		vis.PositionYSourceMode = InteractionSourceMode.EnabledWithInertia;
		tracker.InteractionSources.Add(vis);

		border.PointerPressed += async (_, e) =>
		{
			if (e.Pointer.PointerDeviceType == PointerDeviceType.Touch)
				vis.TryRedirectForManipulation(e.GetCurrentPoint(null));
			await TestServices.WindowHelper.WaitForIdle();
		};

		await TestServices.WindowHelper.WaitForIdle();

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var finger = injector.GetFinger();
		finger.Drag(new(position.Left + 50, position.Top + 50), new(position.Left + 100, position.Top + 50), stepOffsetInMilliseconds: 0);

		string logs = await WaitTrackerLogs(tracker);
		var helper = new TrackerAssertHelper(logs);

		Assert.AreEqual(
			TrackerLogsConstructingHelper.GetInteractingStateEntered(
				trackerPosition: new(-50.0f, 0.0f, 0.0f),
				requestId: 0),
			helper.Current);

		helper.Advance();

		var linesSkipped = helper.SkipLines(current => current.StartsWith("ValuesChanged:", StringComparison.Ordinal));
		Assert.IsTrue(linesSkipped >= 2);
		helper.Back();

		Assert.AreEqual(
			TrackerLogsConstructingHelper.GetValuesChanged(
				trackerPosition: new(-50.0f, 0.0f, 0.0f),
				requestId: 0,
				argsPosition: new(-50.0f, 0.0f, 0.0f)),
			helper.Current);

		helper.Advance();

		Assert.AreEqual(
			TrackerLogsConstructingHelper.GetIdleStateEntered(
				trackerPosition: new(-50.0f, 0.0f, 0.0f),
				requestId: 0),
			helper.Current);

		helper.Advance();
		Assert.IsTrue(captureLostRaised);
		Assert.IsTrue(helper.IsDone);
	}

#if HAS_UNO
	[TestMethod]
	[RequiresFullWindow]
#if !HAS_COMPOSITION_API
	[Ignore("Composition APIs are not supported on this platform.")]
#elif !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_MouseWheel()
	{
		var border = new Border()
		{
			Width = 200,
			Height = 200,
			Background = new SolidColorBrush(Microsoft.UI.Colors.Red),
		};

		var position = await UITestHelper.Load(border);

		var visual = ElementCompositionPreview.GetElementVisual(border);
		var tracker = SetupTracker(visual.Compositor);

		Assert.AreEqual(Vector3.Zero, tracker.Position);

		var vis = VisualInteractionSource.Create(visual);
		vis.ManipulationRedirectionMode = VisualInteractionSourceRedirectionMode.PointerWheelOnly;
		vis.PositionXSourceMode = InteractionSourceMode.EnabledWithInertia;
		vis.PositionYSourceMode = InteractionSourceMode.EnabledWithInertia;
		tracker.InteractionSources.Add(vis);

		await TestServices.WindowHelper.WaitForIdle();

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		var finger = injector.GetMouse();
		finger.MoveTo(new(position.Left + 100, position.Top + 100), steps: 1);
		finger.WheelDown();

		string logs = await WaitTrackerLogs(tracker);
		var helper = new TrackerAssertHelper(logs);

		Assert.AreEqual(
			TrackerLogsConstructingHelper.GetInertiaStateEntered(
				trackerPosition: new(0.0f, 0.0f, 0.0f),
				requestId: 0,
				naturalRestingPosition: new(0.0f, 48.0f, 0.0f),
				modifiedRestingPosition: new(0.0f, 48.0f, 0.0f),
				positionVelocityInPixelsPerSecond: new(0.0f, 192.0f, 0.0f)),
			helper.Current);

		helper.Advance();

		var linesSkipped = helper.SkipLines(current => current.StartsWith("ValuesChanged:", StringComparison.Ordinal));
		Assert.IsTrue(linesSkipped >= 2);
		helper.Back();

		Assert.AreEqual(
			TrackerLogsConstructingHelper.GetValuesChanged(
				trackerPosition: new(0.0f, 48.0f, 0.0f),
				requestId: 0,
				argsPosition: new(0.0f, 48.0f, 0.0f)),
			helper.Current);

		helper.Advance();

		Assert.AreEqual(
			TrackerLogsConstructingHelper.GetIdleStateEntered(
				trackerPosition: new(0.0f, 48.0f, 0.0f),
				requestId: 0),
			helper.Current);

		helper.Advance();
		Assert.IsTrue(helper.IsDone);
	}
#endif
}

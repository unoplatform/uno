using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
internal partial class Given_InteractionTracker
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
	public async Task When_TryUpdatePositionWithAdditionalVelocity()
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
}

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_WebView2_ActivityLaunch
{
	[TestMethod]
	public void When_Cancelled_Before_Launch_No_Activity_Is_Started()
	{
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var launch = new WebViewActivityLaunch<TestActivity>(cancellation.Token);
		var starts = 0;

		Assert.ThrowsExactly<OperationCanceledException>(() => launch.StartAsync(_ => starts++));
		Assert.AreEqual(0, starts);
	}

	[TestMethod]
	public async Task When_Cancelled_During_Start_The_Arriving_Activity_Is_Finished_Before_Releasing_Ownership()
	{
		using var cancellation = new CancellationTokenSource();
		var launch = new WebViewActivityLaunch<TestActivity>(cancellation.Token);
		string? startedId = null;
		var waiting = launch.StartAsync(id => startedId = id);
		cancellation.Cancel();

		Assert.IsFalse(waiting.IsCompleted, "An accepted launch must remain tracked until its activity arrives.");
		var unrelated = new TestActivity();
		launch.OnActivityCreated(unrelated, "another-launch", static activity => activity.Finish());
		Assert.IsFalse(waiting.IsCompleted);
		Assert.AreEqual(0, unrelated.FinishCount);

		var arrived = new TestActivity();
		launch.OnActivityCreated(arrived, startedId, activity =>
		{
			Assert.IsFalse(waiting.IsCompleted, "Finish must run before the launch waiter releases its subscription.");
			activity.Finish();
		});
		await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => waiting);
		Assert.AreEqual(1, arrived.FinishCount);

		launch.OnActivityCreated(arrived, startedId, static activity => activity.Finish());
		Assert.AreEqual(1, arrived.FinishCount);
	}

	[TestMethod]
	public async Task When_Launch_Completes_The_Activity_Is_Handed_To_The_Result_Owner()
	{
		var launch = new WebViewActivityLaunch<TestActivity>(CancellationToken.None);
		var waiting = launch.StartAsync(_ => { });
		var arrived = new TestActivity();
		launch.OnActivityCreated(arrived, launch.Id, static activity => activity.Finish());

		Assert.AreSame(arrived, await waiting);
		Assert.AreEqual(0, arrived.FinishCount);
	}

	[TestMethod]
	public async Task When_Another_Launch_Arrives_It_Is_Not_Claimed_By_The_Cancelled_Owner()
	{
		using var cancellation = new CancellationTokenSource();
		var cancelled = new WebViewActivityLaunch<TestActivity>(cancellation.Token);
		var active = new WebViewActivityLaunch<TestActivity>(CancellationToken.None);
		var cancelledWait = cancelled.StartAsync(_ => { });
		var activeWait = active.StartAsync(_ => { });
		cancellation.Cancel();
		var activeActivity = new TestActivity();

		cancelled.OnActivityCreated(activeActivity, active.Id, static activity => activity.Finish());
		active.OnActivityCreated(activeActivity, active.Id, static activity => activity.Finish());

		Assert.AreSame(activeActivity, await activeWait);
		Assert.AreEqual(0, activeActivity.FinishCount);
		Assert.IsFalse(cancelledWait.IsCompleted);

		var cancelledActivity = new TestActivity();
		cancelled.OnActivityCreated(cancelledActivity, cancelled.Id, static activity => activity.Finish());
		await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => cancelledWait);
		Assert.AreEqual(1, cancelledActivity.FinishCount);
	}

	[TestMethod]
	public void When_Launch_Fails_The_Error_Is_Not_Hidden()
	{
		var launch = new WebViewActivityLaunch<TestActivity>(CancellationToken.None);
		Assert.ThrowsExactly<InvalidOperationException>(() => launch.StartAsync(_ => throw new InvalidOperationException("launch failed")));
	}

	private sealed class TestActivity
	{
		internal int FinishCount { get; private set; }
		internal void Finish() => FinishCount++;
	}
}

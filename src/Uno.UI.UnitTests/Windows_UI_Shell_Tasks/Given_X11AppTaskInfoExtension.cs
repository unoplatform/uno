#nullable enable
#pragma warning disable CS8305

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.WinUI.Runtime.Skia.X11;
using Windows.UI.Shell.Tasks;

namespace Uno.UI.Tests.Windows_UI_Shell_Tasks;

[TestClass]
public class Given_X11AppTaskInfoExtension
{
	[TestMethod]
	public void When_Notification_Service_Is_Absent_Then_Support_Is_False()
	{
		using var extension = new X11AppTaskInfoExtension(new TestNotificationService(), TimeSpan.Zero);
		Assert.IsFalse(extension.IsSupported());
	}

	[TestMethod]
	public async Task When_Probe_Is_Pending_Then_Support_Is_Not_Claimed()
	{
		var probe = new TaskCompletionSource<AppTaskNotificationSupport>(TaskCreationOptions.RunContinuationsAsynchronously);
		var service = new TestNotificationService { Owner = ":1.1", PendingProbe = probe.Task };
		using var extension = new X11AppTaskInfoExtension(service, TimeSpan.Zero);
		Assert.IsFalse(extension.IsSupported());

		probe.SetResult(new(true, ":1.1"));
		for (var attempt = 0; attempt < 100 && !extension.IsSupported(); attempt++)
		{
			await Task.Delay(10);
		}

		Assert.IsTrue(extension.IsSupported());
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public Task When_Probe_Fails_Then_It_Can_Be_Retried(bool asynchronously) =>
		AssertProbeFailure(new IOException("Notification service is unavailable."), asynchronously, isRecoverable: true);

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public Task When_Probe_Has_Unrecoverable_Error_Then_It_Reaches_The_Calling_Context(bool asynchronously) =>
		AssertProbeFailure(new InvalidProgramException("Fatal probe failure."), asynchronously, isRecoverable: false);

	[TestMethod]
	public void When_Disposed_Then_Service_Is_Disposed_Only_Once()
	{
		var service = new TestNotificationService { Owner = ":1.1" };
		using var extension = new X11AppTaskInfoExtension(service, TimeSpan.FromMinutes(1));
		Assert.IsTrue(extension.IsSupported());

		extension.Dispose();
		extension.Dispose();

		Assert.IsFalse(extension.IsSupported());
		Assert.AreEqual(1, service.DisposeCount);
	}

	[TestMethod]
	public void When_Disposed_Then_Probe_Timer_Does_Not_Keep_Extension_Alive()
	{
		var extension = CreateDisposedExtension();

		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();

		Assert.IsFalse(extension.IsAlive);
	}

	[TestMethod]
	public async Task When_Disposed_While_Probing_Then_Recovery_Is_Not_Published()
	{
		var service = new TestNotificationService { Owner = ":1.1" };
		using var extension = new X11AppTaskInfoExtension(service, TimeSpan.Zero);
		extension.Synchronize(1, [CreateSnapshot()]);

		var probe = new TaskCompletionSource<AppTaskNotificationSupport>(TaskCreationOptions.RunContinuationsAsynchronously);
		service.PendingProbe = probe.Task;
		var context = CaptureProbe(extension);

		extension.Dispose();
		probe.SetResult(new(true, ":1.2"));
		await context.Completion.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.IsNull(context.Error);
		Assert.IsFalse(extension.IsSupported());
		Assert.AreEqual(1, service.Notifications.Count);
		Assert.AreEqual(1, service.DisposeCount);
	}

	[TestMethod]
	public void When_Service_Can_Be_Activated_Then_It_Can_Publish()
	{
		var service = new TestNotificationService { CanActivate = true };
		using var extension = new X11AppTaskInfoExtension(service, TimeSpan.Zero);
		Assert.IsTrue(extension.IsSupported());

		extension.Synchronize(1, [CreateSnapshot()]);

		Assert.AreEqual(":activated", service.Owner);
		Assert.AreEqual(1, service.Notifications.Count);
		Assert.AreEqual(0U, service.Notifications[0].ReplacesId);
	}

	[TestMethod]
	public void When_Payload_Is_Unchanged_Then_Notification_Is_Not_Replaced()
	{
		var service = new TestNotificationService { Owner = ":1.1" };
		using var extension = new X11AppTaskInfoExtension(service, TimeSpan.Zero);
		var task = CreateSnapshot();
		extension.Synchronize(1, [task]);
		extension.Synchronize(2, [task]);

		Assert.AreEqual(1, service.Notifications.Count);
	}

	[TestMethod]
	public void When_Daemon_Owner_Changes_Then_Current_Tasks_Are_Republished_With_Fresh_Ids()
	{
		var service = new TestNotificationService { Owner = ":1.1" };
		using var extension = new X11AppTaskInfoExtension(service, TimeSpan.Zero);
		extension.Synchronize(1, [CreateSnapshot()]);

		service.Owner = ":1.2";
		Assert.IsTrue(extension.IsSupported());

		Assert.AreEqual(2, service.Notifications.Count, "Recovery must not require a new application revision.");
		Assert.AreEqual(":1.1", service.Notifications[0].Owner);
		Assert.AreEqual(":1.2", service.Notifications[1].Owner);
		Assert.AreEqual(0U, service.Notifications[1].ReplacesId, "A notification ID is scoped to its original daemon.");
	}

	[TestMethod]
	public async Task When_Monitor_Detects_Restart_Then_No_Application_Call_Is_Required()
	{
		var service = new TestNotificationService { Owner = ":1.1" };
		using var extension = new X11AppTaskInfoExtension(service, TimeSpan.FromMilliseconds(20));
		extension.Synchronize(1, [CreateSnapshot()]);

		service.Owner = ":1.2";
		var owner = await service.Republished.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Assert.AreEqual(":1.2", owner);
		Assert.AreEqual(0U, service.Notifications[1].ReplacesId);
	}

	[TestMethod]
	public void When_Service_Disappears_And_Returns_Then_Last_Tasks_Are_Replayed()
	{
		var service = new TestNotificationService { Owner = ":1.1" };
		using var extension = new X11AppTaskInfoExtension(service, TimeSpan.Zero);
		extension.Synchronize(1, [CreateSnapshot()]);

		service.Owner = null;
		Assert.IsFalse(extension.IsSupported());
		Assert.AreEqual(1, service.Notifications.Count);

		service.Owner = ":1.2";
		Assert.IsTrue(extension.IsSupported());
		Assert.AreEqual(2, service.Notifications.Count);
		Assert.AreEqual(0U, service.Notifications[1].ReplacesId);
	}

	[TestMethod]
	public void When_Publish_Fails_Then_Recovery_Replays_The_Failed_Revision()
	{
		var service = new TestNotificationService { Owner = ":1.1", FailNextNotification = true };
		using var extension = new X11AppTaskInfoExtension(service, TimeSpan.Zero);
		extension.Synchronize(1, [CreateSnapshot()]);
		Assert.AreEqual(1, service.ResetCount);

		Assert.IsTrue(extension.IsSupported());
		Assert.AreEqual(2, service.Notifications.Count);
		Assert.AreEqual(0U, service.Notifications[1].ReplacesId);
	}

	[TestMethod]
	public void When_Task_Is_Removed_Then_Only_Its_Owner_Scoped_Notification_Is_Closed()
	{
		var service = new TestNotificationService { Owner = ":1.1" };
		using var extension = new X11AppTaskInfoExtension(service, TimeSpan.Zero);
		extension.Synchronize(1, [CreateSnapshot()]);
		extension.Synchronize(2, []);

		Assert.AreEqual(1, service.Closed.Count);
		Assert.AreEqual((":1.1", 1U), service.Closed[0]);
	}

	private static async Task AssertProbeFailure(Exception error, bool asynchronously, bool isRecoverable)
	{
		var service = new TestNotificationService();
		using var extension = new X11AppTaskInfoExtension(service, TimeSpan.Zero);
		var probe = new TaskCompletionSource<AppTaskNotificationSupport>(TaskCreationOptions.RunContinuationsAsynchronously);
		service.PendingProbe = probe.Task;
		if (!asynchronously)
		{
			probe.SetException(error);
		}

		var context = CaptureProbe(extension);
		if (asynchronously)
		{
			probe.SetException(error);
		}

		await context.Completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
		if (isRecoverable)
		{
			Assert.IsNull(context.Error);
			Assert.IsFalse(extension.IsSupported());

			service.PendingProbe = null;
			service.Owner = ":1.1";
			Assert.IsTrue(extension.IsSupported());
		}
		else
		{
			Assert.AreSame(error, context.Error);
		}
	}

	private static ExceptionCaptureContext CaptureProbe(X11AppTaskInfoExtension extension)
	{
		var context = new ExceptionCaptureContext();
		var previousContext = SynchronizationContext.Current;
		try
		{
			SynchronizationContext.SetSynchronizationContext(context);
			extension.IsSupported();
		}
		finally
		{
			SynchronizationContext.SetSynchronizationContext(previousContext);
		}

		return context;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateDisposedExtension()
	{
		using var extension = new X11AppTaskInfoExtension(new TestNotificationService(), TimeSpan.FromMinutes(1));
		return new WeakReference(extension);
	}

	private static AppTaskInfoSnapshot CreateSnapshot() => new(
		Guid.NewGuid().ToString("B"),
		"Task",
		string.Empty,
		new Uri("sample-app://tasks/test"),
		new Uri("ms-appx:///Assets/StoreLogo.png"),
		AppTaskState.Running,
		DateTimeOffset.UtcNow,
		null,
		HiddenByUser: false,
		AppTaskContent.CreateTextSummaryResult("Summary").CreateSnapshot());

	private sealed class TestNotificationService : IAppTaskNotificationService
	{
		private uint _nextId;
		internal string? Owner { get; set; }
		internal bool CanActivate { get; set; }
		internal bool FailNextNotification { get; set; }
		internal Task<AppTaskNotificationSupport>? PendingProbe { get; set; }
		internal int ResetCount { get; private set; }
		internal int DisposeCount { get; private set; }
		internal List<(string Owner, uint ReplacesId)> Notifications { get; } = new();
		internal List<(string Owner, uint Id)> Closed { get; } = new();
		internal TaskCompletionSource<string> Republished { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public Task<AppTaskNotificationSupport> ProbeAsync()
			=> PendingProbe ?? Task.FromResult(new AppTaskNotificationSupport(Owner is not null || CanActivate, Owner));

		public Task<string> GetOwnerAsync()
		{
			if (Owner is null && CanActivate)
			{
				Owner = ":activated";
			}

			return Owner is not null
				? Task.FromResult(Owner)
				: Task.FromException<string>(new IOException("Notification daemon is unavailable."));
		}

		public Task<uint> NotifyAsync(string owner, uint replacesId, string icon, string summary, string body)
		{
			Notifications.Add((owner, replacesId));
			if (Notifications.Count == 2)
			{
				Republished.TrySetResult(owner);
			}
			if (FailNextNotification)
			{
				FailNextNotification = false;
				return Task.FromException<uint>(new IOException("Notification connection was lost."));
			}

			return Task.FromResult(++_nextId);
		}

		public Task CloseAsync(string owner, uint notificationId)
		{
			Closed.Add((owner, notificationId));
			return Task.CompletedTask;
		}

		public void Reset() => ResetCount++;

		public void Dispose() => DisposeCount++;
	}

	private sealed class ExceptionCaptureContext : SynchronizationContext
	{
		internal TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		internal Exception? Error { get; private set; }

		public override void Post(SendOrPostCallback callback, object? state)
		{
			try
			{
				callback(state);
			}
			catch (Exception error)
			{
				Error = error;
			}
		}

		public override void OperationCompleted() => Completion.TrySetResult();
	}
}

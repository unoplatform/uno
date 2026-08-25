#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Foundation.Extensibility;
using Windows.ApplicationModel.Background;

namespace Uno.UI.Tests.Windows_ApplicationModel.Background;

[TestClass]
public class Given_BackgroundTask
{
	private static readonly TestScheduler Scheduler = new();
	private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(30);

	[ClassInitialize]
	public static void InitializeClass(TestContext _)
		=> ApiExtensibility.Register(
			typeof(IBackgroundTaskSchedulerExtension),
			_ => Scheduler);

	[TestInitialize]
	public void Initialize()
	{
		Scheduler.Reset();
		BackgroundTaskRegistrationStore.WriteStore(
			BackgroundTaskRegistrationStore.StorePath,
			[]);
		DeferredTask.RunCount = 0;
		CleanupEvents();
	}

	[TestCleanup]
	public void Cleanup()
	{
		BackgroundTaskRegistrationStore.WriteStore(
			BackgroundTaskRegistrationStore.StorePath,
			[]);
		CleanupEvents();
	}

	private static void CleanupEvents()
	{
		if (Directory.Exists(BackgroundTaskRegistrationStore.EventsDirectory))
		{
			foreach (var path in Directory.EnumerateFiles(
				BackgroundTaskRegistrationStore.EventsDirectory,
				"*.event"))
			{
				File.Delete(path);
			}
		}
	}

	[TestMethod]
	public void When_CancelledTaskLeaksDeferral_Then_RunIsBounded()
	{
		var builder = new BackgroundTaskBuilder
		{
			Name = "Cancellation test",
			TaskEntryPoint = typeof(CancellationIgnoringTask).FullName!
		};
		builder.SetTrigger(new TimeTrigger(15, oneShot: false));
		var registration = builder.Register();
		var started = DateTimeOffset.UtcNow;

		var exitCode = BackgroundTaskRunner.Run(registration.TaskId);

		Assert.AreEqual(6, exitCode);
		Assert.IsTrue(
			DateTimeOffset.UtcNow - started < TimeSpan.FromSeconds(10));
		registration.Unregister(cancelTask: true);
	}

	[TestMethod]
	public void When_TimeTrigger_IsBelowMinimum_Then_RegistrationThrows()
	{
		var builder = new BackgroundTaskBuilder
		{
			Name = "Invalid interval",
			TaskEntryPoint = typeof(DeferredTask).FullName!
		};
		builder.SetTrigger(new TimeTrigger(14, oneShot: false));

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(
			() => builder.Register());
	}

	[TestMethod]
	public void When_TriggerIsMissing_Then_RegistrationThrows()
	{
		var builder = new BackgroundTaskBuilder
		{
			Name = "No trigger",
			TaskEntryPoint = typeof(DeferredTask).FullName!
		};

		Assert.ThrowsExactly<InvalidOperationException>(() => builder.Register());
	}

	[TestMethod]
	public void When_TaskCompletes_Then_CompletedIsRaised()
	{
		var registration = RegisterTask(oneShot: false);
		using var completed = new ManualResetEventSlim();
		BackgroundTaskCompletedEventArgs? completedArgs = null;
		BackgroundTaskCompletedEventHandler handler = (_, args) =>
		{
			completedArgs = args;
			completed.Set();
		};
		registration.Completed += handler;

		try
		{
			Assert.AreEqual(0, BackgroundTaskRunner.Run(registration.TaskId));

			Assert.IsTrue(
				completed.Wait(EventTimeout),
				"Completed was not raised. " + DescribeEvents(registration.TaskId));
			Assert.IsNotNull(completedArgs);
			completedArgs.CheckResult();
		}
		finally
		{
			registration.Completed -= handler;
			registration.Unregister(cancelTask: true);
		}
	}

	[TestMethod]
	public void When_TaskFails_Then_CompletedReportsTheError()
	{
		var builder = new BackgroundTaskBuilder
		{
			Name = "Failing task",
			TaskEntryPoint = typeof(FailingTask).FullName!
		};
		builder.SetTrigger(new TimeTrigger(15, oneShot: false));
		var registration = builder.Register();
		using var completed = new ManualResetEventSlim();
		BackgroundTaskCompletedEventArgs? completedArgs = null;
		BackgroundTaskCompletedEventHandler handler = (_, args) =>
		{
			completedArgs = args;
			completed.Set();
		};
		registration.Completed += handler;

		try
		{
			Assert.AreEqual(1, BackgroundTaskRunner.Run(registration.TaskId));

			Assert.IsTrue(
				completed.Wait(EventTimeout),
				"Completed was not raised. " + DescribeEvents(registration.TaskId));
			Assert.IsNotNull(completedArgs);
			Assert.ThrowsExactly<InvalidOperationException>(completedArgs.CheckResult);
		}
		finally
		{
			registration.Completed -= handler;
			registration.Unregister(cancelTask: true);
		}
	}

	[TestMethod]
	public void When_TaskReportsProgress_Then_ProgressIsRaised()
	{
		var builder = new BackgroundTaskBuilder
		{
			Name = "Progress task",
			TaskEntryPoint = typeof(ProgressReportingTask).FullName!
		};
		builder.SetTrigger(new TimeTrigger(15, oneShot: false));
		var registration = builder.Register();
		using var reported = new ManualResetEventSlim();
		uint progress = 0;
		BackgroundTaskProgressEventHandler handler = (_, args) =>
		{
			progress = args.Progress;
			reported.Set();
		};
		registration.Progress += handler;

		try
		{
			Assert.AreEqual(0, BackgroundTaskRunner.Run(registration.TaskId));

			Assert.IsTrue(
				reported.Wait(EventTimeout),
				"Progress was not raised. " + DescribeEvents(registration.TaskId));
			Assert.AreEqual(42u, progress);
		}
		finally
		{
			registration.Progress -= handler;
			registration.Unregister(cancelTask: true);
		}
	}

	[TestMethod]
	public void When_TaskIsRegistered_Then_RegistrationRoundTrips()
	{
		var registration = RegisterTask(oneShot: false);
		IBackgroundTaskRegistration registrationContract = registration;

		Assert.AreEqual("Unit test task", registration.Name);
		Assert.AreEqual(15u, ((TimeTrigger)registration.Trigger).FreshnessTime);
		Assert.IsTrue(BackgroundTaskRegistration.AllTasks.ContainsKey(registration.TaskId));
		Assert.AreEqual(registration.TaskId, Scheduler.Registered.Single().TaskId);

		registrationContract.Unregister(cancelTask: true);

		Assert.IsFalse(BackgroundTaskRegistration.AllTasks.ContainsKey(registration.TaskId));
		Assert.AreEqual(registration.TaskId, Scheduler.Unregistered.Single().TaskId);
		Assert.IsTrue(Scheduler.LastCancelTask);
	}

	[TestMethod]
	public void When_ProcessCommandIsCreated_Then_ApplicationIdentityIsIncluded()
	{
		var taskId = Guid.NewGuid();

		var command = BackgroundTaskProcessCommand.Create(taskId);

		Assert.IsTrue(
			BackgroundTaskActivation.TryGetActivation(
				command.Arguments,
				out var activation));
		Assert.IsNotNull(activation);
		Assert.AreEqual(taskId, activation.TaskId);
		Assert.AreEqual(
			Windows.ApplicationModel.Package.Current.Id.Name,
			activation.ApplicationAssemblyName);
	}

	[TestMethod]
	public void When_OneShotEntryPointIsInvalid_Then_ItIsStillUnregistered()
	{
		var builder = new BackgroundTaskBuilder
		{
			Name = "Invalid entry point",
			TaskEntryPoint = "Missing.BackgroundTask"
		};
		builder.SetTrigger(new TimeTrigger(15, oneShot: true));
		var registration = builder.Register();

		var exitCode = BackgroundTaskRunner.Run(registration.TaskId);

		Assert.AreEqual(3, exitCode);
		Assert.IsFalse(BackgroundTaskRegistration.AllTasks.ContainsKey(registration.TaskId));
		Assert.AreEqual(registration.TaskId, Scheduler.Unregistered.Single().TaskId);
		Assert.AreEqual(
			1,
			Directory.EnumerateFiles(
				BackgroundTaskRegistrationStore.EventsDirectory,
				$"{registration.TaskId:N}-*.event").Count());
	}

	[TestMethod]
	public void When_RegistrationGroupIsSet_Then_RegistrationIsRejected()
	{
		var builder = new BackgroundTaskBuilder
		{
			Name = "Grouped task",
			TaskEntryPoint = typeof(DeferredTask).FullName!,
			TaskGroup = new BackgroundTaskRegistrationGroup("group")
		};
		builder.SetTrigger(new TimeTrigger(15, oneShot: false));

		Assert.ThrowsExactly<PlatformNotSupportedException>(() => builder.Register());
	}

	[TestMethod]
	public void When_AccessIsRemoved_Then_RegistrationsRemain()
	{
		var registration = RegisterTask(oneShot: false);

		BackgroundExecutionManager.RemoveAccess();

		Assert.IsTrue(BackgroundTaskRegistration.AllTasks.ContainsKey(registration.TaskId));
		registration.Unregister(cancelTask: true);
	}

	[TestMethod]
	public void When_OneShotTaskRunsWithDeferral_Then_ItWaitsAndUnregisters()
	{
		var registration = RegisterTask(oneShot: true);

		var exitCode = BackgroundTaskRunner.Run(registration.TaskId);

		Assert.AreEqual(0, exitCode);
		Assert.AreEqual(1, DeferredTask.RunCount);
		Assert.IsFalse(BackgroundTaskRegistration.AllTasks.ContainsKey(registration.TaskId));
		Assert.AreEqual(registration.TaskId, Scheduler.Unregistered.Single().TaskId);
	}

	[TestMethod]
	public void When_RegistrationStoreRoundTrips_Then_AllFieldsArePreserved()
	{
		var taskId = Guid.NewGuid();
		var path = Path.Combine(
			Path.GetTempPath(),
			"UnoBackgroundTaskTests",
			taskId.ToString("N"),
			"registrations.dat");
		var record = new BackgroundTaskRegistrationRecord
		{
			TaskId = taskId,
			Name = "Round trip",
			TaskEntryPoint = typeof(DeferredTask).FullName!,
			Trigger = new TimeTrigger(30, oneShot: true),
			CancelOnConditionLoss = true,
			IsNetworkRequested = true,
			GroupId = "group",
			GroupName = "Group",
			ExecutablePath = "/path with spaces/app",
			ExecutableArguments = ["argument", "value with spaces"],
			WorkingDirectory = "/working directory"
		};

		try
		{
			BackgroundTaskRegistrationStore.WriteStore(path, [record]);

			var restored = BackgroundTaskRegistrationStore.ReadStore(path).Single();

			Assert.AreEqual(record.TaskId, restored.TaskId);
			Assert.AreEqual(record.Name, restored.Name);
			Assert.AreEqual(record.TaskEntryPoint, restored.TaskEntryPoint);
			Assert.AreEqual(record.Trigger.FreshnessTime, restored.Trigger.FreshnessTime);
			Assert.AreEqual(record.Trigger.OneShot, restored.Trigger.OneShot);
			Assert.AreEqual(record.CancelOnConditionLoss, restored.CancelOnConditionLoss);
			Assert.AreEqual(record.IsNetworkRequested, restored.IsNetworkRequested);
			Assert.AreEqual(record.GroupId, restored.GroupId);
			Assert.AreEqual(record.GroupName, restored.GroupName);
			Assert.AreEqual(record.ExecutablePath, restored.ExecutablePath);
			CollectionAssert.AreEqual(
				record.ExecutableArguments.ToArray(),
				restored.ExecutableArguments.ToArray());
			Assert.AreEqual(record.WorkingDirectory, restored.WorkingDirectory);
		}
		finally
		{
			var directory = Path.GetDirectoryName(path);
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			if (directory is not null && Directory.Exists(directory))
			{
				Directory.Delete(directory);
			}
		}
	}

	[TestMethod]
	public void When_StoreFormatIsUnsupported_Then_ReadThrows()
	{
		var path = CreateScratchStorePath();
		try
		{
			File.WriteAllBytes(path, [0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);

			Assert.ThrowsExactly<InvalidDataException>(
				() => BackgroundTaskRegistrationStore.ReadStore(path));
		}
		finally
		{
			DeleteScratchStore(path);
		}
	}

	[TestMethod]
	public void When_StoreHasTrailingData_Then_ReadThrows()
	{
		var path = CreateScratchStorePath();
		try
		{
			BackgroundTaskRegistrationStore.WriteStore(path, []);
			using (var stream = File.Open(path, FileMode.Append, FileAccess.Write))
			{
				stream.WriteByte(0);
			}

			Assert.ThrowsExactly<InvalidDataException>(
				() => BackgroundTaskRegistrationStore.ReadStore(path));
		}
		finally
		{
			DeleteScratchStore(path);
		}
	}

	[TestMethod]
	public void When_EventKindIsUnknown_Then_ReadThrows()
	{
		Directory.CreateDirectory(BackgroundTaskRegistrationStore.EventsDirectory);
		var path = Path.Combine(
			BackgroundTaskRegistrationStore.EventsDirectory,
			$"{Guid.NewGuid():N}-{DateTime.UtcNow.Ticks:D19}-{Guid.NewGuid():N}.event");
		using (var stream = File.Create(path))
		using (var writer = new BinaryWriter(stream, Encoding.UTF8))
		{
			writer.Write((byte)200);
			writer.Write(Guid.NewGuid().ToByteArray());
			writer.Write(Guid.NewGuid().ToByteArray());
			writer.Write(0u);
			writer.Write(string.Empty);
		}

		Assert.ThrowsExactly<InvalidDataException>(
			() => BackgroundTaskRegistrationStore.ReadEvent(path));
	}

	private static string DescribeEvents(Guid taskId)
	{
		var directory = BackgroundTaskRegistrationStore.EventsDirectory;
		var files = Directory.Exists(directory)
			? string.Join(", ", Directory.EnumerateFiles(directory, "*.event").Select(Path.GetFileName))
			: "<missing>";
		return $"taskId={taskId:N} directory={directory} files=[{files}]";
	}

	private static string CreateScratchStorePath()
	{
		var directory = Path.Combine(
			Path.GetTempPath(),
			"UnoBackgroundTaskTests",
			Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(directory);
		return Path.Combine(directory, "registrations.dat");
	}

	private static void DeleteScratchStore(string path)
	{
		var directory = Path.GetDirectoryName(path);
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		if (directory is not null && Directory.Exists(directory))
		{
			Directory.Delete(directory);
		}
	}

	private static BackgroundTaskRegistration RegisterTask(bool oneShot)
	{
		var builder = new BackgroundTaskBuilder
		{
			Name = "Unit test task",
			TaskEntryPoint = typeof(DeferredTask).FullName!
		};
		builder.SetTrigger(new TimeTrigger(15, oneShot));
		return builder.Register();
	}

	public sealed class DeferredTask : IBackgroundTask
	{
		private static int _runCount;

		public static int RunCount
		{
			get => _runCount;
			set => _runCount = value;
		}

		public void Run(IBackgroundTaskInstance taskInstance)
		{
			var deferral = taskInstance.GetDeferral();
			_ = Task.Run(() =>
			{
				Interlocked.Increment(ref _runCount);
				deferral.Complete();
			});
		}
	}

	public sealed class CancellationIgnoringTask : IBackgroundTask
	{
		public void Run(IBackgroundTaskInstance taskInstance)
		{
			_ = taskInstance.GetDeferral();
			((BackgroundTaskInstance)taskInstance).Cancel(
				BackgroundTaskCancellationReason.Terminating);
		}
	}

	public sealed class FailingTask : IBackgroundTask
	{
		public void Run(IBackgroundTaskInstance taskInstance)
			=> throw new InvalidOperationException("Background task failure");
	}

	public sealed class ProgressReportingTask : IBackgroundTask
	{
		public void Run(IBackgroundTaskInstance taskInstance)
			=> taskInstance.Progress = 42;
	}

	private sealed class TestScheduler : IBackgroundTaskSchedulerExtension
	{
		internal List<BackgroundTaskRegistrationRecord> Registered { get; } = [];

		internal List<BackgroundTaskRegistrationRecord> Unregistered { get; } = [];

		internal bool LastCancelTask { get; private set; }

		public bool IsSupported => true;

		public void Reconcile()
		{
		}

		public void Register(BackgroundTaskRegistrationRecord registration)
			=> Registered.Add(registration);

		public void Unregister(
			BackgroundTaskRegistrationRecord registration,
			bool cancelTask)
		{
			Unregistered.Add(registration);
			LastCancelTask = cancelTask;
		}

		public void CompleteOneShot(BackgroundTaskRegistrationRecord registration)
			=> Unregister(registration, cancelTask: false);

		internal void Reset()
		{
			Registered.Clear();
			Unregistered.Clear();
			LastCancelTask = false;
		}
	}
}

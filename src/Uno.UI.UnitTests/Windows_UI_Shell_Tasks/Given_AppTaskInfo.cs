#nullable enable
#pragma warning disable CS8305

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Foundation.Extensibility;
using Uno.UI.Notifications;
using Uno.UI.Shell.Tasks;
using Windows.Foundation.Metadata;
using Windows.UI.Notifications;
using Windows.UI.Shell.Tasks;

namespace Uno.UI.Tests.Windows_UI_Shell_Tasks;

[TestClass]
[DoNotParallelize]
public class Given_AppTaskInfo
{
	private MemoryAppTaskInfoStore _store = null!;

	[TestInitialize]
	public void Initialize()
	{
		_store = new();
		AppTaskInfoRegistry.ConfigureForTests(_store, new TestAppTaskInfoExtension(isSupported: true));
	}

	[TestCleanup]
	public void Cleanup() => AppTaskInfoRegistry.ResetAfterTests();

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23752")]
	public void When_Task_Lifecycle_Is_Updated()
	{
		var task = CreateTask();

		Assert.AreEqual(1, AppTaskInfo.FindAll().Length);
		Assert.AreEqual(AppTaskState.Running, task.State);
		Assert.IsNull(task.EndTime);
		Assert.IsFalse(task.HiddenByUser);
		Assert.IsTrue(Guid.TryParse(task.Id, out _));

		var deepLink = new Uri("sample-app://tasks/updated");
		task.UpdateTitles("Updated title", string.Empty);
		task.UpdateDeepLink(deepLink);
		task.Update(
			AppTaskState.Completed,
			AppTaskContent.CreateSequenceOfSteps(["Prepare"], "Publish"));

		Assert.AreEqual("Updated title", task.Title);
		Assert.AreEqual(string.Empty, task.Subtitle);
		Assert.AreEqual(deepLink, task.DeepLink);
		Assert.AreEqual(AppTaskState.Completed, task.State);
		Assert.IsNotNull(task.EndTime);
		CollectionAssert.AreEqual(new[] { "Prepare" }, task.GetCompletedSteps());
		Assert.AreEqual("Publish", task.GetExecutingStep());

		var endTime = task.EndTime;
		task.UpdateState(AppTaskState.Completed);
		Assert.AreEqual(endTime, task.EndTime, "Re-applying the same ending state keeps the ending timestamp.");

		task.UpdateState(AppTaskState.Running);
		Assert.IsNull(task.EndTime, "Leaving an ending state clears the ending timestamp.");

		task.UpdateState(AppTaskState.Error);
		Assert.IsNotNull(task.EndTime, "Entering another ending state stamps a new ending timestamp.");
		Assert.IsTrue(task.EndTime >= endTime);

		task.Remove();
		task.Remove();
		Assert.AreEqual(0, AppTaskInfo.FindAll().Length);

		task.UpdateState(AppTaskState.Paused);
		Assert.AreEqual(AppTaskState.Paused, task.State);
		Assert.AreEqual(0, AppTaskInfo.FindAll().Length, "Updating a removed handle must not recreate the task.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23752")]
	public void When_Registry_Is_Reloaded_Then_Task_Is_Restored()
	{
		var task = CreateTask();
		task.UpdateTitles("Restored title", "Restored subtitle");
		task.UpdateDeepLink(new Uri("sample-app://tasks/restored"));
		task.Update(AppTaskState.Error, AppTaskContent.CreateTextSummaryResult("Failed"));

		var id = task.Id;
		var startTime = task.StartTime;
		var endTime = task.EndTime;
		AppTaskInfoRegistry.ConfigureForTests(_store, new TestAppTaskInfoExtension(isSupported: true));

		var restored = AppTaskInfo.FindAll().Single();
		Assert.AreEqual(id, restored.Id);
		Assert.AreEqual("Restored title", restored.Title);
		Assert.AreEqual("Restored subtitle", restored.Subtitle);
		Assert.AreEqual(new Uri("sample-app://tasks/restored"), restored.DeepLink);
		Assert.AreEqual(AppTaskState.Error, restored.State);
		Assert.AreEqual(startTime, restored.StartTime);
		Assert.AreEqual(endTime, restored.EndTime);
		Assert.AreEqual(string.Empty, restored.GetExecutingStep());
	}

	[TestMethod]
	public void When_Content_Is_Snapshotted_Then_Source_Mutations_Do_Not_Change_Task()
	{
		var completedSteps = new[] { "One" };
		var content = AppTaskContent.CreateSequenceOfSteps(completedSteps, "Two");
		var task = AppTaskInfo.Create(
			"Snapshot",
			string.Empty,
			new Uri("sample-app://tasks/snapshot"),
			new Uri("ms-appx:///Assets/StoreLogo.png"),
			content);

		completedSteps[0] = "Mutated";
		content.SetQuestion("A later mutation");

		CollectionAssert.AreEqual(new[] { "One" }, task.GetCompletedSteps());
		Assert.AreEqual("Two", task.GetExecutingStep());
	}

	[TestMethod]
	public void When_Content_Snapshots_Have_Equivalent_Arrays_Then_They_Are_Equal()
	{
		var first = new AppTaskContentSnapshot(
			AppTaskContentKind.SequenceOfSteps,
			["One"],
			"Two",
			new Uri("https://example.com/image.png"),
			"Summary",
			[new("Asset", "Context", new Uri("https://example.com/icon.png"), new Uri("https://example.com/asset"))],
			[new("Open", new Uri("https://example.com/open"))],
			"Question",
			"Reply",
			"https://example.com/reply?text={userTextInput}");
		var equivalent = first with
		{
			CompletedSteps = ["One"],
			GeneratedAssets = [new("Asset", "Context", new Uri("https://example.com/icon.png"), new Uri("https://example.com/asset"))],
			Buttons = [new("Open", new Uri("https://example.com/open"))],
		};

		Assert.AreEqual(first, equivalent);
		Assert.AreEqual(first.GetHashCode(), equivalent.GetHashCode());
		Assert.AreNotEqual(first, equivalent with { CompletedSteps = ["Changed"] });
	}

	[TestMethod]
	public void When_Content_Interactions_Are_Invalid_Then_They_Are_Rejected()
	{
		Assert.AreEqual(2u, AppTaskContent.MaxButtons, "Windows caps app task content at two buttons.");

		var content = AppTaskContent.CreateTextSummaryResult("Summary");
		for (var index = 0; index < AppTaskContent.MaxButtons; index++)
		{
			content.AddButton($"Action {index}", new Uri($"sample-app://tasks/action/{index}"));
		}

		Assert.ThrowsExactly<ArgumentException>(() =>
			content.AddButton("Too many", new Uri("sample-app://tasks/action/too-many")));
		Assert.ThrowsExactly<ArgumentException>(() =>
			content.AddButton("Relative", new Uri("/relative", UriKind.Relative)));

		content.SetTextInput("Reply", "sample-app://tasks/reply?text={userTextInput}");
		Assert.ThrowsExactly<ArgumentException>(
			() => content.SetTextInput("Reply", "sample-app://tasks/reply?text={userTextInput}"),
			"Windows rejects a second SetTextInput call on the same content.");

		Assert.AreEqual(
			0,
			typeof(AppTaskResultAsset).GetProperties(BindingFlags.Instance | BindingFlags.Public).Length,
			"Windows SDK exposes only the AppTaskResultAsset constructor.");
	}

	[TestMethod]
	public void When_Content_Is_Created_Then_Windows_Argument_Contract_Is_Matched()
	{
		// Verified against Windows 11 26200.9106 through the Windows.UI.Shell.Tasks activation factories.
		Assert.ThrowsExactly<ArgumentException>(() => AppTaskContent.CreateSequenceOfSteps(["One"], string.Empty));
		Assert.ThrowsExactly<ArgumentException>(() => AppTaskContent.CreateSequenceOfSteps(["One"], null!));
		Assert.ThrowsExactly<ArgumentException>(() => AppTaskContent.CreateTextSummaryResult(string.Empty));
		Assert.ThrowsExactly<ArgumentNullException>(() => AppTaskContent.CreatePreviewThumbnail(null!, "Step"));
		Assert.ThrowsExactly<ArgumentException>(
			() => AppTaskContent.CreatePreviewThumbnail(new Uri("relative", UriKind.Relative), "Step"));

		var nullSteps = AppTaskContent.CreateSequenceOfSteps(null!, "Step");
		var task = AppTaskInfo.Create(
			null!,
			null!,
			new Uri("sample-app://tasks/contract"),
			new Uri("ms-appx:///Assets/StoreLogo.png"),
			nullSteps);

		Assert.AreEqual(string.Empty, task.Title, "Windows accepts an empty title in Create.");
		Assert.AreEqual(string.Empty, task.Subtitle);
		Assert.AreEqual(0, task.GetCompletedSteps().Length);

		// A null step entry is projected as an empty string instead of being rejected.
		var withNullStep = AppTaskContent.CreateSequenceOfSteps([null!, "Two"], "Step");
		task.Update(AppTaskState.Running, withNullStep);
		CollectionAssert.AreEqual(new[] { string.Empty, "Two" }, task.GetCompletedSteps());

		// A text input template does not have to contain the placeholder or be a valid URI.
		var freeFormContent = AppTaskContent.CreateTextSummaryResult("Summary");
		freeFormContent.SetTextInput(null!, "no-placeholder-here");
		freeFormContent.SetQuestion(null!);
		task.Update(AppTaskState.Running, freeFormContent);

		Assert.ThrowsExactly<ArgumentException>(() => task.UpdateTitles(string.Empty, "Subtitle"));
		Assert.ThrowsExactly<ArgumentException>(() => task.UpdateTitles(null!, "Subtitle"));
		task.UpdateTitles("Title", null!);
		Assert.AreEqual(string.Empty, task.Subtitle, "Windows accepts a null subtitle in UpdateTitles.");
		Assert.ThrowsExactly<ArgumentException>(
			() => task.UpdateDeepLink(new Uri("relative", UriKind.Relative)));
	}

	[TestMethod]
	public void When_Task_Is_Created_Then_Id_Uses_The_Windows_Format()
	{
		var task = CreateTask();

		StringAssert.StartsWith(task.Id, "{");
		StringAssert.EndsWith(task.Id, "}");
		Assert.IsTrue(Guid.TryParseExact(task.Id, "B", out _));
	}

	[TestMethod]
	public void When_Accessed_Concurrently_Then_Registry_Remains_Consistent()
	{
		Parallel.For(0, 32, index =>
		{
			var task = AppTaskInfo.Create(
				$"Task {index}",
				string.Empty,
				new Uri($"sample-app://tasks/{index}"),
				new Uri("ms-appx:///Assets/StoreLogo.png"),
				AppTaskContent.CreateTextSummaryResult(index.ToString()));
			task.UpdateState(AppTaskState.Paused);
		});

		var tasks = AppTaskInfo.FindAll();
		Assert.AreEqual(32, tasks.Length);
		Assert.AreEqual(32, tasks.Select(static task => task.Id).Distinct(StringComparer.Ordinal).Count());
		Assert.IsTrue(tasks.All(static task => task.State == AppTaskState.Paused));
	}

	[TestMethod]
	public async Task When_Store_Lock_Is_Contended_Then_Cached_Lookup_Remains_Responsive()
	{
		var task = CreateTask();
		_store.BlockNextLockAcquisition();
		var findAllTask = Task.Run(AppTaskInfo.FindAll);

		Assert.IsTrue(_store.WaitForLockAcquisition(TimeSpan.FromSeconds(5)));
		try
		{
			var lookupTask = Task.Run(() => AppTaskInfoRegistry.TryGet(task.Id));
			var completedTask = await Task.WhenAny(lookupTask, Task.Delay(TimeSpan.FromSeconds(1)));

			Assert.AreSame(lookupTask, completedTask, "Cached lookups must not wait behind storage lock contention.");
			Assert.IsNotNull(await lookupTask);
		}
		finally
		{
			_store.ReleaseLockAcquisition();
			await findAllTask;
		}
	}

	[TestMethod]
	public void When_Platform_Is_Unsupported_Then_Creation_Is_Rejected()
	{
		AppTaskInfoRegistry.ConfigureForTests(_store, new TestAppTaskInfoExtension(isSupported: false));

		Assert.IsFalse(AppTaskInfo.IsSupported());
		Assert.AreEqual(0, AppTaskInfo.FindAll().Length);
		Assert.ThrowsExactly<PlatformNotSupportedException>(CreateTask);
	}

	[TestMethod]
	public void When_Persisted_Data_Is_Corrupt_Then_It_Is_Quarantined()
	{
		_store.Value = "{ not valid JSON";
		AppTaskInfoRegistry.ConfigureForTests(_store, new TestAppTaskInfoExtension(isSupported: true));

		Assert.AreEqual(0, AppTaskInfo.FindAll().Length);
		Assert.AreEqual(1, _store.QuarantineCount);
		Assert.IsNull(_store.Value);
	}

	[TestMethod]
	public void When_File_Store_Is_Repeatedly_Quarantined_Then_Only_Recent_Files_Are_Retained()
	{
		var directory = Path.Join(Path.GetTempPath(), Path.GetFileName($"uno-app-task-tests-{Guid.NewGuid():N}"));
		var filePath = Path.Join(directory, "tasks.json");
		try
		{
			var store = new FileAppTaskInfoStore(filePath);
			for (var index = 0; index < 5; index++)
			{
				store.Write($"corrupt-{index}");
				store.Quarantine();
			}

			Assert.AreEqual(3, Directory.GetFiles(directory, "tasks.corrupt.*.json").Length);
			Assert.IsFalse(File.Exists(filePath));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[TestMethod]
	public void When_Persisted_Task_Is_Null_Then_It_Is_Quarantined()
	{
		_store.Value = """{"version":1,"tasks":[null]}""";
		AppTaskInfoRegistry.ConfigureForTests(_store, new TestAppTaskInfoExtension(isSupported: true));

		Assert.AreEqual(0, AppTaskInfo.FindAll().Length);
		Assert.AreEqual(1, _store.QuarantineCount);
	}

	[TestMethod]
	public void When_Persisted_State_Is_Tampered_Then_It_Is_Quarantined()
	{
		_ = CreateTask();
		_store.Value = _store.Value!.Replace("\"state\":0", "\"state\":42", StringComparison.Ordinal);

		AppTaskInfoRegistry.ConfigureForTests(_store, new TestAppTaskInfoExtension(isSupported: true));

		Assert.AreEqual(0, AppTaskInfo.FindAll().Length);
		Assert.AreEqual(1, _store.QuarantineCount);
	}

	[TestMethod]
	public void When_Persisted_Deep_Link_Is_Tampered_Then_It_Is_Quarantined()
	{
		_ = CreateTask();
		_store.Value = _store.Value!.Replace("sample-app://tasks/test", "not a uri", StringComparison.Ordinal);

		AppTaskInfoRegistry.ConfigureForTests(_store, new TestAppTaskInfoExtension(isSupported: true));

		Assert.AreEqual(0, AppTaskInfo.FindAll().Length);
		Assert.AreEqual(1, _store.QuarantineCount);
	}

	[TestMethod]
	public void When_Text_Input_Template_Has_No_Placeholder_Then_It_Round_Trips()
	{
		var content = AppTaskContent.CreateTextSummaryResult("Summary");
		content.SetTextInput("Reply", "sample-app://tasks/reply");
		_ = AppTaskInfo.Create(
			"Free-form template",
			string.Empty,
			new Uri("sample-app://tasks/free-form"),
			new Uri("ms-appx:///Assets/StoreLogo.png"),
			content);

		AppTaskInfoRegistry.ConfigureForTests(_store, new TestAppTaskInfoExtension(isSupported: true));

		Assert.AreEqual(1, AppTaskInfo.FindAll().Length);
		Assert.AreEqual(0, _store.QuarantineCount, "Windows does not constrain the text-input template.");
	}

	[TestMethod]
	public void When_Persistence_Fails_Then_Registry_Mutation_Is_Rolled_Back()
	{
		var task = CreateTask();
		_store.ThrowOnWrite = true;

		Assert.ThrowsExactly<IOException>(() => task.UpdateState(AppTaskState.Completed));
		Assert.AreEqual(AppTaskState.Running, task.State);
		Assert.IsNull(task.EndTime);
		Assert.AreEqual(1, AppTaskInfo.FindAll().Length);
	}

	[TestMethod]
	public void When_Querying_Contract_Then_Version_Two_Is_Present()
	{
		Assert.IsTrue(ApiInformation.IsApiContractPresent("Windows.UI.Shell.Tasks.AppTaskContract", 2));
		Assert.IsFalse(ApiInformation.IsApiContractPresent("Windows.UI.Shell.Tasks.AppTaskContract", 3));
	}

	[TestMethod]
	public async Task When_Presenter_Is_Busy_Then_Only_Latest_Revision_Is_Queued()
	{
		var extension = new ControllableAppTaskInfoExtension();
		var firstCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var secondCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		extension.AddCompletion(firstCompletion.Task);
		extension.AddCompletion(secondCompletion.Task);

		extension.Synchronize(1, [CreateSnapshot("one")]);
		await extension.WaitForInvocation();
		extension.Synchronize(2, [CreateSnapshot("two")]);
		extension.Synchronize(3, [CreateSnapshot("three")]);

		firstCompletion.SetResult();
		await extension.WaitForInvocation();
		CollectionAssert.AreEqual(new[] { "one", "three" }, extension.InvokedTitles.ToArray());
		secondCompletion.SetResult();
	}

	[TestMethod]
	public async Task When_Presenter_Fails_Then_Revision_Can_Be_Retried()
	{
		var extension = new FailFirstAppTaskInfoExtension();
		var snapshot = CreateSnapshot("retry");

		extension.Synchronize(1, [snapshot]);
		await extension.WaitForInvocation();

		for (var attempt = 0; attempt < 50 && extension.InvocationCount < 2; attempt++)
		{
			await Task.Delay(20);
			extension.Synchronize(1, [snapshot]);
		}

		Assert.AreEqual(2, extension.InvocationCount);
	}

	[TestMethod]
	public async Task When_Presenter_Becomes_Available_Then_Current_Revision_Is_Replayed()
	{
		var extension = new ControllableAppTaskInfoExtension();
		extension.AddCompletion(Task.CompletedTask);
		extension.AddCompletion(Task.CompletedTask);
		var snapshot = CreateSnapshot("replay");

		extension.SetAvailability(true);
		extension.Synchronize(1, [snapshot]);
		await extension.WaitForInvocation();
		extension.SetAvailability(false);
		extension.SetAvailability(true);
		extension.Synchronize(1, [snapshot]);
		await extension.WaitForInvocation();

		CollectionAssert.AreEqual(new[] { "replay", "replay" }, extension.InvokedTitles.ToArray());
	}

	[TestMethod]
	public void When_Explicit_Badge_Is_Set_Then_It_Takes_Precedence_Over_App_Tasks()
	{
		ApiExtensibility.Register(typeof(IBadgeUpdaterExtension), _ => TestBadgeUpdaterExtension.Instance);
		var updater = BadgeUpdateManager.CreateBadgeUpdaterForApplication();

		BadgeUpdater.SetAppTaskBadge(2);
		Assert.AreEqual(2, TestBadgeUpdaterExtension.Instance.Value);

		var xml = BadgeUpdateManager.GetTemplateContent(BadgeTemplateType.BadgeNumber);
		((Windows.Data.Xml.Dom.XmlElement)xml.SelectSingleNode("/badge")!).SetAttribute("value", "7");
		updater.Update(new BadgeNotification(xml));
		BadgeUpdater.SetAppTaskBadge(3);
		Assert.AreEqual(7, TestBadgeUpdaterExtension.Instance.Value);

		updater.Clear();
		Assert.AreEqual(3, TestBadgeUpdaterExtension.Instance.Value);
		BadgeUpdater.SetAppTaskBadge(null);
		Assert.IsNull(TestBadgeUpdaterExtension.Instance.Value);
	}

	private static AppTaskInfo CreateTask() =>
		AppTaskInfo.Create(
			"Test task",
			"Test subtitle",
			new Uri("sample-app://tasks/test"),
			new Uri("ms-appx:///Assets/StoreLogo.png"),
			AppTaskContent.CreateSequenceOfSteps(Array.Empty<string>(), "Prepare"));

	private static AppTaskInfoSnapshot CreateSnapshot(string title) => new(
		Guid.NewGuid().ToString("B"),
		title,
		string.Empty,
		new Uri("sample-app://tasks/test"),
		new Uri("ms-appx:///Assets/StoreLogo.png"),
		AppTaskState.Running,
		DateTimeOffset.UtcNow,
		null,
		HiddenByUser: false,
		AppTaskContent.CreateTextSummaryResult(title).CreateSnapshot());

	private sealed class MemoryAppTaskInfoStore : IAppTaskInfoStore
	{
		private readonly object _gate = new();
		private readonly ManualResetEventSlim _lockAcquireStarted = new(initialState: false);
		private readonly ManualResetEventSlim _releaseLockAcquire = new(initialState: false);
		private string? _value;
		private int _blockNextLockAcquisition;

		internal int QuarantineCount { get; private set; }

		internal bool ThrowOnWrite { get; set; }

		internal string? Value
		{
			get
			{
				lock (_gate)
				{
					return _value;
				}
			}
			set
			{
				lock (_gate)
				{
					_value = value;
				}
			}
		}

		public string? Read()
		{
			lock (_gate)
			{
				return _value;
			}
		}

		public void Write(string value)
		{
			lock (_gate)
			{
				if (ThrowOnWrite)
				{
					throw new IOException("Test persistence failure.");
				}

				_value = value;
			}
		}

		public void Quarantine()
		{
			lock (_gate)
			{
				_value = null;
				QuarantineCount++;
			}
		}

		internal void BlockNextLockAcquisition()
		{
			_lockAcquireStarted.Reset();
			_releaseLockAcquire.Reset();
			Volatile.Write(ref _blockNextLockAcquisition, 1);
		}

		internal bool WaitForLockAcquisition(TimeSpan timeout) => _lockAcquireStarted.Wait(timeout);

		internal void ReleaseLockAcquisition() => _releaseLockAcquire.Set();

		public IDisposable AcquireLock()
		{
			if (Interlocked.Exchange(ref _blockNextLockAcquisition, 0) == 1)
			{
				_lockAcquireStarted.Set();
				if (!_releaseLockAcquire.Wait(TimeSpan.FromSeconds(5)))
				{
					throw new TimeoutException("Timed out waiting for the test store lock to be released.");
				}
			}

			return NoopAppTaskInfoStoreLock.Instance;
		}
	}

	private sealed class TestAppTaskInfoExtension : AppTaskInfoExtensionBase
	{
		private readonly bool _isSupported;

		internal TestAppTaskInfoExtension(bool isSupported)
		{
			_isSupported = isSupported;
		}

		public override bool IsSupported() => _isSupported;

		protected override Task OnSynchronizeAsync(AppTaskInfoSnapshot[] tasks) => Task.CompletedTask;
	}

	private sealed class ControllableAppTaskInfoExtension : AppTaskInfoExtensionBase
	{
		private readonly ConcurrentQueue<Task> _completions = new();
		private readonly SemaphoreSlim _invocationSignal = new(initialCount: 0);

		internal List<string> InvokedTitles { get; } = new();

		public override bool IsSupported() => true;

		internal void AddCompletion(Task completion) => _completions.Enqueue(completion);

		internal Task<bool> WaitForInvocation() => _invocationSignal.WaitAsync(TimeSpan.FromSeconds(5));

		protected override Task OnSynchronizeAsync(AppTaskInfoSnapshot[] tasks)
		{
			lock (InvokedTitles)
			{
				InvokedTitles.Add(tasks.Single().Title);
			}

			_invocationSignal.Release();
			return _completions.TryDequeue(out var completion) ? completion : Task.CompletedTask;
		}
	}

	private sealed class FailFirstAppTaskInfoExtension : AppTaskInfoExtensionBase
	{
		private readonly SemaphoreSlim _invocationSignal = new(initialCount: 0);
		private int _invocationCount;

		internal int InvocationCount => Volatile.Read(ref _invocationCount);

		public override bool IsSupported() => true;

		internal Task<bool> WaitForInvocation() => _invocationSignal.WaitAsync(TimeSpan.FromSeconds(5));

		protected override Task OnSynchronizeAsync(AppTaskInfoSnapshot[] tasks)
		{
			var invocation = Interlocked.Increment(ref _invocationCount);
			_invocationSignal.Release();
			return invocation == 1
				? Task.FromException(new InvalidOperationException("Test presenter failure."))
				: Task.CompletedTask;
		}
	}

	private sealed class TestBadgeUpdaterExtension : IBadgeUpdaterExtension
	{
		internal static TestBadgeUpdaterExtension Instance { get; } = new();

		internal int? Value { get; private set; }

		public void SetBadge(int? value) => Value = value;
	}
}

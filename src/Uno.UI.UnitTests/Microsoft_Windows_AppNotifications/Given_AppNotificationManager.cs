#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Internal;
using PublicAppNotificationProgressData = Microsoft.Windows.AppNotifications.AppNotificationProgressData;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_AppNotificationManager
{
	[TestInitialize]
	public void Initialize() => AppNotificationActivationBroker.ResetForTests();

	[TestCleanup]
	public void Cleanup() => AppNotificationActivationBroker.ResetForTests();

	[TestMethod]
	public void When_Backend_Is_Not_Available_State_Is_Unsupported()
	{
		Assert.AreSame(AppNotificationManager.Default, AppNotificationManager.Default);
		Assert.IsFalse(AppNotificationManager.IsSupported());
		Assert.AreEqual(AppNotificationSetting.Unsupported, AppNotificationManager.Default.Setting);
	}

	[TestMethod]
	public void When_Backend_Is_Enabled_Manager_Delegates_Lifecycle()
	{
		var backend = new TestBackend();
		var manager = new AppNotificationManager(backend);
		manager.Register();
		manager.Unregister();
		var assetsBackend = new TestBackend();
		var assetsManager = new AppNotificationManager(assetsBackend);
		assetsManager.Register("Contoso", new Uri("file:///icon.png"));
		assetsManager.Unregister();

		Assert.AreEqual(AppNotificationSetting.Enabled, manager.Setting);
		CollectionAssert.AreEqual(new[] { "Register", "Unregister" }, backend.Calls);
		CollectionAssert.AreEqual(new[] { "Register:Contoso:file:///icon.png", "Unregister" }, assetsBackend.Calls);
	}

	[TestMethod]
	public void When_Backend_Is_Disabled_Show_Is_A_NoOp()
	{
		var backend = new TestBackend { Setting = AppNotificationSetting.DisabledForApplication };
		var manager = new AppNotificationManager(backend);
		var notification = CreateNotification();
		manager.Register();

		manager.Show(notification);

		Assert.AreEqual(0u, notification.Id);
		Assert.AreEqual(0, backend.Shown.Count);
	}

	[TestMethod]
	public void When_Public_Show_Is_Called_Before_Register_It_Is_A_NoOp()
	{
		var backend = new TestBackend();
		var manager = new AppNotificationManager(backend);
		var notification = CreateNotification();

		manager.Show(notification);

		Assert.AreEqual(0u, notification.Id);
		Assert.AreEqual(0, backend.Shown.Count);
		Assert.AreEqual(0, backend.Calls.Count);
	}

	[TestMethod]
	public void When_Manager_Unregisters_Public_Show_Remains_Available()
	{
		var backend = new TestBackend();
		var manager = new AppNotificationManager(backend);
		manager.Register();
		manager.Unregister();
		var notification = CreateNotification();

		manager.Show(notification);

		Assert.AreNotEqual(0u, notification.Id);
		Assert.AreEqual(1, backend.Shown.Count);
		CollectionAssert.AreEqual(new[] { "Register", "Unregister" }, backend.Calls);
	}

	[TestMethod]
	public void When_Manager_Unregisters_All_Public_Show_Waits_For_Register()
	{
		var backend = new TestBackend();
		var manager = new AppNotificationManager(backend);
		manager.Register();
		manager.UnregisterAll();
		var blocked = CreateNotification();

		manager.Show(blocked);

		Assert.AreEqual(0u, blocked.Id);
		Assert.AreEqual(0, backend.Shown.Count);

		manager.Register();
		var restored = CreateNotification();
		manager.Show(restored);

		Assert.AreNotEqual(0u, restored.Id);
		Assert.AreEqual(1, backend.Shown.Count);
		CollectionAssert.AreEqual(new[] { "Register", "UnregisterAll", "Register" }, backend.Calls);
	}

	[TestMethod]
	public void When_UnregisterAll_Fails_After_Unregister_Public_Show_Remains_Available()
	{
		var backend = new TestBackend { UnregisterAllException = new InvalidOperationException("failed") };
		var manager = new AppNotificationManager(backend);
		manager.Register();
		manager.Unregister();

		Assert.ThrowsExactly<InvalidOperationException>(manager.UnregisterAll);
		var notification = CreateNotification();
		manager.Show(notification);

		Assert.AreNotEqual(0u, notification.Id);
		Assert.AreEqual(1, backend.Shown.Count);
	}

	[TestMethod]
	public void When_Backend_Accepts_Show_Id_Is_Assigned_After_Posting()
	{
		var backend = new TestBackend();
		var manager = new AppNotificationManager(backend);
		var first = CreateNotification("first", "group");
		var second = CreateNotification("second", "group");
		manager.Register();

		manager.Show(first);
		manager.Show(second);

		Assert.AreEqual(1u, first.Id);
		Assert.AreEqual(2u, second.Id);
		Assert.AreEqual("first", backend.Shown[0].Tag);
		Assert.AreEqual("Title", backend.Shown[0].Payload.Title?.Content);
	}

	[TestMethod]
	public void When_Public_Show_Uses_Tag_And_Group_Replacement_Semantics()
	{
		var persistence = new InMemoryAppNotificationStatePersistence();
		var backend = new TestBackend();
		var manager = new AppNotificationManager(backend, persistence);
		var original = CreateNotificationWithTitle("Original", "tag", "group");
		var replacement = CreateNotificationWithTitle("Replacement", "tag", "group");
		manager.Register();

		manager.Show(original);
		manager.Show(replacement);

		Assert.AreEqual(original.Id, replacement.Id);
		Assert.AreEqual(1, backend.Shown.Count);
		Assert.AreEqual(1, backend.Updated.Count);
		var record = new AppNotificationStateStore(persistence).GetShown().Single();
		StringAssert.Contains(record.Payload, "Replacement");
	}

	[TestMethod]
	public void When_Web_Notification_Content_Is_Twelve_Thousand_Characters_It_Is_Not_Rejected()
	{
		var title = new string('x', 12_000);
		var backend = new TestBackend();
		var manager = new AppNotificationManager(backend);
		manager.Register();

		manager.Show(new AppNotification(
			$"<toast><visual><binding template='ToastGeneric'><text>{title}</text></binding></visual></toast>"));

		Assert.AreEqual(title, backend.Shown.Single().Payload.Title?.Content);
	}

	[TestMethod]
	public void When_Backend_Rejects_Show_Id_Remains_Zero_And_Reserved_Id_Is_Not_Reused()
	{
		var backend = new TestBackend { AcceptShow = false };
		var manager = new AppNotificationManager(backend);
		var rejected = CreateNotification();
		manager.Register();
		manager.Show(rejected);

		backend.AcceptShow = true;
		var accepted = CreateNotification();
		manager.Show(accepted);

		Assert.AreEqual(0u, rejected.Id);
		Assert.AreEqual(2u, accepted.Id);
	}

	[TestMethod]
	public void When_Notification_Was_Already_Posted_Show_Throws()
	{
		var manager = new AppNotificationManager(new TestBackend());
		var notification = CreateNotification();
		manager.Register();
		manager.Show(notification);

		var exception = Assert.ThrowsExactly<COMException>(() => manager.Show(notification));

		Assert.AreEqual(unchecked((int)0x803E0106), exception.HResult);
	}

	[TestMethod]
	public void When_Supported_Register_Assets_Are_Invalid_Manager_Throws()
	{
		var manager = new AppNotificationManager(new TestBackend());

		Assert.ThrowsExactly<ArgumentException>(() => manager.Register(string.Empty, new Uri("file:///icon.png")));
		Assert.ThrowsExactly<ArgumentNullException>(() => manager.Register("Contoso", null!));
	}

	[TestMethod]
	public void When_Backend_Is_Registered_After_Manager_Creation_Resolution_Retries()
	{
		IAppNotificationManagerBackend? backend = null;
		var manager = new AppNotificationManager(() => backend);

		Assert.AreEqual(AppNotificationSetting.Unsupported, manager.Setting);

		backend = new TestBackend();

		Assert.AreEqual(AppNotificationSetting.Enabled, manager.Setting);
		manager.Register();
		manager.Show(CreateNotification());
		Assert.AreEqual(1, ((TestBackend)backend).Shown.Count);
	}

	[TestMethod]
	public void When_Activation_Was_Queued_Before_Register_Handler_Receives_It()
	{
		var manager = new AppNotificationManager(new TestBackend());
		AppNotificationActivatedEventArgs? received = null;
		manager.NotificationInvoked += (_, args) => received = args;
		AppNotificationActivationBroker.Publish(new AppNotificationActivation("action=open%3Bitem", new Dictionary<string, string> { ["reply"] = "Hello" }));

		manager.Register();

		Assert.IsNotNull(received);
		Assert.AreEqual("action=open%3Bitem", received.Argument);
		Assert.AreEqual("open;item", received.Arguments["action"]);
		Assert.AreEqual("Hello", received.UserInput["reply"]);
	}

	[TestMethod]
	public void When_Register_Drains_Activation_Handler_Calling_Unregister_Is_Rejected()
	{
		var backend = new TestBackend();
		var manager = new AppNotificationManager(backend);
		manager.NotificationInvoked += (_, _) =>
			Assert.ThrowsExactly<InvalidOperationException>(manager.Unregister);
		AppNotificationActivationBroker.Publish(CreateActivation("open"));

		manager.Register();
		var notification = CreateNotification();
		manager.Show(notification);

		Assert.AreNotEqual(0u, notification.Id);
		CollectionAssert.AreEqual(new[] { "Register" }, backend.Calls);
	}

	[TestMethod]
	public void When_Activation_Arrives_After_Register_Handler_Runs_On_Publisher_Thread()
	{
		var manager = new AppNotificationManager(new TestBackend());
		var publisherThread = Environment.CurrentManagedThreadId;
		var callbackThread = 0;
		manager.NotificationInvoked += (_, _) => callbackThread = Environment.CurrentManagedThreadId;
		manager.Register();

		AppNotificationActivationBroker.Publish(new AppNotificationActivation("open", new Dictionary<string, string>()));

		Assert.AreEqual(publisherThread, callbackThread);
	}

	[TestMethod]
	public void When_Handler_Is_Added_After_Register_Manager_Throws()
	{
		var manager = new AppNotificationManager(new TestBackend());
		manager.Register();

		Assert.ThrowsExactly<InvalidOperationException>(() => manager.NotificationInvoked += (_, _) => { });
	}

	[TestMethod]
	public void When_Manager_Unregisters_Activation_Is_Rejected_And_Not_Replayed()
	{
		var manager = new AppNotificationManager(new TestBackend());
		var count = 0;
		manager.NotificationInvoked += (_, _) => count++;
		manager.Register();
		manager.Unregister();

		var accepted = AppNotificationActivationBroker.Publish(new AppNotificationActivation("open", new Dictionary<string, string>()));
		Assert.AreEqual(0, count);
		Assert.IsFalse(accepted);

		manager.Register();
		Assert.AreEqual(0, count);
	}

	[TestMethod]
	public async Task When_Unregister_Waits_For_Activation_Handler_Calling_Show_It_Does_Not_Deadlock()
	{
		var backend = new TestBackend();
		var manager = new AppNotificationManager(backend);
		using var activationEntered = new ManualResetEventSlim();
		using var allowShow = new ManualResetEventSlim();
		manager.NotificationInvoked += (_, _) =>
		{
			activationEntered.Set();
			Assert.IsTrue(allowShow.Wait(TimeSpan.FromSeconds(5)));
			manager.Show(CreateNotification());
		};
		manager.Register();

		var publish = Task.Run(() => AppNotificationActivationBroker.Publish(CreateActivation("open")));
		Assert.IsTrue(activationEntered.Wait(TimeSpan.FromSeconds(5)));
		var unregister = Task.Run(manager.Unregister);
		allowShow.Set();

		await Task.WhenAll(publish, unregister).WaitAsync(TimeSpan.FromSeconds(5));

		Assert.AreEqual(1, backend.Shown.Count);
		CollectionAssert.AreEqual(new[] { "Register", "Unregister" }, backend.Calls);
	}

	[TestMethod]
	public async Task When_Unregister_Waits_For_Activation_Handler_Calling_Unregister_It_Does_Not_Deadlock()
	{
		using var activationEntered = new ManualResetEventSlim();
		using var unregisterEntered = new ManualResetEventSlim();
		var backend = new TestBackend { UnregisterAction = unregisterEntered.Set };
		var manager = new AppNotificationManager(backend);
		manager.NotificationInvoked += (_, _) =>
		{
			activationEntered.Set();
			Assert.IsTrue(unregisterEntered.Wait(TimeSpan.FromSeconds(5)));
			Assert.ThrowsExactly<InvalidOperationException>(manager.Unregister);
		};
		manager.Register();

		var publish = Task.Run(() => AppNotificationActivationBroker.Publish(CreateActivation("open")));
		Assert.IsTrue(activationEntered.Wait(TimeSpan.FromSeconds(5)));
		var unregister = Task.Run(manager.Unregister);

		await Task.WhenAll(publish, unregister).WaitAsync(TimeSpan.FromSeconds(5));

		CollectionAssert.AreEqual(new[] { "Register", "Unregister" }, backend.Calls);
	}

	[TestMethod]
	public async Task When_UnregisterAll_Waits_For_Activation_Handler_Calling_Show_It_Is_A_NoOp()
	{
		using var activationEntered = new ManualResetEventSlim();
		using var unregisterAllEntered = new ManualResetEventSlim();
		var backend = new TestBackend { UnregisterAllAction = unregisterAllEntered.Set };
		var manager = new AppNotificationManager(backend);
		var notification = CreateNotification();
		manager.NotificationInvoked += (_, _) =>
		{
			activationEntered.Set();
			Assert.IsTrue(unregisterAllEntered.Wait(TimeSpan.FromSeconds(5)));
			manager.Show(notification);
		};
		manager.Register();

		var publish = Task.Run(() => AppNotificationActivationBroker.Publish(CreateActivation("open")));
		Assert.IsTrue(activationEntered.Wait(TimeSpan.FromSeconds(5)));
		var unregisterAll = Task.Run(manager.UnregisterAll);

		await Task.WhenAll(publish, unregisterAll).WaitAsync(TimeSpan.FromSeconds(5));

		Assert.AreEqual(0u, notification.Id);
		Assert.AreEqual(0, backend.Shown.Count);
		CollectionAssert.AreEqual(new[] { "Register", "UnregisterAll" }, backend.Calls);
	}

	[TestMethod]
	public void When_Manager_Register_Is_Called_Twice_It_Throws()
	{
		var manager = new AppNotificationManager(new TestBackend());
		manager.Register();

		Assert.ThrowsExactly<InvalidOperationException>(() => manager.Register());
	}

	[TestMethod]
	public void When_Manager_Unregisters_Before_Register_It_Throws()
	{
		var manager = new AppNotificationManager(new TestBackend());

		Assert.ThrowsExactly<InvalidOperationException>(() => manager.Unregister());
	}

	[TestMethod]
	public void When_Backend_Register_Fails_Activation_Handler_Is_Not_Left_Registered()
	{
		var backend = new TestBackend { RegisterException = new InvalidOperationException("failed") };
		var manager = new AppNotificationManager(backend);
		var count = 0;
		manager.NotificationInvoked += (_, _) => count++;

		Assert.ThrowsExactly<InvalidOperationException>(() => manager.Register());
		AppNotificationActivationBroker.Publish(new AppNotificationActivation("open", new Dictionary<string, string>()));
		var notification = CreateNotification();
		manager.Show(notification);

		Assert.AreEqual(0, count);
		Assert.AreEqual(0u, notification.Id);
		Assert.AreEqual(0, backend.Shown.Count);
	}

	[TestMethod]
	public void When_Activation_Is_Published_During_Cold_Drain_Fifo_Is_Preserved()
	{
		var received = new List<string>();
		AppNotificationActivationBroker.Publish(CreateActivation("first"));
		AppNotificationActivationBroker.Publish(CreateActivation("second"));

		AppNotificationActivationBroker.Register(activation =>
		{
			received.Add(activation.Argument);
			if (activation.Argument == "first")
			{
				AppNotificationActivationBroker.Publish(CreateActivation("third"));
			}
		});

		CollectionAssert.AreEqual(new[] { "first", "second", "third" }, received);
	}

	[TestMethod]
	public void When_Handler_Unregisters_During_Cold_Drain_Remaining_Backlog_Is_Dropped()
	{
		var received = new List<string>();
		Action<AppNotificationActivation>? handler = null;
		handler = activation =>
		{
			received.Add(activation.Argument);
			AppNotificationActivationBroker.Unregister(handler!);
		};
		AppNotificationActivationBroker.Publish(CreateActivation("first"));
		AppNotificationActivationBroker.Publish(CreateActivation("second"));

		AppNotificationActivationBroker.Register(handler);

		CollectionAssert.AreEqual(new[] { "first" }, received);
		Assert.IsFalse(AppNotificationActivationBroker.Publish(CreateActivation("third")));
	}

	[TestMethod]
	public void When_Handler_Throws_During_Cold_Drain_Remaining_Activations_Are_Delivered()
	{
		var received = new List<string>();
		Action<AppNotificationActivation> handler = activation =>
		{
			received.Add(activation.Argument);
			if (activation.Argument == "first")
			{
				throw new InvalidOperationException("handler failure");
			}
		};
		AppNotificationActivationBroker.Publish(CreateActivation("first"));
		AppNotificationActivationBroker.Publish(CreateActivation("second"));

		AppNotificationActivationBroker.Register(handler);

		CollectionAssert.AreEqual(new[] { "first", "second" }, received);
		Assert.IsTrue(AppNotificationActivationBroker.Publish(CreateActivation("third")));
		CollectionAssert.AreEqual(new[] { "first", "second", "third" }, received);
	}

	[TestMethod]
	public void When_More_Than_Maximum_Cold_Activations_Are_Queued_Oldest_Are_Dropped()
	{
		for (var index = 0; index < 40; index++)
		{
			Assert.IsTrue(AppNotificationActivationBroker.Publish(CreateActivation(index.ToString(System.Globalization.CultureInfo.InvariantCulture))));
		}
		var received = new List<string>();

		AppNotificationActivationBroker.Register(activation => received.Add(activation.Argument));

		Assert.AreEqual(32, received.Count);
		Assert.AreEqual("8", received[0]);
		Assert.AreEqual("39", received[^1]);
	}

	[TestMethod]
	public void When_Activation_Input_Exceeds_Limits_It_Is_Rejected()
	{
		Assert.IsFalse(AppNotificationActivationBroker.Publish(new AppNotificationActivation(
			new string('a', 5121),
			new Dictionary<string, string>())));
		Assert.IsFalse(AppNotificationActivationBroker.Publish(new AppNotificationActivation(
			"open",
			new Dictionary<string, string> { ["reply"] = new string('a', 4097) })));
	}

	[TestMethod]
	public void When_Manager_Registers_Without_Public_Handler_Activation_Is_Consumed()
	{
		var manager = new AppNotificationManager(new TestBackend());
		manager.Register();

		Assert.IsTrue(AppNotificationActivationBroker.Publish(CreateActivation("open")));
	}

	[TestMethod]
	public async Task When_Shown_Notifications_Are_Queried_Transient_Properties_Reset()
	{
		var manager = new AppNotificationManager(new TestBackend(), new InMemoryAppNotificationStatePersistence());
		var notification = CreateNotification("tag", "group");
		notification.Priority = AppNotificationPriority.High;
		notification.SuppressDisplay = true;
		notification.Progress = new PublicAppNotificationProgressData(1) { Status = "Starting", Value = 0.1 };
		manager.Register();
		manager.Show(notification);

		var shown = await manager.GetAllAsync();

		Assert.AreEqual(1, shown.Count);
		Assert.AreEqual(notification.Id, shown[0].Id);
		Assert.AreEqual("tag", shown[0].Tag);
		Assert.AreEqual("group", shown[0].Group);
		Assert.AreEqual(AppNotificationPriority.Default, shown[0].Priority);
		Assert.IsFalse(shown[0].SuppressDisplay);
		Assert.AreEqual(1u, shown[0].Progress?.SequenceNumber);
	}

	[TestMethod]
	public async Task When_Remove_Selectors_Are_Used_All_Matching_Native_Notifications_Are_Removed()
	{
		var backend = new TestBackend();
		var first = CreateStateRecord(1, AppNotificationPostingState.Shown);
		var second = CreateStateRecord(2, AppNotificationPostingState.Shown);
		var other = CreateStateRecord(3, AppNotificationPostingState.Shown) with { Group = "other" };
		var persistence = new InMemoryAppNotificationStatePersistence(new AppNotificationStateSnapshot(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			4,
			new[] { first, second, other }));
		var manager = new AppNotificationManager(backend, persistence);

		await manager.RemoveByTagAndGroupAsync("tag", "group");

		CollectionAssert.AreEquivalent(new[] { first.Id, second.Id }, backend.Removed.Select(record => record.Id).ToArray());
		var remaining = await manager.GetAllAsync();
		CollectionAssert.AreEqual(new[] { other.Id }, remaining.Select(item => item.Id).ToArray());
	}

	[TestMethod]
	public async Task When_Tag_Only_Remove_Is_Used_Only_The_Default_Group_Is_Removed()
	{
		var backend = new TestBackend();
		var persistence = new InMemoryAppNotificationStatePersistence();
		var manager = new AppNotificationManager(backend, persistence);
		manager.Register();
		var defaultGroup = CreateNotification("tag", string.Empty);
		var namedGroup = CreateNotification("tag", "named");
		manager.Show(defaultGroup);
		manager.Show(namedGroup);

		await manager.RemoveByTagAsync("tag");

		CollectionAssert.AreEqual(new[] { defaultGroup.Id }, backend.Removed.Select(record => record.Id).ToArray());
		CollectionAssert.AreEqual(
			new[] { namedGroup.Id },
			new AppNotificationStateStore(persistence).GetShown().Select(record => record.Id).ToArray());
	}

	[TestMethod]
	public async Task When_Remove_Arguments_Are_Invalid_Manager_Throws()
	{
		var manager = new AppNotificationManager(new TestBackend(), new InMemoryAppNotificationStatePersistence());

		await Assert.ThrowsExactlyAsync<ArgumentException>(() => manager.RemoveByIdAsync(0).AsTask());
		await Assert.ThrowsExactlyAsync<ArgumentException>(() => manager.RemoveByTagAsync(string.Empty).AsTask());
		await manager.RemoveByTagAndGroupAsync("tag", string.Empty);
		await Assert.ThrowsExactlyAsync<ArgumentException>(() => manager.RemoveByGroupAsync(string.Empty).AsTask());
	}

	[TestMethod]
	public async Task When_Progress_Is_Updated_Only_Newer_Sequences_Reach_Backend()
	{
		var backend = new TestBackend();
		var manager = new AppNotificationManager(backend, new InMemoryAppNotificationStatePersistence());
		var notification = CreateNotification("progress", "group");
		manager.Register();
		manager.Show(notification);

		var first = await manager.UpdateAsync(new PublicAppNotificationProgressData(5) { Status = "Half", Value = 0.5 }, "progress", "group");
		var stale = await manager.UpdateAsync(new PublicAppNotificationProgressData(4) { Status = "Old", Value = 0.4 }, "progress", "group");

		Assert.AreEqual(AppNotificationProgressResult.Succeeded, first);
		Assert.AreEqual(AppNotificationProgressResult.Succeeded, stale);
		Assert.AreEqual(1, backend.Updated.Count);
		Assert.AreEqual(5u, backend.Updated[0].Progress?.SequenceNumber);
	}

	[TestMethod]
	public async Task When_Progress_Target_Is_Missing_Result_Is_NotFound()
	{
		var manager = new AppNotificationManager(new TestBackend(), new InMemoryAppNotificationStatePersistence());

		var result = await manager.UpdateAsync(new PublicAppNotificationProgressData(1), "missing");

		Assert.AreEqual(AppNotificationProgressResult.AppNotificationNotFound, result);
	}

	[TestMethod]
	public async Task When_Default_Group_Progress_Is_Updated_Only_The_Default_Group_Is_Targeted()
	{
		var backend = new TestBackend();
		var manager = new AppNotificationManager(backend, new InMemoryAppNotificationStatePersistence());
		manager.Register();
		var defaultGroup = CreateNotification("progress", string.Empty);
		var namedGroup = CreateNotification("progress", "named");
		manager.Show(defaultGroup);
		manager.Show(namedGroup);

		var result = await manager.UpdateAsync(new PublicAppNotificationProgressData(1), "progress");
		var explicitResult = await manager.UpdateAsync(new PublicAppNotificationProgressData(2), "progress", string.Empty);

		Assert.AreEqual(AppNotificationProgressResult.Succeeded, result);
		Assert.AreEqual(AppNotificationProgressResult.Succeeded, explicitResult);
		Assert.AreEqual(2, backend.Updated.Count);
		Assert.IsTrue(backend.Updated.All(record => record.Id == defaultGroup.Id));
	}

	[TestMethod]
	public async Task When_Backend_Is_Unsupported_Lifecycle_Operations_Are_Empty_Or_Unsupported()
	{
		var manager = new AppNotificationManager((IAppNotificationManagerBackend?)null, new InMemoryAppNotificationStatePersistence());

		var update = await manager.UpdateAsync(new PublicAppNotificationProgressData(1), "tag");
		var invalidUpdate = await manager.UpdateAsync(null!, string.Empty);
		await manager.RemoveByIdAsync(0);
		await manager.RemoveByTagAsync(string.Empty);
		await manager.RemoveAllAsync();
		var shown = await manager.GetAllAsync();
		manager.Show(null!);

		Assert.AreEqual(AppNotificationProgressResult.Unsupported, update);
		Assert.AreEqual(AppNotificationProgressResult.Unsupported, invalidUpdate);
		Assert.AreEqual(0, shown.Count);
	}

	[TestMethod]
	public async Task When_Backend_Reports_No_Active_Notifications_Query_Reconciles_State()
	{
		var backend = new TestBackend { ActiveNotificationIds = new HashSet<uint>() };
		var manager = new AppNotificationManager(backend, new InMemoryAppNotificationStatePersistence());
		manager.Register();
		manager.Show(CreateNotification());

		var shown = await manager.GetAllAsync();

		Assert.AreEqual(0, shown.Count);
	}

	[TestMethod]
	public async Task When_Native_Removal_Fails_Durable_State_Remains_For_Retry()
	{
		var backend = new TestBackend { RemoveException = new InvalidOperationException("failed") };
		var manager = new AppNotificationManager(backend, new InMemoryAppNotificationStatePersistence());
		var notification = CreateNotification();
		manager.Register();
		manager.Show(notification);

		await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => manager.RemoveByIdAsync(notification.Id).AsTask());
		backend.RemoveException = null;

		var shown = await manager.GetAllAsync();

		CollectionAssert.AreEqual(new[] { notification.Id }, shown.Select(item => item.Id).ToArray());
	}

	[TestMethod]
	public async Task When_Notification_Was_Dismissed_Progress_Update_Does_Not_Repost_It()
	{
		var backend = new AsyncTestBackend();
		var manager = new AppNotificationManager(backend, new InMemoryAppNotificationStatePersistence());
		manager.Register();
		manager.Show(CreateNotification("progress", "group"));
		backend.ActiveNotificationIds = new HashSet<uint>();

		var result = await manager.UpdateAsync(new PublicAppNotificationProgressData(1), "progress", "group");

		Assert.AreEqual(AppNotificationProgressResult.AppNotificationNotFound, result);
		Assert.AreEqual(0, backend.Updated.Count);
	}

	[TestMethod]
	public async Task When_Active_Id_Result_Is_Stale_A_Newer_Replacement_Is_Not_Removed()
	{
		var lookupStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var lookupResult = new TaskCompletionSource<IReadOnlyCollection<uint>?>(TaskCreationOptions.RunContinuationsAsynchronously);
		var backend = new AsyncTestBackend
		{
			GetActiveNotificationIdsAsyncHandler = () =>
			{
				lookupStarted.SetResult();
				return lookupResult.Task;
			},
		};
		var persistence = new InMemoryAppNotificationStatePersistence();
		var manager = new AppNotificationManager(backend, persistence);
		manager.Register();
		var original = CreateNotificationWithTitle("Original", "tag", "group");
		manager.Show(original);

		var query = manager.GetAllAsync().AsTask();
		await lookupStarted.Task;
		var replacement = CreateNotificationWithTitle("Replacement", "tag", "group");
		manager.Show(replacement);
		lookupResult.SetResult(Array.Empty<uint>());
		await query;

		var record = new AppNotificationStateStore(persistence).GetShown().Single();
		Assert.AreEqual(original.Id, replacement.Id);
		StringAssert.Contains(record.Payload, "Replacement");
	}

	[TestMethod]
	public async Task When_Persisted_Posting_Is_Active_Recovery_Marks_It_Shown()
	{
		var pending = CreateStateRecord(7, AppNotificationPostingState.Posting);
		var persistence = new InMemoryAppNotificationStatePersistence(new AppNotificationStateSnapshot(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			8,
			new[] { pending }));
		var backend = new TestBackend { ActiveNotificationIds = new HashSet<uint> { pending.Id } };
		var manager = new AppNotificationManager(backend, persistence);

		var shown = await manager.GetAllAsync();

		CollectionAssert.AreEqual(new[] { pending.Id }, shown.Select(notification => notification.Id).ToArray());
		Assert.AreEqual(0, backend.Removed.Count);
	}

	[TestMethod]
	public async Task When_Another_Owner_Has_A_Live_Posting_Lease_Recovery_Does_Not_Abort_It()
	{
		var pending = CreateStateRecord(7, AppNotificationPostingState.Posting) with
		{
			OperationOwner = "other-owner",
			OperationLeaseExpirationUtc = DateTimeOffset.UtcNow.AddMinutes(1),
		};
		var persistence = new InMemoryAppNotificationStatePersistence(new AppNotificationStateSnapshot(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			8,
			new[] { pending }));
		var backend = new AsyncTestBackend { ActiveNotificationIds = new HashSet<uint>() };
		var manager = new AppNotificationManager(backend, persistence);

		await manager.GetAllAsync();

		Assert.AreEqual(pending, new AppNotificationStateStore(persistence).GetPendingPostings().Single());
		Assert.AreEqual(0, backend.Removed.Count);
	}

	[TestMethod]
	public async Task When_Abandoned_Removal_Backend_Fails_Caller_Operation_Still_Succeeds()
	{
		var abandoned = CreateStateRecord(7, AppNotificationPostingState.Removing) with
		{
			OperationOwner = "other-owner",
			OperationLeaseExpirationUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
		};
		var persistence = new InMemoryAppNotificationStatePersistence(new AppNotificationStateSnapshot(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			8,
			new[] { abandoned }));
		var backend = new TestBackend
		{
			ActiveNotificationIds = new HashSet<uint> { 7 },
			RemoveException = new InvalidOperationException("failed"),
		};
		var manager = new AppNotificationManager(backend, persistence);
		manager.Register();

		manager.Show(CreateNotification());

		Assert.AreEqual(1, backend.Shown.Count);
		CollectionAssert.Contains(
			new AppNotificationStateStore(persistence).GetAllRecords().Select(record => record.Id).ToArray(),
			7u);

		backend.RemoveException = null;
		backend.ActiveNotificationIds = new HashSet<uint>();
		await manager.GetAllAsync();

		CollectionAssert.DoesNotContain(
			new AppNotificationStateStore(persistence).GetAllRecords().Select(record => record.Id).ToArray(),
			7u);
	}

	[TestMethod]
	public async Task When_Another_Owner_Posting_Lease_Expires_Recovery_Can_Claim_It()
	{
		var pending = CreateStateRecord(7, AppNotificationPostingState.Posting) with
		{
			OperationOwner = "other-owner",
			OperationLeaseExpirationUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
		};
		var persistence = new InMemoryAppNotificationStatePersistence(new AppNotificationStateSnapshot(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			8,
			new[] { pending }));
		var backend = new AsyncTestBackend { ActiveNotificationIds = new HashSet<uint>() };
		var manager = new AppNotificationManager(backend, persistence);

		await manager.GetAllAsync();

		Assert.AreEqual(0, new AppNotificationStateStore(persistence).GetAllRecords().Count);
	}

	[TestMethod]
	public async Task When_Foreign_Replacement_Lease_Is_Live_Mutations_Wait_For_Its_Late_Callback()
	{
		var persistence = new InMemoryAppNotificationStatePersistence();
		var firstBackend = new DeferredTestBackend();
		var firstManager = new AppNotificationManager(firstBackend, persistence);
		firstManager.Register();
		var original = CreateNotificationWithTitle("Original", "tag", "group");
		firstManager.Show(original);
		firstBackend.CompleteShow(original.Id, succeeded: true);
		var firstReplacement = CreateNotificationWithTitle("First replacement", "tag", "group");
		firstManager.Show(firstReplacement);
		var firstOperation = firstBackend.GetPendingOperations(original.Id).Single();
		var pending = new AppNotificationStateStore(persistence).GetPendingUpdates().Single();

		var secondBackend = new AsyncTestBackend { ActiveNotificationIds = new HashSet<uint>() };
		var secondManager = new AppNotificationManager(secondBackend, persistence);
		secondManager.Register();
		var blockedReplacement = CreateNotificationWithTitle("Blocked replacement", "tag", "group");
		secondManager.Show(blockedReplacement);
		var progressResult = await secondManager.UpdateAsync(new PublicAppNotificationProgressData(1), "tag", "group");

		Assert.AreEqual(0u, blockedReplacement.Id);
		Assert.AreEqual(0, secondBackend.Shown.Count);
		Assert.AreEqual(0, secondBackend.Updated.Count);
		Assert.AreEqual(AppNotificationProgressResult.AppNotificationNotFound, progressResult);
		Assert.AreEqual(pending, new AppNotificationStateStore(persistence).GetPendingUpdates().Single());

		firstBackend.CompleteOperation(firstOperation, succeeded: true);

		var shown = new AppNotificationStateStore(persistence).GetShown().Single();
		Assert.AreEqual(original.Id, shown.Id);
		StringAssert.Contains(shown.Payload, "First replacement");
	}

	[TestMethod]
	public async Task When_Persistent_Active_Ids_Are_Unknown_Synchronous_Recovery_Preserves_Posting()
	{
		var pending = CreateStateRecord(7, AppNotificationPostingState.Posting) with
		{
			OperationOwner = "other-owner",
			OperationLeaseExpirationUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
		};
		var persistence = new InMemoryAppNotificationStatePersistence(new AppNotificationStateSnapshot(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			8,
			new[] { pending }));
		var refreshStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var firstRefresh = new TaskCompletionSource<IReadOnlyCollection<uint>?>(TaskCreationOptions.RunContinuationsAsynchronously);
		var refreshCount = 0;
		var backend = new AsyncTestBackend
		{
			RequiresActiveIdsForStateChanges = true,
			GetActiveNotificationIdsAsyncHandler = () =>
			{
				if (Interlocked.Increment(ref refreshCount) == 1)
				{
					refreshStarted.SetResult();
					return firstRefresh.Task;
				}
				return Task.FromResult<IReadOnlyCollection<uint>?>(new HashSet<uint> { pending.Id });
			},
		};
		var manager = new AppNotificationManager(backend, persistence);

		manager.GetAll();
		await refreshStarted.Task;

		Assert.AreEqual(pending, new AppNotificationStateStore(persistence).GetPendingPostings().Single());
		Assert.AreEqual(0, backend.Removed.Count);

		firstRefresh.SetResult(new HashSet<uint> { pending.Id });
		var shown = await manager.GetAllAsync();

		CollectionAssert.AreEqual(new[] { pending.Id }, shown.Select(notification => notification.Id).ToArray());
		Assert.IsTrue(refreshCount >= 2);
	}

	[TestMethod]
	public async Task When_Persistent_Refresh_Confirms_Posting_Is_Absent_It_Is_Reconciled()
	{
		var pending = CreateStateRecord(7, AppNotificationPostingState.Posting) with
		{
			OperationOwner = "other-owner",
			OperationLeaseExpirationUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
		};
		var persistence = new InMemoryAppNotificationStatePersistence(new AppNotificationStateSnapshot(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			8,
			new[] { pending }));
		var backend = new AsyncTestBackend
		{
			RequiresActiveIdsForStateChanges = true,
			GetActiveNotificationIdsAsyncHandler = () => Task.FromResult<IReadOnlyCollection<uint>?>(Array.Empty<uint>()),
		};
		var manager = new AppNotificationManager(backend, persistence);

		var shown = await manager.GetAllAsync();

		Assert.AreEqual(0, shown.Count);
		Assert.AreEqual(0, new AppNotificationStateStore(persistence).GetAllRecords().Count);
	}

	[TestMethod]
	public async Task When_Persistent_Refresh_Cannot_Determine_Active_Ids_Posting_Is_Preserved()
	{
		var pending = CreateStateRecord(7, AppNotificationPostingState.Posting) with
		{
			OperationOwner = "other-owner",
			OperationLeaseExpirationUtc = DateTimeOffset.UtcNow.AddMinutes(-1),
		};
		var persistence = new InMemoryAppNotificationStatePersistence(new AppNotificationStateSnapshot(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			8,
			new[] { pending }));
		var backend = new AsyncTestBackend
		{
			RequiresActiveIdsForStateChanges = true,
			GetActiveNotificationIdsAsyncHandler = () => Task.FromResult<IReadOnlyCollection<uint>?>(null),
		};
		var manager = new AppNotificationManager(backend, persistence);

		var shown = await manager.GetAllAsync();

		Assert.AreEqual(0, shown.Count);
		Assert.AreEqual(pending, new AppNotificationStateStore(persistence).GetPendingPostings().Single());
		Assert.AreEqual(0, backend.Removed.Count);
	}

	[TestMethod]
	public async Task When_Persistent_Notification_Is_Removed_Active_Ids_Are_Refreshed_First()
	{
		var shown = CreateStateRecord(7, AppNotificationPostingState.Shown);
		var persistence = new InMemoryAppNotificationStatePersistence(new AppNotificationStateSnapshot(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			8,
			new[] { shown }));
		var backend = new AsyncTestBackend
		{
			RequiresActiveIdsForStateChanges = true,
			GetActiveNotificationIdsAsyncHandler = () =>
				Task.FromResult<IReadOnlyCollection<uint>?>(new HashSet<uint> { shown.Id }),
		};
		var manager = new AppNotificationManager(backend, persistence);

		await manager.RemoveByIdAsync(shown.Id);

		Assert.IsTrue(backend.ActiveIdRefreshCount >= 2);
		CollectionAssert.AreEqual(new[] { shown.Id }, backend.Removed.Select(record => record.Id).ToArray());
		Assert.AreEqual(0, new AppNotificationStateStore(persistence).GetAllRecords().Count);
	}

	[TestMethod]
	public void When_Show_Reconciles_Dismissed_Records_Before_Reserving()
	{
		var persistence = new InMemoryAppNotificationStatePersistence();
		var backend = new TestBackend();
		var manager = new AppNotificationManager(backend, persistence);
		var dismissed = CreateNotification("dismissed", "group");
		manager.Register();
		manager.Show(dismissed);
		backend.ActiveNotificationIds = new HashSet<uint>();

		var current = CreateNotification("current", "group");
		manager.Show(current);

		var recovered = new AppNotificationStateStore(persistence);
		CollectionAssert.AreEqual(new[] { current.Id }, recovered.GetShown().Select(record => record.Id).ToArray());
	}

	[TestMethod]
	public async Task When_Equal_Sequence_Update_Remains_Unresolved_Result_Is_NotFound()
	{
		var backend = new AsyncTestBackend();
		var manager = new AppNotificationManager(backend, new InMemoryAppNotificationStatePersistence());
		var notification = CreateNotification("progress", "group");
		manager.Register();
		manager.Show(notification);
		backend.ActiveNotificationIds = new HashSet<uint> { notification.Id };
		backend.AcceptUpdate = false;

		var first = await manager.UpdateAsync(new PublicAppNotificationProgressData(5), "progress", "group");
		var retry = await manager.UpdateAsync(new PublicAppNotificationProgressData(5), "progress", "group");

		Assert.AreEqual(AppNotificationProgressResult.AppNotificationNotFound, first);
		Assert.AreEqual(AppNotificationProgressResult.AppNotificationNotFound, retry);
		Assert.AreEqual(1, backend.Updated.Count);
	}

	[TestMethod]
	public void When_Deferred_Update_Is_Recovered_It_Remains_Pending_Until_Completion()
	{
		var pending = CreateStateRecord(7, AppNotificationPostingState.Updating) with
		{
			CreatedUtc = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromMinutes(2)),
			Progress = new AppNotificationProgressSnapshot(1, "Title", 0.5, "50%", "Running"),
		};
		var persistence = new InMemoryAppNotificationStatePersistence(new AppNotificationStateSnapshot(
			AppNotificationStateSnapshot.CurrentSchemaVersion,
			8,
			new[] { pending }));
		var backend = new DeferredTestBackend { ActiveNotificationIds = new HashSet<uint> { pending.Id } };
		var manager = new AppNotificationManager(backend, persistence);

		manager.GetAll();

		Assert.AreEqual(1, backend.Updated.Count);
		Assert.AreEqual(AppNotificationPostingState.Updating, new AppNotificationStateStore(persistence).GetPendingUpdates().Single().PostingState);
		var operation = backend.GetPendingOperations(pending.Id).Single();
		backend.CompleteOperation(operation, succeeded: true);
		Assert.AreEqual(AppNotificationPostingState.Shown, new AppNotificationStateStore(persistence).GetShown().Single().PostingState);
	}

	[TestMethod]
	public void When_Deferred_Show_Is_Pending_A_Subsequent_Show_Does_Not_Recover_It_As_Abandoned()
	{
		var persistence = new InMemoryAppNotificationStatePersistence();
		var backend = new DeferredTestBackend();
		var manager = new AppNotificationManager(backend, persistence);
		manager.Register();
		var first = CreateNotification("first", "group");
		var second = CreateNotification("second", "group");

		manager.Show(first);
		manager.Show(second);

		var state = new AppNotificationStateStore(persistence);
		CollectionAssert.AreEquivalent(new[] { first.Id, second.Id }, state.GetPendingPostings().Select(record => record.Id).ToArray());
		Assert.AreEqual(0, backend.Removed.Count);
	}

	[TestMethod]
	public void When_Deferred_Show_Fails_The_Pending_Record_Is_Aborted()
	{
		var persistence = new InMemoryAppNotificationStatePersistence();
		var backend = new DeferredTestBackend();
		var manager = new AppNotificationManager(backend, persistence);
		manager.Register();
		var notification = CreateNotification();
		manager.Show(notification);

		backend.CompleteShow(notification.Id, succeeded: false);

		var state = new AppNotificationStateStore(persistence);
		Assert.AreEqual(0, state.GetPendingPostings().Count);
		Assert.AreEqual(0, state.GetShown().Count);
	}

	[TestMethod]
	public void When_Deferred_Replacement_Fails_The_Previous_Payload_Is_Restored()
	{
		var persistence = new InMemoryAppNotificationStatePersistence();
		var backend = new DeferredTestBackend();
		var manager = new AppNotificationManager(backend, persistence);
		manager.Register();
		var original = CreateNotificationWithTitle("Original", "tag", "group");
		manager.Show(original);
		backend.CompleteShow(original.Id, succeeded: true);
		var replacement = CreateNotificationWithTitle("Replacement", "tag", "group");

		manager.ShowReplacingTagAndGroup(replacement);
		backend.CompleteShow(replacement.Id, succeeded: false);

		var record = new AppNotificationStateStore(persistence).GetShown().Single();
		Assert.AreEqual(original.Id, replacement.Id);
		StringAssert.Contains(record.Payload, "Original");
		Assert.IsFalse(record.Payload.Contains("Replacement", StringComparison.Ordinal));
	}

	[TestMethod]
	public async Task When_Deferred_Replacements_Overlap_Completions_Use_Unique_Correlation()
	{
		var persistence = new InMemoryAppNotificationStatePersistence();
		var backend = new DeferredTestBackend();
		var manager = new AppNotificationManager(backend, persistence);
		manager.Register();
		var original = CreateNotificationWithTitle("Original", "tag", "group");
		manager.Show(original);
		backend.CompleteShow(original.Id, succeeded: true);
		var firstReplacement = CreateNotificationWithTitle("First replacement", "tag", "group");
		var secondReplacement = CreateNotificationWithTitle("Second replacement", "tag", "group");

		manager.Show(firstReplacement);
		var firstOperation = backend.GetPendingOperations(original.Id).Single();
		manager.Show(secondReplacement);
		var secondOperation = backend.GetPendingOperations(original.Id).Single(operation => operation != firstOperation);
		var completion = backend.WaitForPendingShowsAsync();

		backend.CompleteOperation(firstOperation, succeeded: true);
		Assert.IsFalse(completion.IsCompleted);
		backend.CompleteOperation(secondOperation, succeeded: false);
		await completion;

		Assert.AreEqual(original.Id, firstReplacement.Id);
		Assert.AreEqual(original.Id, secondReplacement.Id);
		var record = new AppNotificationStateStore(persistence).GetShown().Single();
		StringAssert.Contains(record.Payload, "First replacement");
		Assert.IsFalse(record.Payload.Contains("Second replacement", StringComparison.Ordinal));
	}

	[TestMethod]
	public async Task When_Backend_Does_Not_Support_Progress_Update_Returns_Unsupported()
	{
		var persistence = new InMemoryAppNotificationStatePersistence();
		var backend = new DeferredTestBackend { SupportsProgressUpdates = false };
		var manager = new AppNotificationManager(backend, persistence);
		manager.Register();
		var notification = CreateNotification("progress", "group");
		manager.Show(notification);
		backend.CompleteShow(notification.Id, succeeded: true);

		var result = await manager.UpdateAsync(new PublicAppNotificationProgressData(1), "progress", "group");

		Assert.AreEqual(AppNotificationProgressResult.Unsupported, result);
		Assert.AreEqual(0, backend.Updated.Count);
	}

	[TestMethod]
	public async Task When_Async_Removal_Is_Not_Acknowledged_Durable_State_Remains()
	{
		var persistence = new InMemoryAppNotificationStatePersistence();
		var backend = new DeferredTestBackend();
		var manager = new AppNotificationManager(backend, persistence);
		manager.Register();
		var notification = CreateNotification();
		manager.Show(notification);
		backend.CompleteShow(notification.Id, succeeded: true);
		var acknowledgement = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		backend.RemoveAsyncHandler = _ => acknowledgement.Task;

		var removal = manager.RemoveByIdAsync(notification.Id).AsTask();
		Assert.AreEqual(1, new AppNotificationStateStore(persistence).GetShown().Count);
		acknowledgement.SetResult(false);
		await removal;

		CollectionAssert.AreEqual(new[] { notification.Id }, new AppNotificationStateStore(persistence).GetShown().Select(record => record.Id).ToArray());
		backend.RemoveAsyncHandler = _ => Task.FromResult(true);
		await manager.RemoveByIdAsync(notification.Id);
		Assert.AreEqual(0, new AppNotificationStateStore(persistence).GetShown().Count);
	}

	private static AppNotificationActivation CreateActivation(string argument)
		=> new(argument, new Dictionary<string, string>());

	private static AppNotification CreateNotification(string tag = "tag", string group = "group")
		=> new("<toast><visual><binding template='ToastGeneric'><text>Title</text></binding></visual></toast>")
		{
			Tag = tag,
			Group = group,
		};

	private static AppNotification CreateNotificationWithTitle(string title, string tag, string group)
		=> new($"<toast><visual><binding template='ToastGeneric'><text>{title}</text></binding></visual></toast>")
		{
			Tag = tag,
			Group = group,
		};

	private static AppNotificationStateRecord CreateStateRecord(uint id, AppNotificationPostingState state)
		=> new(
			id,
			"<toast><visual><binding template='ToastGeneric'><text>Title</text></binding></visual></toast>",
			"tag",
			"group",
			DateTimeOffset.UtcNow,
			DateTimeOffset.FromFileTime(0),
			false,
			null,
			AppNotificationPriority.Default,
			false,
			state,
			null);

	private class TestBackend : IAppNotificationManagerBackend
	{
		public bool IsSupported { get; set; } = true;

		public AppNotificationSetting Setting { get; set; } = AppNotificationSetting.Enabled;

		public string? BootIdentifier { get; set; } = "boot";

		public bool AcceptShow { get; set; } = true;

		public bool AcceptUpdate { get; set; } = true;

		public Exception? RegisterException { get; set; }

		public Exception? RemoveException { get; set; }

		public Exception? UnregisterAllException { get; set; }

		public Action? UnregisterAction { get; set; }

		public Action? UnregisterAllAction { get; set; }

		public List<string> Calls { get; } = new();

		public List<AppNotificationEnvelope> Shown { get; } = new();

		public List<AppNotificationStateRecord> Updated { get; } = new();

		public List<AppNotificationStateRecord> Removed { get; } = new();

		public IReadOnlyCollection<uint>? ActiveNotificationIds { get; set; }

		public void Register()
		{
			if (RegisterException is not null)
			{
				throw RegisterException;
			}
			Calls.Add("Register");
		}

		public void Register(string displayName, Uri iconUri) => Calls.Add($"Register:{displayName}:{iconUri}");

		public void Unregister()
		{
			Calls.Add("Unregister");
			UnregisterAction?.Invoke();
		}

		public void UnregisterAll()
		{
			if (UnregisterAllException is not null)
			{
				throw UnregisterAllException;
			}
			Calls.Add("UnregisterAll");
			UnregisterAllAction?.Invoke();
		}

		public bool TryShow(AppNotificationEnvelope notification)
		{
			Shown.Add(notification);
			return AcceptShow;
		}

		public bool TryUpdate(AppNotificationStateRecord notification)
		{
			Updated.Add(notification);
			return AcceptUpdate;
		}

		public void Remove(AppNotificationStateRecord notification)
		{
			if (RemoveException is not null)
			{
				throw RemoveException;
			}
			Removed.Add(notification);
		}

		public void RemoveAll() => Calls.Add("RemoveAll");

		public IReadOnlyCollection<uint>? GetActiveNotificationIds() => ActiveNotificationIds;
	}

	private sealed class AsyncTestBackend : TestBackend, IAsyncAppNotificationManagerBackend, IAppNotificationActiveIdRefreshCapability
	{
		public Func<Task<IReadOnlyCollection<uint>?>>? GetActiveNotificationIdsAsyncHandler { get; set; }

		public bool RequiresActiveIdsForStateChanges { get; set; }

		public int ActiveIdRefreshCount { get; private set; }

		public Task<bool> TryUpdateAsync(AppNotificationStateRecord notification)
			=> Task.FromResult(TryUpdate(notification));

		public Task<bool> RemoveAsync(AppNotificationStateRecord notification)
		{
			Remove(notification);
			return Task.FromResult(true);
		}

		public Task<bool> RemoveAllAsync()
		{
			RemoveAll();
			return Task.FromResult(true);
		}

		public Task<IReadOnlyCollection<uint>?> GetActiveNotificationIdsAsync()
		{
			ActiveIdRefreshCount++;
			return GetActiveNotificationIdsAsyncHandler?.Invoke() ?? Task.FromResult(ActiveNotificationIds);
		}
	}

	private sealed class DeferredTestBackend : IAppNotificationManagerBackend, IDeferredAppNotificationManagerBackend, IAsyncAppNotificationManagerBackend, IAppNotificationProgressUpdateCapability
	{
		private readonly Dictionary<string, (uint Id, TaskCompletionSource Completion)> _pendingShows = new(StringComparer.Ordinal);
		private Action<string, uint, bool>? _showCompleted;

		public bool IsSupported => true;

		public AppNotificationSetting Setting => AppNotificationSetting.Enabled;

		public string? BootIdentifier => null;

		public bool DefersShowCompletion => true;

		public bool SupportsProgressUpdates { get; set; } = true;

		public Func<AppNotificationStateRecord, Task<bool>> RemoveAsyncHandler { get; set; } = _ => Task.FromResult(true);

		public List<AppNotificationStateRecord> Updated { get; } = new();

		public List<AppNotificationStateRecord> Removed { get; } = new();

		public IReadOnlyCollection<uint>? ActiveNotificationIds { get; set; }

		public void Register()
		{
		}

		public void Register(string displayName, Uri iconUri)
		{
		}

		public void Unregister()
		{
		}

		public void UnregisterAll()
		{
		}

		public bool TryShow(AppNotificationEnvelope notification)
		{
			AddPendingShow(notification.Id, Guid.NewGuid().ToString("N"));
			return true;
		}

		public bool TryShow(AppNotificationEnvelope notification, string operationCorrelation)
		{
			AddPendingShow(notification.Id, operationCorrelation);
			return true;
		}

		public bool TryUpdate(AppNotificationStateRecord notification)
		{
			Updated.Add(notification);
			AddPendingShow(notification.Id, Guid.NewGuid().ToString("N"));
			return true;
		}

		public bool TryUpdate(AppNotificationStateRecord notification, string operationCorrelation)
		{
			Updated.Add(notification);
			AddPendingShow(notification.Id, operationCorrelation);
			return true;
		}

		public void Remove(AppNotificationStateRecord notification) => Removed.Add(notification);

		public void RemoveAll()
		{
		}

		public IReadOnlyCollection<uint>? GetActiveNotificationIds() => ActiveNotificationIds;

		public bool IsShowPending(uint id) => _pendingShows.Values.Any(pending => pending.Id == id);

		public Task WaitForPendingShowsAsync()
			=> _pendingShows.Count == 0 ? Task.CompletedTask : Task.WhenAll(_pendingShows.Values.Select(pending => pending.Completion.Task));

		public void SetShowCompletedHandler(Action<string, uint, bool> handler) => _showCompleted = handler;

		public Task<bool> TryUpdateAsync(AppNotificationStateRecord notification)
		{
			Updated.Add(notification);
			return Task.FromResult(true);
		}

		public async Task<bool> RemoveAsync(AppNotificationStateRecord notification)
		{
			Removed.Add(notification);
			return await RemoveAsyncHandler(notification);
		}

		public Task<bool> RemoveAllAsync() => Task.FromResult(true);

		public Task<IReadOnlyCollection<uint>?> GetActiveNotificationIdsAsync() => Task.FromResult(ActiveNotificationIds);

		public void CompleteShow(uint id, bool succeeded)
		{
			var operations = GetPendingOperations(id);
			if (operations.Count != 1)
			{
				Assert.Fail($"Notification {id} has {operations.Count} pending operations.");
			}
			CompleteOperation(operations[0], succeeded);
		}

		public IReadOnlyList<string> GetPendingOperations(uint id)
			=> _pendingShows
				.Where(pending => pending.Value.Id == id)
				.Select(pending => pending.Key)
				.ToArray();

		public void CompleteOperation(string operationCorrelation, bool succeeded)
		{
			if (!_pendingShows.Remove(operationCorrelation, out var pending))
			{
				Assert.Fail($"Operation {operationCorrelation} is not pending.");
			}
			try
			{
				_showCompleted?.Invoke(operationCorrelation, pending.Id, succeeded);
			}
			finally
			{
				pending.Completion.SetResult();
			}
		}

		private void AddPendingShow(uint id, string operationCorrelation)
			=> _pendingShows.Add(
				operationCorrelation,
				(id, new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously)));
	}
}

#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_LinuxAppNotificationNativeStateStore
{
	[TestMethod]
	public void When_Server_Owner_Changes_Stale_Native_Ids_Are_Cleared()
	{
		var store = CreateStore();
		Assert.IsTrue(store.SetServerOwner(":1.10"));
		store.Set(1, 100);

		Assert.IsTrue(store.SetServerOwner(":1.11"));

		Assert.IsNull(store.GetNativeId(1));
		Assert.AreEqual(0, store.GetAll().Count);
	}

	[TestMethod]
	public void When_Server_Owner_Is_Unchanged_Mappings_Are_Preserved()
	{
		var store = CreateStore();
		store.SetServerOwner(":1.10");
		store.Set(1, 100);

		Assert.IsFalse(store.SetServerOwner(":1.10"));

		Assert.AreEqual((uint)100, store.GetNativeId(1));
	}

	[TestMethod]
	public void When_Mapping_Is_Replaced_Both_Directions_Remain_Unique()
	{
		var store = CreateStore();
		store.Set(1, 100);
		store.Set(2, 100);

		Assert.IsNull(store.GetNativeId(1));
		Assert.AreEqual((uint)100, store.GetNativeId(2));
		Assert.AreEqual((uint)2, store.GetNotificationId(100));
	}

	[TestMethod]
	public void When_Native_Notification_Closes_Mapping_Is_Removed()
	{
		var store = CreateStore();
		store.Set(7, 42);

		Assert.IsTrue(store.RemoveByNativeId(42));

		Assert.IsNull(store.GetNativeId(7));
		Assert.IsFalse(store.RemoveByNativeId(42));
	}

	[TestMethod]
	public void When_Invalid_Persisted_State_Is_Loaded_Store_Starts_Empty()
	{
		var persistence = new InMemoryLinuxAppNotificationNativeStatePersistence(new LinuxAppNotificationNativeStateSnapshot(
			LinuxAppNotificationNativeStateSnapshot.CurrentSchemaVersion,
			":1.10",
			new[]
			{
				new LinuxAppNotificationNativeIdRecord(1, 100),
				new LinuxAppNotificationNativeIdRecord(1, 101),
			}));

		var store = new LinuxAppNotificationNativeStateStore(persistence);

		Assert.AreEqual(0, store.GetAll().Count);
	}

	private static LinuxAppNotificationNativeStateStore CreateStore()
		=> new(new InMemoryLinuxAppNotificationNativeStatePersistence());
}
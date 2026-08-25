#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Windows.AppNotifications.Internal;

namespace Uno.UI.Tests.Microsoft_Windows_AppNotifications;

[TestClass]
public class Given_LinuxAppNotificationNativeStateStore
{
	[TestMethod]
	public void When_A_New_Session_Starts_With_The_Same_Server_Owner_Previous_Mappings_Are_Cleared()
	{
		var store = new LinuxAppNotificationNativeStateStore();
		using var previous = store.StartSession(":1.10");
		Assert.IsTrue(previous.TrySet(1, 100));

		using var current = store.StartSession(":1.10");

		Assert.IsFalse(previous.IsActive);
		Assert.IsNull(current.GetNativeId(1));
		Assert.AreEqual(0, current.GetAll().Count);
	}

	[TestMethod]
	public void When_A_Session_Ends_Its_Mappings_Cannot_Be_Read_Or_Restored()
	{
		var store = new LinuxAppNotificationNativeStateStore();
		var previous = store.StartSession(":1.10");
		Assert.IsTrue(previous.TrySet(1, 100));

		previous.Dispose();

		Assert.IsFalse(previous.IsActive);
		Assert.IsNull(previous.GetNativeId(1));
		Assert.IsFalse(previous.TrySet(2, 200));
		using var current = store.StartSession(":1.10");
		Assert.AreEqual(0, current.GetAll().Count);
	}

	[TestMethod]
	public void When_A_Session_Is_Live_Its_Action_Command_Is_Available_Only_To_That_Session()
	{
		var store = new LinuxAppNotificationNativeStateStore();
		using var previous = store.StartSession(":1.10");
		var command = CreateCommand(1);
		Assert.IsTrue(previous.TrySet(1, 100, command));
		Assert.AreSame(command, previous.GetCommand(100));

		using var current = store.StartSession(":1.10");

		Assert.IsNull(previous.GetCommand(100));
		Assert.IsNull(current.GetCommand(100));
	}

	[TestMethod]
	public void When_A_Stale_Session_Receives_A_Closure_It_Cannot_Remove_Current_State()
	{
		var store = new LinuxAppNotificationNativeStateStore();
		using var previous = store.StartSession(":1.10");
		Assert.IsTrue(previous.TrySet(1, 100));
		using var current = store.StartSession(":1.10");
		Assert.IsTrue(current.TrySet(2, 100));

		Assert.IsFalse(previous.RemoveByNativeId(100));

		Assert.AreEqual((uint)100, current.GetNativeId(2));
		Assert.AreEqual((uint)2, current.GetNotificationId(100));
	}

	[TestMethod]
	public void When_Mapping_Is_Replaced_Both_Directions_Remain_Unique()
	{
		var store = new LinuxAppNotificationNativeStateStore();
		using var session = store.StartSession(":1.10");
		session.TrySet(1, 100);
		session.TrySet(2, 100);

		Assert.IsNull(session.GetNativeId(1));
		Assert.AreEqual((uint)100, session.GetNativeId(2));
		Assert.AreEqual((uint)2, session.GetNotificationId(100));
	}

	[TestMethod]
	public void When_Native_Notification_Closes_Mapping_Is_Removed()
	{
		var store = new LinuxAppNotificationNativeStateStore();
		using var session = store.StartSession(":1.10");
		var command = CreateCommand(7);
		session.TrySet(7, 42, command);
		Assert.AreSame(command, session.GetCommand(42));

		Assert.IsTrue(session.RemoveByNativeId(42));

		Assert.IsNull(session.GetNativeId(7));
		Assert.IsNull(session.GetCommand(42));
		Assert.IsFalse(session.RemoveByNativeId(42));
	}

	private static LinuxAppNotificationCommand CreateCommand(uint id)
		=> new(
			id,
			string.Empty,
			string.Empty,
			string.Empty,
			string.Empty,
			1,
			-1,
			false,
			false,
			null,
			LinuxAppNotificationTranslator.BodyActionKey,
			string.Empty,
			null,
			Array.Empty<LinuxAppNotificationActionCommand>(),
			Array.Empty<string>());
}

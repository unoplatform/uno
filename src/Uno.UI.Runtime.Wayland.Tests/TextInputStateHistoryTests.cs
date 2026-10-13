#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class TextInputStateHistoryTests
{
	[TestMethod]
	public void UnacknowledgedCommitsRetainOnlyTheLast64States()
	{
		var history = new WaylandTextInputStateHistory<object>(64);
		var host = new object();
		for (uint serial = 1; serial <= 100; serial++)
		{
			history.Record(serial, new(1, host, null, serial.ToString(), serial));
		}
		Assert.IsFalse(history.TryGetValue(36, out _));
		Assert.IsTrue(history.TryGetValue(37, out var first));
		Assert.AreEqual("37", first.Text);
		Assert.IsTrue(history.TryGetValue(100, out var last));
		Assert.AreSame(host, last.Host);
	}

	[TestMethod]
	public void EvictedSnapshotDoesNotInvalidateCurrentSessionSerial()
	{
		var history = new WaylandTextInputStateHistory<object>(64);
		var host = new object();
		for (uint serial = 10; serial <= 100; serial++)
		{
			history.Record(serial, new(1, host, null, string.Empty, 0));
		}
		Assert.IsFalse(history.TryGetValue(10, out _));
		var state = history.GetStateOrCurrent(10, 1, host);
		Assert.AreSame(host, state.Host);
		Assert.AreEqual(1L, state.Session);
		Assert.IsNull(state.Surrounding);
		Assert.AreEqual(-1L, state.Revision);
		Assert.IsTrue(WaylandTextInputBatch.IsSerialInSession(10, 10, 100));
		Assert.IsFalse(WaylandTextInputBatch.IsSerialInSession(9, 10, 100));
		Assert.IsFalse(WaylandTextInputBatch.IsSerialInSession(101, 10, 100));
		Assert.IsFalse(WaylandTextInputBatch.IsSerialInSession(9, 10, 9));
		Assert.IsTrue(WaylandTextInputBatch.IsSerialInSession(uint.MaxValue, uint.MaxValue - 1, 2));
		Assert.IsTrue(WaylandTextInputBatch.IsSerialInSession(0, uint.MaxValue - 1, 2));
		Assert.IsFalse(WaylandTextInputBatch.IsSerialInSession(uint.MaxValue - 2, uint.MaxValue - 1, 2));
	}

	[TestMethod]
	public void SerialWrapDoesNotEvictRecentStates()
	{
		var history = new WaylandTextInputStateHistory<object>(64);
		var host = new object();
		var serial = uint.MaxValue - 32;
		for (var i = 0; i < 100; i++, serial = unchecked(serial + 1))
		{
			history.Record(serial, new(1, host, null, i.ToString(), i));
		}
		Assert.IsFalse(history.TryGetValue(uint.MaxValue, out _));
		Assert.IsTrue(history.TryGetValue(3, out var first));
		Assert.AreEqual("36", first.Text);
		Assert.IsTrue(history.TryGetValue(66, out var last));
		Assert.AreEqual("99", last.Text);
	}

	[TestMethod]
	public void RepeatedDoneUpdatesItsSnapshotWithoutConsumingHistory()
	{
		var history = new WaylandTextInputStateHistory<object>(64);
		var host = new object();
		history.Record(1, new(1, host, null, "old", 0));
		for (var i = 0; i < 100; i++)
		{
			history.Record(2, new(1, host, null, "updated", i));
		}
		Assert.IsTrue(history.TryGetValue(1, out _));
		Assert.IsTrue(history.TryGetValue(2, out var state));
		Assert.AreEqual("updated", state.Text);
		Assert.AreEqual(99L, state.Revision);
	}

	[TestMethod]
	public void LatestDoneDiscardsOlderSessionsAndKeepsItsSnapshot()
	{
		var history = new WaylandTextInputStateHistory<object>(64);
		var oldHost = new object();
		var newHost = new object();
		history.Record(1, new(1, oldHost, null, "old", 0));
		history.Record(2, new(2, newHost, new("new", 0, 3, 3), "new", 1));
		history.RetainOnly(2);
		Assert.IsFalse(history.TryGetValue(1, out _));
		Assert.IsTrue(history.TryGetValue(2, out var state));
		Assert.AreSame(newHost, state.Host);
		Assert.AreEqual(2L, state.Session);
		Assert.AreEqual(3, state.Surrounding!.Value.Cursor);
		history.Clear();
		Assert.IsFalse(history.TryGetValue(2, out _));
	}
}

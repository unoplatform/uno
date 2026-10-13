#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Wayland;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class SelectionSerialTests
{
	[TestMethod]
	public void CancelledDeferredSelectionIsNotPublishedOnFocus()
	{
		var state = new WaylandSelectionSerial();
		var published = 0;
		var cancelled = 0;
		state.Request(() => published++, () => cancelled++);
		state.CancelPendingSelection();
		state.CancelPendingSelection();
		Assert.AreEqual(1, cancelled);
		state.Enter(11);
		state.Update(12);
		Assert.AreEqual(0, published);
		state.Request(() => published++);
		Assert.AreEqual(1, published);
	}

	[TestMethod]
	public void SelectionRequiresCurrentKeyboardFocus()
	{
		var state = new WaylandSelectionSerial();
		state.Update(10);
		Assert.IsFalse(state.HasFocus);
		state.Enter(11);
		Assert.IsTrue(state.HasFocus);
		Assert.AreEqual(11u, state.Serial);
		state.Update(12);
		Assert.AreEqual(12u, state.Serial);
		state.Leave();
		state.Update(13);
		Assert.IsFalse(state.HasFocus);
		Assert.AreEqual(12u, state.Serial);
		state.Enter(14);
		Assert.AreEqual(14u, state.Serial);
	}

	[TestMethod]
	public void DeferredSelectionPublishesOnlyLatestRequestOnFocus()
	{
		var state = new WaylandSelectionSerial();
		var published = 0;
		state.Request(() => published = 1);
		state.Request(() => published = 2);
		Assert.AreEqual(0, published);
		state.Update(10);
		Assert.AreEqual(0, published);
		state.Enter(11);
		Assert.AreEqual(2, published);
		state.Update(12);
		Assert.AreEqual(2, published);
		state.Leave();
		state.Request(() => published = 3);
		Assert.AreEqual(2, published);
		state.Enter(13);
		Assert.AreEqual(3, published);
		state.Request(() => published = 4);
		Assert.AreEqual(4, published);
	}

	[TestMethod]
	public void WrappedSerialZeroIsValidWithFocus()
	{
		var state = new WaylandSelectionSerial();
		state.Enter(uint.MaxValue);
		state.Update(0);
		Assert.IsTrue(state.HasFocus);
		Assert.AreEqual(0u, state.Serial);
	}
}

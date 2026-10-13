#nullable enable

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Core;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class WindowActivationTests
{
	[TestMethod]
	public void ConfigureBeforeLoadedDefersCloseUntilAfterWindowEnumeration()
	{
		var ui = new Queue<Action>();
		var windows = new List<int> { 1, 13 };
		var events = new List<CoreWindowActivationState>();
		var closed = false;
		void Notify(CoreWindowActivationState state)
		{
			if (state == CoreWindowActivationState.CodeActivated)
			{
				events.Add(state);
				closed = true;
				windows.Remove(13);
			}
		}
		var activation = new WaylandWindowActivation(ui.Enqueue, () => !closed, Notify);
		activation.Configure(WaylandToplevelStates.None, notifyActivation: true);
		foreach (var window in windows)
		{
			if (window == 13)
			{
				// ContentManager.Loaded -> TryActivate -> Show(true) -> host Activate.
				activation.Request();
				if (activation.IsReadyForActivation)
				{
					Notify(CoreWindowActivationState.CodeActivated);
				}
			}
		}
		Assert.IsFalse(closed, "TryActivate must not raise activation inline.");
		Assert.AreEqual(1, ui.Count);
		ui.Dequeue()();
		Assert.IsTrue(closed);
		CollectionAssert.AreEqual(new[] { 1 }, windows);
		CollectionAssert.AreEqual(new[] { CoreWindowActivationState.CodeActivated }, events);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void ActivationWaitsForConfigureAndDoesNotRepeatInPostedTurn(bool configureBeforePostedTurn)
	{
		var ui = new Queue<Action>();
		var events = new List<CoreWindowActivationState>();
		var activation = new WaylandWindowActivation(ui.Enqueue, () => true, events.Add);
		activation.Request();
		if (!configureBeforePostedTurn)
		{
			ui.Dequeue()();
			Assert.AreEqual(0, events.Count, "Unconfigured bounds must not be observable from activation.");
		}
		activation.Configure(WaylandToplevelStates.Activated, notifyActivation: true);
		if (configureBeforePostedTurn)
		{
			ui.Dequeue()();
		}
		CollectionAssert.AreEqual(new[] { CoreWindowActivationState.CodeActivated, CoreWindowActivationState.PointerActivated }, events);
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void CloseSuppressesQueuedActivationAndRemainderOfInitialSequence(bool closeInActivation)
	{
		var ui = new Queue<Action>();
		var events = new List<CoreWindowActivationState>();
		var closed = false;
		var activation = new WaylandWindowActivation(ui.Enqueue, () => !closed, state =>
		{
			events.Add(state);
			closed = true;
		});
		activation.Configure(WaylandToplevelStates.Activated, notifyActivation: false);
		activation.Request();
		closed = !closeInActivation;
		ui.Dequeue()();
		CollectionAssert.AreEqual(closeInActivation ? new[] { CoreWindowActivationState.CodeActivated } : Array.Empty<CoreWindowActivationState>(), events);
	}

	[TestMethod]
	public void LaterFocusConfiguresDoNotRepeatInitialCodeActivation()
	{
		var ui = new Queue<Action>();
		var events = new List<CoreWindowActivationState>();
		var activation = new WaylandWindowActivation(ui.Enqueue, () => true, events.Add);
		activation.Configure(WaylandToplevelStates.None, notifyActivation: false);
		activation.Request();
		ui.Dequeue()();
		activation.Configure(WaylandToplevelStates.Activated, notifyActivation: true);
		activation.Configure(WaylandToplevelStates.None, notifyActivation: true);
		CollectionAssert.AreEqual(new[]
		{
			CoreWindowActivationState.CodeActivated, CoreWindowActivationState.PointerActivated, CoreWindowActivationState.Deactivated
		}, events);
	}
}

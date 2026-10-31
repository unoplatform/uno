#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Dispatching;

namespace Uno.UI.Tests.Uno_UI_Dispatching;

[UITestClass]
public class Given_NativeDispatcher
{
	// A UI thread busy on every frame: the next render is already requested when one ends, and each frame queues
	// its layout/Rendering tick at Normal (CoreServices.RequestAdditionalFrame).
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24032")]
	[DataRow((int)NativeDispatcherPriority.Low)]
	[DataRow((int)NativeDispatcherPriority.Idle)]
	public void When_Every_Frame_Is_Busy_Then_Low_Priority_Work_Still_Runs(int priorityValue)
	{
		var priority = (NativeDispatcherPriority)priorityValue;
		using var pump = new DispatcherPump();

		var renders = 0;
		var ran = false;

		pump.Dispatcher.Enqueue(() => ran = true, priority);
		pump.StartRenderLoop(() => renders++);

		pump.Run(100, () => ran);

		Assert.IsTrue(ran, $"{priority} work never ran over {renders} frames.");
		Assert.IsTrue(renders <= 5, $"{priority} work waited {renders} frames.");

		var rendersWhenItRan = renders;
		pump.Run(10, () => false);

		Assert.IsTrue(renders > rendersWhenItRan, "Rendering stopped once the queued work ran.");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24032")]
	public void When_Idle_Queue_Is_Deep_Then_Rendering_Keeps_Going()
	{
		using var pump = new DispatcherPump();

		var renders = 0;
		var idleItems = 0;

		for (var i = 0; i < 50; i++)
		{
			pump.Dispatcher.Enqueue(() => idleItems++, NativeDispatcherPriority.Idle);
		}

		pump.StartRenderLoop(() => renders++);
		pump.Run(100, () => false);

		Assert.IsTrue(renders >= 20, $"Rendering was held back by the idle backlog ({renders} frames, {idleItems} idle items).");
		Assert.IsTrue(idleItems >= 5, $"Idle work was starved by rendering ({idleItems} idle items over {renders} frames).");
	}

	[TestMethod]
	public void When_Not_Rendering_Then_Priority_Order_Is_Kept()
	{
		using var pump = new DispatcherPump();

		var order = new List<NativeDispatcherPriority>();
		foreach (var priority in new[] { NativeDispatcherPriority.Idle, NativeDispatcherPriority.Low, NativeDispatcherPriority.Normal, NativeDispatcherPriority.High })
		{
			pump.Dispatcher.Enqueue(() => order.Add(priority), priority);
		}

		pump.Run(10, () => false);

		CollectionAssert.AreEqual(
			new[] { NativeDispatcherPriority.High, NativeDispatcherPriority.Normal, NativeDispatcherPriority.Low, NativeDispatcherPriority.Idle },
			order);
	}

	/// <summary>
	/// Owns the dispatcher's native pump for the duration of a test, so dispatch order is deterministic.
	/// </summary>
	private sealed class DispatcherPump : IDisposable
	{
		private readonly Action<Action, NativeDispatcherPriority> _previousDispatch;
		private readonly Func<bool> _previousHasThreadAccess;
		private readonly Queue<Action> _pending = new();
		private readonly object _renderTarget = new();

		private Action? _onRender;
		private bool _renderLoopRunning;
		private bool _tickQueued;

		public DispatcherPump()
		{
			_previousDispatch = NativeDispatcher.DispatchOverride;
			_previousHasThreadAccess = NativeDispatcher.HasThreadAccessOverride;

			NativeDispatcher.HasThreadAccessOverride = () => true;
			NativeDispatcher.DispatchOverride = (action, _) => _pending.Enqueue(action);

			Dispatcher.RemoveCompositionTargets(_ => true);

			var primed = false;
			Dispatcher.Enqueue(() => primed = true);
			Drain();

			Assert.IsTrue(primed, "Another test left work pending on the dispatcher.");
		}

		public NativeDispatcher Dispatcher => NativeDispatcher.Main;

		public void StartRenderLoop(Action onRender)
		{
			_onRender = onRender;
			_renderLoopRunning = true;

			Dispatcher.EnqueueRender(_renderTarget, OnRender);
		}

		/// <summary>Runs at most <paramref name="maxTurns"/> dispatcher turns, stopping early when <paramref name="until"/> is met.</summary>
		public void Run(int maxTurns, Func<bool> until)
		{
			for (var turns = 0; turns < maxTurns && _pending.Count > 0 && !until(); turns++)
			{
				_pending.Dequeue()();
			}
		}

		public void Dispose()
		{
			try
			{
				_renderLoopRunning = false;
				Dispatcher.RemoveCompositionTargets(target => ReferenceEquals(target, _renderTarget));

				// The dispatcher is a process-wide singleton: items left queued would keep its count elevated and
				// break the posting of every later test.
				Drain();
			}
			finally
			{
				NativeDispatcher.DispatchOverride = _previousDispatch;
				NativeDispatcher.HasThreadAccessOverride = _previousHasThreadAccess;
			}
		}

		private void OnRender()
		{
			_onRender?.Invoke();

			if (!_tickQueued)
			{
				_tickQueued = true;
				Dispatcher.Enqueue(() => _tickQueued = false, NativeDispatcherPriority.Normal);
			}

			if (_renderLoopRunning)
			{
				Dispatcher.EnqueueRender(_renderTarget, OnRender);
			}
		}

		private void Drain() => Run(1000, () => false);
	}
}

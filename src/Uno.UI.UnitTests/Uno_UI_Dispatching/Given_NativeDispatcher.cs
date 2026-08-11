#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Dispatching;

namespace Uno.UI.Tests.Uno_UI_Dispatching
{
	[TestClass]
	public class Given_NativeDispatcher
	{
		private const int MaxTurns = 100;

		/// <summary>
		/// On a host without a frame pacer a render action is pending on nearly every dispatcher turn, and each
		/// frame posts CompositionTarget.RaiseRendering at High priority. Renders outrank every queue, so queued
		/// work must still be let through or anything awaiting RunIdleAsync never completes.
		/// </summary>
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24032")]
		public void When_Render_Loop_Is_Active_Then_Idle_Work_Runs()
		{
			using var pump = new DispatcherPump();

			var renders = 0;
			var idleRan = false;

			pump.Dispatcher.Enqueue(() => idleRan = true, NativeDispatcherPriority.Idle);
			pump.StartRenderLoop(() => renders++);

			var turns = pump.Run(MaxTurns, () => idleRan);

			Assert.IsTrue(idleRan, $"Idle work never ran ({renders} render actions over {turns} dispatcher turns).");

			var rendersWhenIdleRan = renders;
			pump.Run(10, () => false);

			Assert.IsTrue(renders > rendersWhenIdleRan, "Rendering stopped once queued work was let through.");
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24032")]
		public void When_Idle_Queue_Is_Deep_Then_Rendering_Is_Not_Starved()
		{
			using var pump = new DispatcherPump();

			var renders = 0;
			var idleItems = 0;

			for (var i = 0; i < 20; i++)
			{
				pump.Dispatcher.Enqueue(() => idleItems++, NativeDispatcherPriority.Idle);
			}

			pump.StartRenderLoop(() => renders++);
			pump.Run(30, () => false);

			Assert.IsTrue(renders >= 4, $"Rendering was starved by the idle backlog ({renders} renders, {idleItems} idle items).");
			Assert.IsTrue(idleItems >= 4, $"Idle work was starved by rendering ({idleItems} idle items, {renders} renders).");
		}

		/// <summary>
		/// Owns the dispatcher's native pump for the duration of a test, so dispatch order is fully deterministic.
		/// </summary>
		private sealed class DispatcherPump : IDisposable
		{
			private readonly Action<Action, NativeDispatcherPriority> _previousDispatch;
			private readonly Func<bool> _previousHasThreadAccess;
			private readonly Queue<Action> _pending = new();
			private readonly object _renderTarget = new();

			private Action? _onRender;
			private bool _renderLoopRunning;
			private bool _raiseRenderingScheduled;

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
			public int Run(int maxTurns, Func<bool> until)
			{
				var turns = 0;
				while (turns < maxTurns && _pending.Count > 0 && !until())
				{
					_pending.Dequeue()();
					turns++;
				}

				return turns;
			}

			public void Dispose()
			{
				try
				{
					_renderLoopRunning = false;
					Dispatcher.RemoveCompositionTargets(target => ReferenceEquals(target, _renderTarget));

					// The dispatcher is a process-wide singleton: leaving items queued would keep _globalCount
					// elevated and break the 0->1 posting transition for every later test.
					Drain();
				}
				finally
				{
					NativeDispatcher.DispatchOverride = _previousDispatch;
					NativeDispatcher.HasThreadAccessOverride = _previousHasThreadAccess;
				}
			}

			// Models an unpaced host: the frame is drawn on the dispatcher thread, so the next frame is requested
			// straight away, and each recorded frame posts CompositionTarget.RaiseRendering at High priority.
			private void OnRender()
			{
				_onRender?.Invoke();

				if (!_raiseRenderingScheduled)
				{
					_raiseRenderingScheduled = true;
					Dispatcher.Enqueue(() => _raiseRenderingScheduled = false, NativeDispatcherPriority.High);
				}

				if (_renderLoopRunning)
				{
					Dispatcher.EnqueueRender(_renderTarget, OnRender);
				}
			}

			private void Drain() => Run(1000, () => false);
		}
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BuildTreeService_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using System;
using System.Collections.Generic;
using Windows.UI.Core;

namespace DirectUI
{
	internal class BuildTreeService
	{
		private readonly List<WeakReference<ITreeBuilder>> _activeWorkers = new();
		private readonly List<WeakReference<ITreeBuilder>> _suspendedWorkers = new();
		private bool _isDrainScheduled;

		internal int ActiveWorkersCount => _activeWorkers.Count;

		internal bool HasSuspendedWorkers => _suspendedWorkers.Count > 0;

		public void RegisterWork(ITreeBuilder treeBuildingElement)
		{
			_activeWorkers.Add(new WeakReference<ITreeBuilder>(treeBuildingElement));

			treeBuildingElement.IsRegisteredForCallbacks = true;

			// Request another UI thread frame to handle the new build tree work
			ScheduleDrain();
		}

		// TODO Uno: WinUI calls BuildTrees once per UI tick. Uno has no such tick, so a Low priority item stands in,
		// which leaves input and Normal items to run between two passes.
		private void ScheduleDrain()
		{
			if (!_isDrainScheduled)
			{
				_isDrainScheduled = true;
				var isEnqueuing = true;
				_ = CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Low, () =>
				{
					_isDrainScheduled = false;

					DXamlCore.Current.GetBudgetManager().StoreFrameTime();

					// A dispatcher running this inline has no tick between passes: rescheduling would recurse forever.
					if (BuildTrees() && !isEnqueuing)
					{
						ScheduleDrain();
					}
				});
				isEnqueuing = false;
			}
		}

		// Uno equivalent of the tick looking at the suspended queue: a worker leaves it after a change that goes through layout.
		internal void OnLayoutTick()
		{
			if (HasSuspendedWorkers)
			{
				ScheduleDrain();
			}
		}

		internal bool BuildTrees()
		{
			// workers are in a vector. we will process a worker until our policy says otherwise.
			// that policy is decided by the worker. It is keeping its own curated list of work that it will
			// perform until it decides that enough is enough.
			// Since we do not expect multiple workers ever, and work is temporarily, we just pop from the back (fast) and
			// perform work until the worker yields. When it yields he can report whether there is more work to be done.
			// If not, we continue to the next worker, otherwise we insert the worker again, but this time at index 0 and stop
			// working. This allows another worker to get a chance next tick.
			// Summary: This service is relying on the worker to indicate when too much work has been done

			List<WeakReference<ITreeBuilder>>? workersToBeSuspended = null;

			// 1. check the workers in active queue, try to perform BuildTree on them
			while (_activeWorkers.Count > 0)
			{
				var weakWorker = _activeWorkers[^1];

				_activeWorkers.RemoveAt(_activeWorkers.Count - 1); // it's gone

				if (!weakWorker.TryGetTarget(out var worker))
				{
					// well, it was a weak ref, so it can be gone now.
					continue;
				}

				// check if this worker can run buildtree now.
				if (worker.IsBuildTreeSuspended)
				{
					(workersToBeSuspended ??= new()).Add(weakWorker);
					continue;
				}

				worker.IsRegisteredForCallbacks = false;

				if (worker.BuildTree())
				{
					// worker could have reregistered
					if (!worker.IsRegisteredForCallbacks)
					{
						// returning him to the pool, but at the beginning, so that
						// another worker will get a chance in the next tick
						_activeWorkers.Insert(0, weakWorker);
						worker.IsRegisteredForCallbacks = true;
					}

					// the worker yielded but has more work to do, this is a signal for us to stop
					// note how that also limits the cost of the relatively slow insert call above.
					break;
				}
			}

			// 2. if there is no worker left in active queue, let's check if any suspended workers that not longer suspended.
			for (var i = _suspendedWorkers.Count; i > 0; --i)
			{
				var weakWorker = _suspendedWorkers[i - 1];

				if (!weakWorker.TryGetTarget(out var worker))
				{
					// well, it was a weak ref, so it can be gone now.
					_suspendedWorkers.RemoveAt(i - 1);
					continue;
				}

				if (worker.IsBuildTreeSuspended)
				{
					continue;
				}

				// remove it from suspended queue and put it back to active queue.
				_suspendedWorkers.RemoveAt(i - 1);
				// put it at the end so it will be picked up first in next tick.
				_activeWorkers.Add(weakWorker);
			}

			// 3. move all workers that are suspended in this tick to the suspended queue.
			if (workersToBeSuspended is not null)
			{
				_suspendedWorkers.AddRange(workersToBeSuspended);
			}

			// requests additional UI tick only when active queue is not empty.
			return _activeWorkers.Count > 0;
		}

		internal void ClearWork()
		{
			ClearWork(_activeWorkers);
			ClearWork(_suspendedWorkers);

			static void ClearWork(List<WeakReference<ITreeBuilder>> workers)
			{
				while (workers.Count > 0)
				{
					var weakWorker = workers[^1];

					workers.RemoveAt(workers.Count - 1); // it's gone

					if (weakWorker.TryGetTarget(out var worker))
					{
						worker.IsRegisteredForCallbacks = false;

						worker.ShutDownDeferredWork();
					}
				}
			}
		}
	}
}

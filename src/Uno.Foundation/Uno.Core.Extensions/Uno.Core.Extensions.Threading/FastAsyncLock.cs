#nullable enable

// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Threading
{
	/// <summary>
	/// An re-entrant asynchronous lock, that can be used in conjuction with C# async/await
	/// </summary>
	public sealed class FastAsyncLock
	{
		private readonly AsyncLocal<AsyncLocalMonitor> _localMonitor = new AsyncLocal<AsyncLocalMonitor>();

		private AsyncLocalMonitor? _tail;

		/// <summary>
		/// Acquires the lock, then provides a disposable to release it.
		/// </summary>
		/// <param name="ct">A cancellation token to cancel the acquisition of the lock</param>
		/// <returns>An IDisposable instance that allows the release of the lock.</returns>
		public Task<IDisposable> LockAsync(CancellationToken ct)
		{
			var monitor = _localMonitor.Value;
			if (monitor?.TryReEnterSync() ?? false)
			{
				// If the node from the async local is not null, it means that the current 'ExecutionContext' already acquired the lock.
				// Check if we can re-enter (if the lock was not released yet), and if so continue with current monitor.
				return Task.FromResult<IDisposable>(new Handle(monitor));
			}

			// Creates a new monitor and set it on current 'ExecutionContext' to allow re-entrency
			_localMonitor.Value = monitor = new AsyncLocalMonitor(this);

			// Then enqueue this new monitor
			var previous = Interlocked.Exchange(ref _tail, monitor);
			if (previous?.TryEnqueue(monitor) ?? false)
			{
				// The lock was already aquired by someone else, add us into the queue
				return monitor.EnterAsync(ct);
			}
			else 
			{
				// The waiting queue was empty (or the previous node is already completed)
				// Tt means that we sucessfully acquired the lock
				return Task.FromResult(monitor.EnterSync());
			}
		}

		private class AsyncLocalMonitor
		{
			private readonly object _exitGate = new object();
			private readonly FastAsyncLock _owner;

			private int _state = State.Waiting;
			private FastTaskCompletionSource<IDisposable>? _enterAsync;
			private int _count;
			private AsyncLocalMonitor? _next;

			private static class State
			{
				public const int Waiting = 0;
				public const int Entered = 1;
				public const int Exited = 2;
				public const int Aborted = int.MaxValue;
			}

			public AsyncLocalMonitor(FastAsyncLock owner)
			{
				_owner = owner;
			}

			public IDisposable EnterSync()
			{
				// No concurrency consideration: we are on a single 'ExecutionContext' (i.e. on a single thread at a time)

				Debug.Assert(_count == 0);
				Debug.Assert(_state == State.Waiting);

				_count = 1;
				_state = State.Entered;

				return new Handle(this);
			}

			public bool TryReEnterSync()
			{
				// No concurrency consideration: we are on a single 'ExecutionContext' (i.e. on a single thread at a time)

				switch (_state)
				{
					case State.Waiting:
						throw new InvalidOperationException("'ExecutionContext' corrupted.");

					case State.Entered:
						Interlocked.Increment(ref _count);
						lock (_exitGate)
						{
							return _state == State.Entered;
						}

					case State.Aborted:
					case State.Exited:
						return false;

					default:
						throw new InvalidOperationException("Invalid state.");
				}
			}

			public Task<IDisposable> EnterAsync(CancellationToken ct)
			{
				// The item may have been already dequeued by the previous since it was set as '_next',
				// If so, return sync (note: the '_count' was already incremented)
				if (_state == State.Waiting)
				{
					_enterAsync = new FastTaskCompletionSource<IDisposable>();

					if (ct.CanBeCanceled)
					{
						_enterAsync.OnCompleted(ct.Register(Abort).Dispose);

						void Abort()
						{
							if (Interlocked.CompareExchange(ref _state, State.Aborted, State.Waiting) == State.Waiting)
							{
								_enterAsync.SetCanceled();
							}
						}
					}

					// This instance may have been dequeued by previous while we where initiazing the async hanlde,
					// so make sure to not wait a task that will never complete.
					if (_state == State.Waiting)
					{
						return _enterAsync.Task!;
					}
				}

				return Task.FromResult<IDisposable>(new Handle(this));
			}

			private void Dequeued()
			{
				// Dequeing may occures more than once. So move to next step only if item is really waiting!
				switch (Interlocked.CompareExchange(ref _state, State.Entered, State.Waiting))
				{
					case State.Waiting:
						_count = 1;
						_enterAsync?.SetResult(new Handle(this)); // null check: item may be dequeued even if EnterAsync was not yet invoked
						break;

					case State.Aborted:
						_next?.Dequeued();
						break;
				}
			}

			public bool TryEnqueue(AsyncLocalMonitor next)
			{
				Debug.Assert(_next == null);

				if (_state >= State.Exited)
				{
					return false;
				}

				// First set item as '_next'
				_next = next;
				if (_state >= State.Exited)
				{
					// It may occures that this monitor is being exited while we where setting '_next'.
					// If so, make sure that the '_next' is being dequeued.
					// Note: This may conduct the item to be 'Dequeued' twice (if it was already set while exiting, '_next' has already been dequeued)

					_next.Dequeued();
				}

				return true;
			}

			public void Exit()
			{
				// As we are running asynchronous, it may occures that the inner task completes from another execution context (e.g. an event).
				// So unlike the synchronous lock which must be exited from the same execution context / thread (otherwise we receive an SynchronizationLockException),
				// here we cannot enforce the 'Exit' to be invoked only from the entering execution context (ie. check that '_owner._localMonitor.Value == this')
				// Consequently, we have to handle concurrency with ReEntrency (Enter{Sync|Async} are not impacted since for those the caller did not received an 'Handle' yet)

				if (Interlocked.Decrement(ref _count) == 0)
				{
					lock (_exitGate)
					{
						if (_count == 0 && Interlocked.CompareExchange(ref _state, State.Exited, State.Entered) == State.Entered)
						{
							_next?.Dequeued();
						}
					}
				}
			}
		}

		/// <summary>
		/// An handle on an async lock which makes sure that disposing it mutiple times won't exit the monitor multiple times
		/// </summary>
		private class Handle : IDisposable
		{
			private readonly AsyncLocalMonitor _awaiter;
			private int _isDisposed;

			public Handle(AsyncLocalMonitor awaiter)
			{
				_awaiter = awaiter;
			}

			public void Dispose()
			{
				if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
				{
					_awaiter.Exit();
				}
			}
		}
	}
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;

namespace Uno.Helpers
{
	internal sealed class EventLoop : IDisposable
	{
		/// <summary>
		/// Counter for diagnostic purposes, to name the threads.
		/// </summary>
		private static int _counter;

		/// <summary>
		/// Thread factory function.
		/// </summary>
		private readonly Func<ThreadStart, Thread> _threadFactory;

		/// <summary>
		/// Thread used by the event loop to run work items on. No work should be run on any other thread.
		/// If ExitIfEmpty is set, the thread can quit and a new thread will be created when new work is scheduled.
		/// </summary>
		private Thread? _thread;

		/// <summary>
		/// Gate to protect data structures, including the work queue and the ready list.
		/// </summary>
		private readonly object _gate;

		/// <summary>
		/// Semaphore to count requests to re-evaluate the queue, from either Schedule requests or when a timer
		/// expires and moves on to the next item in the queue.
		/// </summary>
		private readonly SemaphoreSlim _evt;

		/// <summary>
		/// Queue holding items that are ready to be run as soon as possible. Protected by the gate.
		/// </summary>
		private readonly Queue<Action> _readyList;

		/// <summary>
		/// Flag indicating whether the event loop should quit. When set, the event should be signaled as well to
		/// wake up the event loop thread, which will subsequently abandon all work.
		/// </summary>
		private bool _disposed;

		/// <summary>
		/// Creates an object that schedules units of work on a designated thread.
		/// </summary>
		public EventLoop()
			: this(a => new Thread(a) { Name = "Event Loop " + Interlocked.Increment(ref _counter), IsBackground = true })
		{
		}

		/// <summary>
		/// Creates an object that schedules units of work on a designated thread, using the specified factory to control thread creation options.
		/// </summary>
		/// <param name="threadFactory">Factory function for thread creation.</param>
		/// <exception cref="ArgumentNullException"><paramref name="threadFactory"/> is <c>null</c>.</exception>
		public EventLoop(Func<ThreadStart, Thread> threadFactory)
		{
			_threadFactory = threadFactory ?? throw new ArgumentNullException(nameof(threadFactory));

			_gate = new object();

			_evt = new SemaphoreSlim(0);
			_readyList = new Queue<Action>();
		}

		public void Schedule(Action action)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			lock (_gate)
			{
				_readyList.Enqueue(action);
				_evt.Release();

				EnsureThread();
			}
		}


		/// <summary>
		/// Ends the thread associated with this scheduler. All remaining work in the scheduler queue is abandoned.
		/// </summary>
		public void Dispose()
		{
			lock (_gate)
			{
				if (!_disposed)
				{
					_disposed = true;
					_evt.Release();
				}
			}
		}

		/// <summary>
		/// Ensures there is an event loop thread running. Should be called under the gate.
		/// </summary>
		private void EnsureThread()
		{
			if (_thread == null)
			{
				_thread = _threadFactory(Run);
				_thread.Start();
			}
		}

		private void Run()
		{
			while (true)
			{
				_evt.Wait();

				Action[]? ready = null;

				lock (_gate)
				{
					while (_evt.CurrentCount > 0)
					{
						_evt.Wait();
					}

					if (_disposed)
					{
						_evt.Dispose();
						return;
					}

					if (_readyList.Count > 0)
					{
						ready = _readyList.ToArray();
						_readyList.Clear();
					}
				}

				if (ready != null)
				{
					foreach (var item in ready)
					{
						item.Invoke();
					}
				}
			}
		}
	}
}

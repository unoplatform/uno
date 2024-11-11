// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using Uno.UI.Dispatching;

namespace Uno.UI.Runtime.Skia.Win32
{
	internal sealed class Win32EventLoop : IDisposable
	{
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

		private readonly List<HWND> _hwnds = new();

		/// <summary>
		/// Flag indicating whether the event loop should quit. When set, the event should be signaled as well to
		/// wake up the event loop thread, which will subsequently abandon all work.
		/// </summary>
		private bool _disposed;

		/// <summary>
		/// Creates an object that schedules units of work on a designated thread, using the specified factory to control thread creation options.
		/// </summary>
		/// <param name="threadFactory">Factory function for thread creation.</param>
		/// <exception cref="ArgumentNullException"><paramref name="threadFactory"/> is <c>null</c>.</exception>
		public Win32EventLoop(Func<ThreadStart, Thread> threadFactory)
		{
			var thread = threadFactory(Run);
			_gate = new object();

			_evt = new SemaphoreSlim(0);
			_readyList = new Queue<Action>();

			thread.Start();
		}

		public void Schedule(Action action, NativeDispatcherPriority p)
		{
			if (action == null)
			{
				throw new ArgumentNullException(nameof(action));
			}

			lock (_gate)
			{
				_readyList.Enqueue(action);
				_evt.Release();
			}
		}

		public void AddHwnd(HWND hwnd)
		{
			lock (_gate)
			{
				_hwnds.Add(hwnd);
			}
		}

		public void RemoveHwnd(HWND hwnd)
		{
			lock (_gate)
			{
				_hwnds.Remove(hwnd);
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

		private void Run()
		{
			while (true)
			{
				SpinWait.SpinUntil(() =>
				{
					bool aHwndHasAMessage;
					lock (_gate)
					{
						aHwndHasAMessage = !_hwnds.TrueForAll(static hwnd =>
							!PInvoke.PeekMessage(out _, hwnd, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_NOREMOVE));
					}

					// ReSharper disable once AccessToDisposedClosure
					return _evt.CurrentCount > 0 || aHwndHasAMessage;
				});

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

					var foundAMessage = true;
					while (foundAMessage)
					{
						foundAMessage = false;
						foreach (var hwnd in _hwnds)
						{
							if (PInvoke.PeekMessage(out var msg, hwnd, 0, 0, PEEK_MESSAGE_REMOVE_TYPE.PM_REMOVE) != 0)
							{
								foundAMessage = true;
								PInvoke.TranslateMessage(msg);
								PInvoke.DispatchMessage(msg);
							}
						}
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

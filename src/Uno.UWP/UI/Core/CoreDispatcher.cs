using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Uno.Logging;
using Windows.UI.Composition;

namespace Windows.UI.Core
{
	public delegate void DispatchedHandler();
	public delegate void CancellableDispatchedHandler(CancellationToken ct);
	public delegate void IdleDispatchedHandler(IdleDispatchedHandlerArgs e);

	/// <summary>
	/// Defines a priority-based UI Thread scheduler.
	/// </summary>
	/// <remarks>
	/// This implementation is based on the fact that the native queue will 
	/// only contain one instance of the callback for the current core dispatcher.
	/// 
	/// This gives the native events, such as touch, the priority over managed-side queued
	/// events, and will allow a properly prioritized processing of idle events.
	/// </remarks>
	public sealed partial class CoreDispatcher
	{
		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{EA0762E9-8208-4501-B4A5-CC7ECF7BE85E}");

			public const int CoreDispatcher_Schedule = 1;
			public const int CoreDispatcher_InvokeStart = 2;
			public const int CoreDispatcher_InvokeStop = 3;
			public const int CoreDispatcher_Exception = 4;
			public const int CoreDispatcher_Cancelled = 5;
		}

		/// <summary>
		/// Defines a set of queues based on the number of priorities defined in <see cref="CoreDispatcherPriority"/>.
		/// </summary>
		private Queue<UIAsyncOperation>[] _queues = new Queue<UIAsyncOperation>[] {
			new Queue<UIAsyncOperation>(),
			new Queue<UIAsyncOperation>(),
			new Queue<UIAsyncOperation>(),
			new Queue<UIAsyncOperation>(),
		};

		[ThreadStatic]
		private static bool? _hasThreadAccess;

		/// <summary>
		/// Enforce access on the UI thread.
		/// </summary>
		internal static void CheckThreadAccess()
		{
#if !__WASM__
			// This check is disabled on WASM until threading support is enabled, since HasThreadAccess is currently user-configured (and defaults to false).
			if (!Main.HasThreadAccess)
			{
				throw new InvalidOperationException("The application called an interface that was marshalled for a different thread.");
			}
#endif
		}

		private readonly CoreDispatcherSynchronizationContext _highSyncCtx;
		private readonly CoreDispatcherSynchronizationContext _normalSyncCtx;
		private readonly CoreDispatcherSynchronizationContext _lowSyncCtx;
		private readonly CoreDispatcherSynchronizationContext _idleSyncCtx;
		private readonly IdleDispatchedHandlerArgs _idleDispatchedHandlerArgs;
		private readonly object _gate = new object();

		private int _globalCount;
		private CoreDispatcherPriority _currentPriority;

		internal bool ShouldRaiseRenderEvents => Rendering != null;
		/// <summary>
		/// Backs the CompositionTarget.Rendering event for WebAssembly.
		/// </summary>
		internal event EventHandler<object> Rendering;
		internal int RenderEventThrottle;
		internal Func<TimeSpan, object> RenderingEventArgsGenerator { get; set; }

		private readonly DateTimeOffset _startTime;

		public CoreDispatcher()
		{
			_highSyncCtx = new CoreDispatcherSynchronizationContext(this, CoreDispatcherPriority.High);
			_normalSyncCtx = new CoreDispatcherSynchronizationContext(this, CoreDispatcherPriority.Normal);
			_lowSyncCtx = new CoreDispatcherSynchronizationContext(this, CoreDispatcherPriority.Low);
			_idleSyncCtx = new CoreDispatcherSynchronizationContext(this, CoreDispatcherPriority.Idle);

			_idleDispatchedHandlerArgs = new IdleDispatchedHandlerArgs(() => IsQueueIdle);

			Initialize();

			_startTime = DateTimeOffset.UtcNow;
		}


		/// <summary>
		/// Determines if the current thread has access to this CoreDispatcher.
		/// </summary>
		public bool HasThreadAccess
		{
			get
			{
				if (!_hasThreadAccess.HasValue)
				{
					_hasThreadAccess = GetHasThreadAccess();
				}

				return _hasThreadAccess.Value;
			}
		}

		/// <summary>
		/// Gets the priority of the current task.
		/// </summary>
		/// <remarks>Sets has no effect on Uno</remarks>
		public CoreDispatcherPriority CurrentPriority
		{
			get => _currentPriority;
			[Uno.NotImplemented] set { } // Drop the set done by external code
		}

		/// <summary>
		/// Determines if there are no elements in queues other than the idle one.
		/// </summary>
		private bool IsQueueIdle => GetQueue(CoreDispatcherPriority.Low).Count +
				GetQueue(CoreDispatcherPriority.Normal).Count +
				GetQueue(CoreDispatcherPriority.High).Count == 0;

		partial void Initialize();

		/// <summary>
		/// Schedules the provided handler on the dispatcher.
		/// </summary>
		/// <param name="priority">The execution priority for the handler</param>
		/// <param name="handler">The handler to execute</param>
		/// <returns>An async operation for the scheduled handler.</returns>
		public UIAsyncOperation RunAsync(CoreDispatcherPriority priority, DispatchedHandler handler)
		{
			return EnqueueOperation(priority, handler);
		}

		/// <summary>
		/// Schedules the provided handler on the dispatcher.
		/// </summary>
		/// <param name="priority">The execution priority for the handler</param>
		/// <param name="handler">The handler to execute</param>
		/// <returns>An async operation for the scheduled handler.</returns>
		/// <remarks>Can only be invoked on the UI thread</remarks>
		internal UIAsyncOperation RunAsync(CoreDispatcherPriority priority, CancellableDispatchedHandler handler)
		{
			CoreDispatcher.CheckThreadAccess();

			UIAsyncOperation operation = null;

			DispatchedHandler nonCancellableHandler = () => handler(operation.Token);

			return operation = EnqueueOperation(priority, nonCancellableHandler);
		}

		/// <summary>
		/// Schedules the provided handler using the idle priority
		/// </summary>
		/// <param name="handler">The handler to execute</param>
		/// <returns>An async operation for the scheduled handler.</returns>
		public UIAsyncOperation RunIdleAsync(IdleDispatchedHandler handler)
		{
			return EnqueueOperation(
				CoreDispatcherPriority.Idle,
				() => handler(_idleDispatchedHandlerArgs)
			);
		}

		private UIAsyncOperation EnqueueOperation(CoreDispatcherPriority priority, DispatchedHandler handler)
		{
			EventActivity scheduleActivity = null;
			if (_trace.IsEnabled)
			{
				scheduleActivity = _trace.WriteEventActivity(
					TraceProvider.CoreDispatcher_Schedule,
					EventOpcode.Send,
					new[] {
						((int)priority).ToString(),
						handler.Method.DeclaringType.FullName + "." + handler.Method.DeclaringType.Name
					}
				);
			}

			var operation = new UIAsyncOperation(handler, scheduleActivity);

			if (priority < CoreDispatcherPriority.Idle || priority > CoreDispatcherPriority.High)
			{
				throw new ArgumentException($"The priority {priority} is not supported");
			}

			var queue = GetQueue(priority);

			bool shouldEnqueue;

			lock (_gate)
			{
				queue.Enqueue(operation);
				shouldEnqueue = IncrementGlobalCount() == 1;
			}

			if (shouldEnqueue)
			{
				EnqueueNative();
			}

			return operation;
		}

		private Queue<UIAsyncOperation> GetQueue(CoreDispatcherPriority priority)
		{
			return _queues[(int)priority + 2];
		}

		/// <summary>
		/// Enqueues a operation on the native UI Thread.
		/// </summary>
		partial void EnqueueNative();

		private int IncrementGlobalCount()
		{
			return Interlocked.Increment(ref _globalCount);
		}

		private int DecrementGlobalCount()
		{
			return Interlocked.Decrement(ref _globalCount);
		}

		private void DispatchItems()
		{
			UIAsyncOperation operation = null;

			Rendering?.Invoke(null, RenderingEventArgsGenerator(DateTimeOffset.UtcNow - _startTime));

			var didEnqueue = false;
			for (var i = 3; i >= 0; i--)
			{
				var queue = _queues[i];

				lock (_gate)
				{
					if (queue.Count > 0)
					{
						operation = queue.Dequeue();
						_currentPriority = (CoreDispatcherPriority)(i - 2);

						if (DecrementGlobalCount() > 0)
						{
							didEnqueue = true;
							EnqueueNative();
						}
						break;
					}
				}
			}

			if (operation != null)
			{
				if (!operation.IsCancelled)
				{
					IDisposable runActivity = null;

					try
					{
						if (_trace.IsEnabled)
						{
							runActivity = _trace.WriteEventActivity(
								TraceProvider.CoreDispatcher_InvokeStart,
								TraceProvider.CoreDispatcher_InvokeStop,
								relatedActivity: operation.ScheduleEventActivity,
								payload: new[] { ((int)CurrentPriority).ToString(), operation.GetDiagnosticsName() }
							);
						}

						using (runActivity)
						using (GetSyncContext(CurrentPriority).Apply())
						{
							operation.Action();
							operation.Complete();
						}
					}
					catch (Exception ex)
					{
						if (_trace.IsEnabled)
						{
							_trace.WriteEvent(TraceProvider.CoreDispatcher_Exception, EventOpcode.Send, new[] { ex.GetType().ToString(), operation.GetDiagnosticsName() });
						}
						operation.SetError(ex);
						this.Log().Error("Dispatcher unhandled exception", ex);
					}
				}
				else
				{
					if (_trace.IsEnabled)
					{
						_trace.WriteEvent(TraceProvider.CoreDispatcher_Cancelled, EventOpcode.Send, new[] { operation.GetDiagnosticsName() });
					}
				}
			}
			else if (!ShouldRaiseRenderEvents)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Error("Dispatch queue is empty");
				}
			}

			// Restore the priority to the default for task that are coming from the native events
			// (i.e. not dispatch by this running loop)
			_currentPriority = CoreDispatcherPriority.Normal;

			if (!didEnqueue && ShouldRaiseRenderEvents)
			{
				DispatchWakeUp();
			}
		}

		async void DispatchWakeUp()
		{
			await Task.Delay(RenderEventThrottle);
			if (ShouldRaiseRenderEvents)
			{
				WakeUp();
			}
		}

		/// <summary>
		/// Wakes up the dispatcher.
		/// </summary>
		internal void WakeUp()
		{
			CheckThreadAccess();

			if (IncrementGlobalCount() == 1)
			{
				EnqueueNative();
			}

			DecrementGlobalCount();
		}

		private CoreDispatcherSynchronizationContext GetSyncContext(CoreDispatcherPriority priority)
		{
			switch (priority)
			{
				case CoreDispatcherPriority.High: return _highSyncCtx;
				case CoreDispatcherPriority.Normal: return _normalSyncCtx;
				case CoreDispatcherPriority.Low: return _lowSyncCtx;
				case CoreDispatcherPriority.Idle: return _idleSyncCtx;
				default: throw new ArgumentOutOfRangeException(nameof(priority));
			}
		}
	}
}

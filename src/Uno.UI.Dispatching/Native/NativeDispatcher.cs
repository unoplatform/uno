// #define REPORT_FPS

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Uno.Diagnostics.Eventing;
using Uno.Foundation.Logging;

namespace Uno.UI.Dispatching
{
	/// <summary>
	/// Defines a priority-based UI Thread scheduler.
	/// </summary>
	internal sealed partial class NativeDispatcher
	{
		/// <summary>
		/// Defines a set of queues based on the number of priorities defined in <see cref="NativeDispatcherPriority"/>.
		/// </summary>
		private readonly Queue<Delegate>[] _queues = new[]
		{
			new Queue<Delegate>(), // High
			new Queue<Delegate>(), // Normal
			new Queue<Delegate>(), // Low
			new Queue<Delegate>(), // Idle
		};

		private readonly object _gate = new();

		private readonly long _startTime;

		private NativeDispatcherPriority _currentPriority;

		private int _globalCount;

		[ThreadStatic]
		private static bool? _hasThreadAccess;

		private readonly static IEventProvider _trace = Tracing.Get(TraceProvider.Id);

		private NativeDispatcher()
		{
			Debug.Assert(
				(int)NativeDispatcherPriority.High == 0 &&
				(int)NativeDispatcherPriority.Normal == 1 &&
				(int)NativeDispatcherPriority.Low == 2 &&
				(int)NativeDispatcherPriority.Idle == 3 &&
				Enum.GetValues<NativeDispatcherPriority>().Length == 4);

			_currentPriority = NativeDispatcherPriority.Normal;

			SynchronizationContext = new NativeDispatcherSynchronizationContext(this);

			Initialize();

			_startTime = Stopwatch.GetTimestamp();
		}

		internal NativeDispatcherSynchronizationContext SynchronizationContext { get; }

		/// <summary>
		/// Enforce access on the UI thread.
		/// </summary>
		internal static void CheckThreadAccess()
		{
			if (!Main.HasThreadAccess)
			{
				throw new InvalidOperationException("The application called an interface that was marshalled for a different thread.");
			}
		}

#if __ANDROID__ || __WASM__ || __SKIA__ || __APPLE_UIKIT__ || IS_UNIT_TESTS
		private static void DispatchItems()
		{
			// Currently, we have a singleton NativeDispatcher.
			// We want DispatchItems to be static to avoid delegate allocations.
			var @this = NativeDispatcher.Main;
			if (@this.Rendering != null)
			{
				Debug.Assert(@this.RenderingEventArgsGenerator != null);

				@this.Rendering.Invoke(null, @this.RenderingEventArgsGenerator.Invoke(Stopwatch.GetElapsedTime(@this._startTime)));
			}

			Action? action = null;

			for (var p = 0; p <= 3; p++)
			{
				var queue = @this._queues[p];

				lock (@this._gate)
				{
					if (queue.Count > 0)
					{
						action = Unsafe.As<Action>(queue.Dequeue());

						@this._currentPriority = (NativeDispatcherPriority)p;

						if (Interlocked.Decrement(ref @this._globalCount) > 0)
						{
							@this.EnqueueNative(@this._currentPriority);
						}

						break;
					}
				}
			}

			RunAction(@this, action);

			// Restore the priority to the default for native events
			// (i.e. not dispatched by this running loop)
			@this._currentPriority = NativeDispatcherPriority.Normal;
		}

		/// <remarks>
		/// This method runs in a separate method in order to workaround for the following issue:
		/// https://github.com/dotnet/runtime/issues/111281
		/// which prevents AOT on WebAssembly when try/catch/finally are found in the same method.
		/// </remarks>
		private static void RunAction(NativeDispatcher dispatcher, Action? action)
		{
			if (action != null)
			{
				try
				{
					using (dispatcher.SynchronizationContext.Apply())
					{
						action();
					}
				}
				catch (Exception exception)
				{
					dispatcher.Log().Error("NativeDispatcher unhandled exception", exception);
				}
			}
			else if (!dispatcher.IsRendering && dispatcher.Log().IsEnabled(LogLevel.Debug))
			{
				dispatcher.Log().Error("Dispatch queue is empty.");
			}
		}
#endif
		internal void SynchronousDispatchRendering()
			=> SynchronousDispatchRenderingPartial();

		partial void SynchronousDispatchRenderingPartial();

#if REPORT_FPS
		static FrameRateLogger _dispatchRenderingLogger = new FrameRateLogger(typeof(NativeDispatcher), "DispatchRendering");
#endif

		internal void DispatchRendering()
		{
			if (IsRendering)
			{
#if REPORT_FPS
				_dispatchRenderingLogger.ReportFrame();
#endif
				Enqueue(() =>
				{
					RaiseRendered();
				});
			}
		}

		private void RaiseRendered()
		{
			if (Rendering != null)
			{
				// If we raised the Rendering event we can render composition tree.
				Rendered?.Invoke();
			}
		}

		internal void Enqueue(Action handler, NativeDispatcherPriority priority = NativeDispatcherPriority.Normal)
		{
			if (!_trace.IsEnabled)
			{
				EnqueueCore(handler, priority);
			}
			else
			{
				var activity = LogScheduleEvent(handler, priority);

				EnqueueCore(() =>
				{
					try
					{
						using var runActivity = LogRunEvent(activity, handler, _currentPriority);

						handler();
					}
					catch (Exception exception)
					{
						LogExceptionEvent(exception, handler);

						throw;
					}
				}, priority);
			}
		}

		internal Task EnqueueAsync(Action handler, NativeDispatcherPriority priority = NativeDispatcherPriority.Normal)
		{
			var tcs = new TaskCompletionSource();

			if (!_trace.IsEnabled)
			{
				EnqueueCore(() =>
				{
					try
					{
						handler();

						tcs.SetResult();
					}
					catch (Exception exception)
					{
						tcs.SetException(exception);

						throw;
					}
				}, priority);
			}
			else
			{
				var activity = LogScheduleEvent(handler, priority);

				EnqueueCore(() =>
				{
					try
					{
						using var runActivity = LogRunEvent(activity, handler, _currentPriority);

						handler();

						tcs.SetResult();
					}
					catch (Exception exception)
					{
						LogExceptionEvent(exception, handler);

						tcs.SetException(exception);

						throw;
					}
				}, priority);
			}

			return tcs.Task;
		}

		internal UIAsyncOperation EnqueueOperation(Action handler, NativeDispatcherPriority priority = NativeDispatcherPriority.Normal)
		{
			if (!_trace.IsEnabled)
			{
				var operation = new UIAsyncOperation(handler);

				EnqueueCore(() =>
				{
					if (!operation.IsCancelled)
					{
						try
						{
							operation.Action();

							operation.Complete();
						}
						catch (Exception exception)
						{
							operation.SetError(exception);

							throw;
						}
					}
				}, priority);

				return operation;
			}
			else
			{
				var activity = LogScheduleEvent(handler, priority);

				var operation = new UIAsyncOperation(handler, activity);

				EnqueueCore(() =>
				{
					if (!operation.IsCancelled)
					{
						try
						{
							using var runActivity = LogRunEvent(operation.ScheduleEventActivity, operation.Action, _currentPriority);

							operation.Action();

							operation.Complete();
						}
						catch (Exception exception)
						{
							LogExceptionEvent(exception, operation.Action);

							operation.SetError(exception);

							throw;
						}
					}
					else
					{
						_trace.WriteEvent(TraceProvider.NativeDispatcher_Cancelled, EventOpcode.Send, new[] { operation.Action });
					}
				}, priority);

				return operation;
			}
		}

		internal UIAsyncOperation EnqueueCancellableOperation(Action<CancellationToken> handler, NativeDispatcherPriority priority = NativeDispatcherPriority.Normal)
		{
			UIAsyncOperation? operation = null;

			if (!_trace.IsEnabled)
			{
				operation = new UIAsyncOperation(() => handler(operation!.Token));

				EnqueueCore(() =>
				{
					if (!operation.IsCancelled)
					{
						try
						{
							operation.Action();

							operation.Complete();
						}
						catch (Exception exception)
						{
							operation.SetError(exception);

							throw;
						}
					}
				}, priority);

				return operation;
			}
			else
			{
				var delegatingHandler = () => handler(operation!.Token);

				var activity = LogScheduleEvent(delegatingHandler, priority);

				operation = new UIAsyncOperation(delegatingHandler, activity);

				EnqueueCore(() =>
				{
					if (!operation.IsCancelled)
					{
						try
						{
							using var runActivity = LogRunEvent(operation.ScheduleEventActivity, operation.Action, _currentPriority);

							operation.Action();

							operation.Complete();
						}
						catch (Exception exception)
						{
							LogExceptionEvent(exception, operation.Action);

							operation.SetError(exception);

							throw;
						}
					}
					else
					{
						_trace.WriteEvent(TraceProvider.NativeDispatcher_Cancelled, EventOpcode.Send, new[] { operation.Action });
					}
				}, priority);

				return operation;
			}
		}

		internal UIAsyncOperation EnqueueIdleOperation(Action<NativeDispatcher> handler)
			=> EnqueueOperation(() => handler(this), NativeDispatcherPriority.Idle);

		private void EnqueueCore(Delegate handler, NativeDispatcherPriority priority)
		{
			Debug.Assert((int)priority >= 0 && (int)priority <= 3);

			bool shouldEnqueue;

			lock (_gate)
			{
				_queues[(int)priority].Enqueue(handler);

				shouldEnqueue = Interlocked.Increment(ref _globalCount) == 1;
			}

			if (shouldEnqueue)
			{
				EnqueueNative(priority);
			}
		}

		/// <summary>
		/// Enqueues an operation on the native UI thread.
		/// </summary>
		partial void EnqueueNative(NativeDispatcherPriority priority);

		partial void Initialize();

		private static void LogExceptionEvent(Exception exception, Action handler) =>
			_trace.WriteEvent(
				TraceProvider.NativeDispatcher_Exception,
				EventOpcode.Send,
				new[] {
					exception.GetType().ToString(),
					handler.Method.DeclaringType?.FullName + "." + handler.Method.Name });

		private static EventProviderExtensions.DisposableEventActivity LogRunEvent(EventActivity activity, Action handler, NativeDispatcherPriority priority) =>
			_trace.WriteEventActivity(
				TraceProvider.NativeDispatcher_InvokeStart,
				TraceProvider.NativeDispatcher_InvokeStop,
				relatedActivity: activity,
				payload: new[] {
					((int)priority).ToString(CultureInfo.InvariantCulture),
					handler.Method.DeclaringType?.FullName + "." + handler.Method.Name });

		private static EventActivity LogScheduleEvent(Action handler, NativeDispatcherPriority priority) =>
			_trace.WriteEventActivity(
				TraceProvider.NativeDispatcher_Schedule,
				EventOpcode.Send,
				new[] {
					((int)priority).ToString(CultureInfo.InvariantCulture),
					handler.Method.DeclaringType?.FullName + "." + handler.Method.Name });

		/// <summary>
		/// Wakes up the dispatcher.
		/// </summary>
		internal void WakeUp()
		{
			lock (_gate)
			{
				if (Interlocked.Increment(ref _globalCount) == 1)
				{
					EnqueueNative(NativeDispatcherPriority.Normal);
				}

				Interlocked.Decrement(ref _globalCount);
			}
		}

		/// <summary>
		/// Gets the priority of the current task.
		/// </summary>
		internal NativeDispatcherPriority CurrentPriority => _currentPriority;

		/// <summary>
		/// Determines if the current thread has access to this dispatcher.
		/// </summary>
		internal bool HasThreadAccess => _hasThreadAccess ??= GetHasThreadAccess();

		internal bool IsIdle => _queues[(int)NativeDispatcherPriority.High].Count +
								_queues[(int)NativeDispatcherPriority.Normal].Count +
								_queues[(int)NativeDispatcherPriority.Low].Count == 0;

		internal bool IsRendering => Rendering != null;

		/// <summary>
		/// Gets the dispatcher for the main thread.
		/// </summary>
		internal static NativeDispatcher Main { get; } = new NativeDispatcher();

		// Dispatching for the CompositionTarget.Rendering event
		internal event EventHandler<object>? Rendering;

#pragma warning disable CS0067
		// Dispatching for the compositor to actually render the frame only called
		// when there are subscribers to Rendering
		internal event Action? Rendered;
#pragma warning restore CS0067

		internal Func<TimeSpan, object>? RenderingEventArgsGenerator { get; set; }

		public static class TraceProvider
		{
			public readonly static Guid Id = Guid.Parse("{EA0762E9-8208-4501-B4A5-CC7ECF7BE85E}");

			public const int NativeDispatcher_Schedule = 1;
			public const int NativeDispatcher_InvokeStart = 2;
			public const int NativeDispatcher_InvokeStop = 3;
			public const int NativeDispatcher_Exception = 4;
			public const int NativeDispatcher_Cancelled = 5;
		}
	}
}

// #define REPORT_FPS

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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

		private readonly Dictionary<object, (Action? renderAction, int normalItemsToProcessBeforeNextRenderAction)> _compositionTargets = new();

		private NativeDispatcherPriority _currentPriority;

		private int _globalCount;
		private int _pendingRenderActions;

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

			Action? action = @this.TryGetRenderAction();

			if (action is null)
			{
				for (var p = 0; p <= 3; p++)
				{
					var queue = @this._queues[p];

					lock (@this._gate)
					{
						if (queue.Count > 0)
						{
							action = Unsafe.As<Action>(queue.Dequeue());

							@this._currentPriority = (NativeDispatcherPriority)p;

							@this.LogTrace()?.Trace($"Running next job in dispatcher queue: priority: {@this._currentPriority} queue states=[{string.Join("] [", @this._queues.Select(q => q.Count))}]");
							if (Interlocked.Decrement(ref @this._globalCount) > 0)
							{
								@this.EnqueueNative(@this._currentPriority);
							}

							if (@this._currentPriority == NativeDispatcherPriority.Normal)
							{
								foreach (var (compositionTarget, details) in @this._compositionTargets)
								{
									if (details.normalItemsToProcessBeforeNextRenderAction > 0)
									{
										@this._compositionTargets[compositionTarget] = details with { normalItemsToProcessBeforeNextRenderAction = details.normalItemsToProcessBeforeNextRenderAction - 1 };
									}
								}
							}
							break;
						}
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
			else if (dispatcher.Log().IsEnabled(LogLevel.Debug))
			{
				dispatcher.Log().Error("Dispatch queue is empty.");
			}
		}

		private Action? TryGetRenderAction()
		{
			lock (_gate)
			{
				foreach (var (compositionTarget, details) in _compositionTargets)
				{
					if (details.renderAction is not null)
					{
						if (details.normalItemsToProcessBeforeNextRenderAction == 0)
						{
							_compositionTargets[compositionTarget] = (renderAction: null, normalItemsToProcessBeforeNextRenderAction: _queues[(int)NativeDispatcherPriority.Normal].Count);
							_pendingRenderActions--;

							_currentPriority = NativeDispatcherPriority.High;

							if (Interlocked.Decrement(ref _globalCount) > 0)
							{
								EnqueueNative(_currentPriority);
							}

							this.LogTrace()?.Trace($"Running render job from the dispatcher: queue states=[{string.Join("] [", _queues.Select(q => q.Count))}]");

							return details.renderAction;
						}
						else
						{
							Debug.Assert(_globalCount > _pendingRenderActions);
						}
					}
				}
			}

			return null;
		}
#endif

		public void EnqueueRender(object compositionTarget, Action handler)
		{
			bool shouldEnqueue = false;
			lock (_gate)
			{
				if (!_compositionTargets.TryGetValue(compositionTarget, out var details))
				{
					details = _compositionTargets[compositionTarget] = (null, 0);
				}

				Debug.Assert(details.renderAction is null);
				if (details.renderAction is null)
				{
					_pendingRenderActions++;
					shouldEnqueue = Interlocked.Increment(ref _globalCount) == 1;
				}
				_compositionTargets[compositionTarget] = details with
				{
					renderAction = handler,
				};
			}

			this.LogTrace()?.Trace($"{nameof(EnqueueRender)} : {nameof(shouldEnqueue)}={shouldEnqueue}");
			if (shouldEnqueue)
			{
				EnqueueNative(NativeDispatcherPriority.High);
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
			Debug.Assert((int)priority >= 0 && (int)priority <= 4);

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

		/// <summary>
		/// Gets the dispatcher for the main thread.
		/// </summary>
		internal static NativeDispatcher Main { get; } = new NativeDispatcher();

		// Dispatching for the CompositionTarget.Rendering event

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

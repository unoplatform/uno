using System;
using System.Collections.Immutable;
using System.Diagnostics;

using Android.OS;
using Android.Runtime;
using Android.Views;
using Java.Lang;
using Uno.Diagnostics.Eventing;
using Uno.Foundation.Logging;

namespace Uno.UI.Dispatching
{
	internal sealed partial class NativeDispatcher
	{
		private static readonly string[] _animationArray = ["Animation"];

		private Handler _handler;
		private NativeDispatcherImplementor _implementor;
		private Choreographer _choreographer;
		private FrameCallbackImplementor _animationImplementor;

		private ImmutableQueue<UIAsyncOperation> _animationQueue = ImmutableQueue<UIAsyncOperation>.Empty;

		/// <summary>
		/// Defines the maximum time for which the queue can be processed. We're assuming 2/3rd
		/// of 60fps, to leave room for other operartions to be computed.
		/// </summary>
		private readonly TimeSpan MaxRenderSpan = TimeSpan.FromSeconds((1 / 60f) * (2 / 3f));

		partial void Initialize()
		{
			_handler = new Handler(Looper.MainLooper);
			_implementor = new NativeDispatcherImplementor(DispatchItems);

			_choreographer = Choreographer.Instance;
			_animationImplementor = new FrameCallbackImplementor(DispatchItemsToChoreographer);
		}

		partial void EnqueueNative(NativeDispatcherPriority priority)
		{
			_handler.Post(_implementor);
		}

		private bool GetHasThreadAccess() => Looper.MyLooper() == Android.OS.Looper.MainLooper;

		/// <summary>
		/// Run operation with 'animation' priority, prior to layout and draw calls. This will run at the beginning of the next UI pass.
		/// </summary>
		internal UIAsyncOperation RunAnimation(Action handler)
		{
			var operation = new UIAsyncOperation(handler);

			ImmutableInterlocked.Enqueue(ref _animationQueue, operation);

			_choreographer.PostFrameCallback(_animationImplementor);

			return operation;
		}

		private void DispatchItemsToChoreographer()
		{
			var ts = Stopwatch.GetTimestamp();

			if (!_trace.IsEnabled)
			{
				while (!_animationQueue.IsEmpty && Stopwatch.GetElapsedTime(ts) < MaxRenderSpan)
				{
					try
					{
						if (ImmutableInterlocked.TryDequeue(ref _animationQueue, out UIAsyncOperation action))
						{
							if (!action.IsCancelled)
							{
								action.Action();
							}
						}
					}
					catch (global::System.Exception exception)
					{
						this.Log().Error("Dispatcher unhandled exception", exception);
					}
				}
			}
			else
			{
				while (!_animationQueue.IsEmpty && Stopwatch.GetElapsedTime(ts) < MaxRenderSpan)
				{
					try
					{
						using var runActivity = _trace.WriteEventActivity(
							TraceProvider.NativeDispatcher_InvokeStart,
							TraceProvider.NativeDispatcher_InvokeStop,
							relatedActivity: _trace.WriteEventActivity(TraceProvider.NativeDispatcher_Schedule, EventOpcode.Send, _animationArray),
							payload: _animationArray
						);

						if (ImmutableInterlocked.TryDequeue(ref _animationQueue, out UIAsyncOperation action))
						{
							if (!action.IsCancelled)
							{
								action.Action();
							}
						}
					}
					catch (global::System.Exception exception)
					{
						_trace.WriteEvent(TraceProvider.NativeDispatcher_Exception, EventOpcode.Send, new[] { exception.GetType().ToString() });

						this.Log().Error("Dispatcher unhandled exception", exception);
					}
				}
			}

			QueueOperations();
		}

		private void QueueOperations()
		{
			if (!_animationQueue.IsEmpty)
			{
				_choreographer.PostFrameCallback(_animationImplementor);
			}
		}

		/// <summary>
		/// An internal implementation of an IRunnable. Used to avoid creating new Java objects for every single action
		/// performed on the UI Thread.
		/// </summary>
		[Register("mono/java/lang/NativeDispatcherImplementor")]
		internal sealed class NativeDispatcherImplementor : Java.Lang.Object, IRunnable
		{
			private readonly static IntPtr _class;
			private readonly static IntPtr _ctor;
			private Action _action;

			static NativeDispatcherImplementor()
			{
				_class = JNIEnv.FindClass(typeof(NativeDispatcherImplementor));
				_ctor = JNIEnv.GetMethodID(_class, "<init>", "()V");
			}

			internal NativeDispatcherImplementor(Action action)
				: base(JNIEnv.StartCreateInstance(_class, _ctor, Array.Empty<JValue>()), JniHandleOwnership.TransferLocalRef)
			{
				JNIEnv.FinishCreateInstance(base.Handle, _class, _ctor, Array.Empty<JValue>());

				_action = action;
			}

			void IRunnable.Run()
			{
				_action();
			}
		}

		internal sealed class FrameCallbackImplementor : Java.Lang.Object, Choreographer.IFrameCallback
		{
			private readonly Action _action;

			public FrameCallbackImplementor(Action action)
			{
				_action = action;
			}

			public void DoFrame(long frameTimeNanos)
			{
				_action();
			}
		}
	}
}

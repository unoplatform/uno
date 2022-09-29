#nullable disable

#if __ANDROID__
using Android.OS;
using Android.Runtime;
using Android.Views;
using Java.Lang;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Foundation.Logging;
using System.Collections.Immutable;
using Uno;
using System.Diagnostics;

namespace Uno.UI.Dispatching
{
	internal sealed partial class CoreDispatcher
	{
		private Handler _handler;
		private Choreographer _choreographer;
		private CoreDispatcherImplementor _implementor;
		private FrameCallbackImplementor _animationImplementor;
		private object _animationGate = new object();
		private ImmutableQueue<UIAsyncOperation> _animationQueue = ImmutableQueue<UIAsyncOperation>.Empty;

		/// <summary>
		/// Defines the maximum time for which the queue can be processed. We're assuming 2/3rd 
		/// of 60fps, to leave room for other operartions to be computed.
		/// </summary>
		private readonly TimeSpan MaxRenderSpan = TimeSpan.FromSeconds((1 / 60f) * (2/3f));

		partial void Initialize()
		{
			_handler = new Handler(Looper.MainLooper);
			_implementor = new CoreDispatcherImplementor(DispatchItems);

			_choreographer = Choreographer.Instance;
			_animationImplementor = new FrameCallbackImplementor(DispatchItemsToChoreographer);
		}

		private bool GetHasThreadAccess() => Looper.MyLooper() == Android.OS.Looper.MainLooper;

		partial void EnqueueNative()
		{
			_handler.Post(_implementor);
		}

		/// <summary>
		/// Run operation with 'animation' priority, prior to layout and draw calls. This will run at the beginning of the next UI pass.
		/// </summary>
		internal UIAsyncOperation RunAnimation(DispatchedHandler handler)
		{
			var operation = new UIAsyncOperation(handler, null);

			ImmutableInterlocked.Enqueue(ref _animationQueue, operation);
			QueueOperations();

			return operation;
		}

		private void QueueOperations()
		{
			if (!_animationQueue.IsEmpty)
			{
				_choreographer.PostFrameCallback(_animationImplementor);
			}
		}

		private void DispatchItemsToChoreographer()
		{
			IDisposable runActivity = null;
			var watch = Stopwatch.StartNew();

			while (!_animationQueue.IsEmpty && watch.Elapsed < MaxRenderSpan)
			{
				try
				{
					if (_trace.IsEnabled)
					{
						runActivity = _trace.WriteEventActivity(
							TraceProvider.CoreDispatcher_InvokeStart,
							TraceProvider.CoreDispatcher_InvokeStop,
							relatedActivity: _trace.WriteEventActivity(TraceProvider.CoreDispatcher_Schedule, EventOpcode.Send, new[] { "Animation" }),
							payload: new[] { "Animation" }
						);
					}

					using (runActivity)
					{
						if (ImmutableInterlocked.TryDequeue(ref _animationQueue, out UIAsyncOperation action))
						{
							if (!action.IsCancelled)
							{
								action.Action();
							}
						}
					}
				}
				catch (global::System.Exception ex)
				{
					if (_trace.IsEnabled)
					{
						_trace.WriteEvent(TraceProvider.CoreDispatcher_Exception, EventOpcode.Send, new[] { ex.GetType().ToString() });
					}
					this.Log().Error("Dispatcher unhandled exception", ex);
				}
			}

			watch.Stop();

			if (!_animationQueue.IsEmpty)
			{
				QueueOperations();
			}
		}

		/// <summary>
		/// An internal implementation of an IRunnable. Used to avoid creating new Java objects for every single action 
		/// performed on the UI Thread.
		/// </summary>
		[Register("mono/java/lang/CoreDispatcherImplementor")]
		internal sealed class CoreDispatcherImplementor : Java.Lang.Object, IRunnable
		{
			private readonly static IntPtr _class;
			private readonly static IntPtr _ctor;
			private Action _action;

			static CoreDispatcherImplementor()
			{
				_class = JNIEnv.FindClass(typeof(CoreDispatcherImplementor));
				_ctor = JNIEnv.GetMethodID(_class, "<init>", "()V");
			}

			internal CoreDispatcherImplementor(Action action)
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
#endif

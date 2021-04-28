#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Composition;
using Windows.UI.Core;
using Android.App;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
using Exception = System.Exception;
using Point = Windows.Foundation.Point;
using Rect = Windows.Foundation.Rect;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Uno.UI.Composition
{
	internal class CompositorThread : Java.Lang.Object, ISurfaceHolderCallback2, Choreographer.IFrameCallback
	{
		[ThreadStatic] // Static to the related UI Thread !!!
		private static CompositorThread? _current;

		/// <summary>
		/// Gets the current compositor thread for the current UI THREAD, if any.
		/// </summary>
		public static CompositorThread? GetForCurrentThread()
			=> _current;

		public static CompositorThread Start(Compositor compositor, Android.Views.Window window)
			=> _current ??= new CompositorThread(compositor, window);

		private readonly Compositor _compositor;
		private readonly ManualResetEventSlim _frameRendered = new ManualResetEventSlim(false);

		private HardwareRenderer? _hwRender;
		private Thread? _thread;
		private Looper? _looper;
		private Choreographer? _choreographer;
		private Handler? _handler;

		public CompositorThread(Compositor compositor, Android.Views.Window window)
		{
			_compositor = compositor;
			//CoreWindow.SetInvalidateRender(OnRenderInvalidated); // TODO
			window.TakeSurface(this);
		}

		#region Surface management
		void ISurfaceHolderCallback.SurfaceCreated(ISurfaceHolder? holder)
		{
			_hwRender = new HardwareRenderer();
			_hwRender.SetContentRoot(_compositor.RootNode);
			_hwRender.SetSurface(holder!.Surface);

			// Even if the SurfaceChanged will be invoke right after this SurfaceCreated,
			// we set the position now so the compositor thread can already render a frame at the right size.
			SetBounds(holder.SurfaceFrame!.Width(), holder.SurfaceFrame.Height());

			_nativeFrameRequestd = false;

			Start();
		}

		void ISurfaceHolderCallback.SurfaceChanged(ISurfaceHolder? holder, Format format, int width, int height)
		{
			SetBounds(width, height);
		}

		void ISurfaceHolderCallback.SurfaceDestroyed(ISurfaceHolder? holder)
		{
			Stop();

			// Note: According to the doc, 'Destroy' only releases the surface and we should be able to restore it by setting a new surface.
			//		 However, as of 2021-03-29, we have to create a new one for each surface (screen stays black otherwise).
			_hwRender?.Destroy();
			_hwRender = null;
		}

		void ISurfaceHolderCallback2.SurfaceRedrawNeeded(ISurfaceHolder? holder)
		{
			_nativeFrameRequestd = true;
			_frameRendered.Reset();
			RequestFrame();
			_frameRendered.Wait();
		}

		// Note: We don't make it an explicit interface implementation as it's not available yet on the Xamarin.Android version used by the CI
		public void /*ISurfaceHolderCallback2.*/SurfaceRedrawNeededAsync(ISurfaceHolder? holder, Java.Lang.IRunnable drawingFinished)
		{
			_frameRendered.Reset();
			RequestFrame();

			Task.Run(() => // TODO: Should we invoke drawingFinished on calling / UI thread?
			{
				try
				{
					_frameRendered.Wait();
					drawingFinished.Run();
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error("Async redraw failed.", e);
					}
				}
			});
		}

		private void SetBounds(int width, int height)
		{
			if (CompositionConfiguration.UseLogicalPixelsForNativeVisualTree)
			{
				var scale = ViewHelperCore.Scale;
				var logicalWidth = width / scale;
				var logicalHeight = height / scale;

				_compositor.RootNode.SetPosition(new Rect(new Point(), new Size(logicalWidth, logicalHeight)));
				_compositor.RootNode.SetScaleX((float)scale);
				_compositor.RootNode.SetScaleY((float)scale);
				_compositor.RootNode.SetPivotX(0);
				_compositor.RootNode.SetPivotY(0);
			}
			else
			{
				_compositor.RootNode.SetPosition(new Rect(new Point(), new Size(width, height)));
			}
		}
		#endregion

		#region Core run loop
		private void Start()
		{
			if (_thread is {})
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn("Tried to start an already running compositor thread.");
				}
				return;
			}

			if (_hwRender is null)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("Tried to start a compositor thread but renderer not set.");
				}
				return;
			}

			_thread = new Thread(() =>
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info("Compositor thread started.");
				}

				_hwRender.Start();

				Looper.Prepare(); // Required to get a Choreographer.Instance
				_looper = Looper.MyLooper()!;
				_choreographer = Choreographer.Instance!;
				_handler = new Handler(_looper);

#if !DEBUG
#error only if debugger not attached?
#endif
				_choreographer.PostFrameCallback(this);

				Looper.Loop(); // Start the looper so the Choreographer.Instance will invoke our callback

				_hwRender.Stop();

				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info("Compositor thread exited.");
				}
			})
			{
				Name = "Compositor",
				Priority = ThreadPriority.AboveNormal
			};

			_thread.Start();
		}

		private void Stop()
		{
			var thread = _thread;
			if (thread is null)
			{
				return;
			}

			if (!TryStop(() => _looper?.Quit())
				&& !TryStop(() => thread.Interrupt())
				&& !TryStop(() => thread.Abort()))
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("Failed to abort compositor thread.");
				}
			}

			_thread = null;
			_looper = null;

			bool TryStop(Action stopAction)
			{
				if (thread.IsAlive)
				{
					try
					{
						stopAction();
						thread.Join(75); // A bit longer than 2 frames at 30 fps
					}
					catch (Exception)
					{
						return false;
					}
				}

				return true;
			}
		}

		//private long _frameIndex = 0;

		void Choreographer.IFrameCallback.DoFrame(long frameTimeNanos)
		{
			try
			{
				Console.WriteLine($"================== DoFrame #{_count}");

				if (_firstFrameTime == 0)
				{
					_firstFrameTime = frameTimeNanos;
					_lastReport.time = frameTimeNanos;
				}

				//if (++_frameIndex % 2 == 0)
				//{
				//	Choreographer.Instance!.PostFrameCallback(this);
				//	return;
				//}

				RenderFrame(frameTimeNanos);

				_frameCount++;
				var elapsed = frameTimeNanos - _lastReport.time;
				if (elapsed > 1000000000)
				{
					var frames = _frameCount - _lastReport.frames;

					Console.WriteLine($"[{Thread.CurrentThread.ManagedThreadId}] {DateTime.Now:T} FPS: {frames * 1000000000.0 / elapsed:F2} (time: {frameTimeNanos}ns | elapsed: {elapsed}ns | frames: {frames})");
					_lastReport = (frameTimeNanos, _frameCount);
				}

				//Choreographer.Instance!.PostFrameCallback(this);
			}
			catch (ThreadAbortException)
			{
				Thread.ResetAbort();
			}
			finally
			{
				//_choreographer!.PostFrameCallback(this);
				_choreographer!.PostFrameCallbackDelayed(this, 250);
			}
		}

		private ulong _frameCount = 0;
		private long _firstFrameTime;
		private (long time, ulong frames) _lastReport;

		private void RenderFrame(long frameTimeNanos)
		{
			try
			{
				const long _ticksPerNanos = TimeSpan.TicksPerMillisecond * 1000;
				_compositor.Render(frameTimeNanos * _ticksPerNanos);

				var request = _hwRender!.CreateRenderRequest(); // Cannot be null if thread is running
				var needsSync = !_frameRendered.IsSet;

				if (needsSync)
				{
					request.SetWaitForPresent(true);
				}

				request.SyncAndDraw();

				if (needsSync)
				{
					_frameRendered.Set();
				}
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("Failed to render frame.", e);
				}
			}

			//RecordingCanvas canvas;
			//try
			//{
			//	canvas = _visualTree.BeginRecording();
			//}
			//catch (Java.Lang.IllegalStateException)
			//{
			//	// Cannot start recording (most probably recording is already running somehow).
			//	if (this.Log().IsEnabled(LogLevel.Error))
			//	{
			//		this.Log().Error("Failed to begin frame rendering.");
			//	}

			//	return;
			//}

			

			//try
			//{
			//	//if (_activity.Compositor.IsDirty)
			//	{
			//		canvas.DrawColor(Color.Orange);

			//		Compositor.Render(_activity.Content);

			//		_activity.Content.Draw(canvas);
			//	}
			//}
			//catch (Exception e)
			//{
			//	if (this.Log().IsEnabled(LogLevel.Error))
			//	{
			//		this.Log().Error("Failed to render frame.", e);
			//	}
			//}
			//finally
			//{
			//	_visualTree.EndRecording();

			//	var request = _hwRender!.CreateRenderRequest(); // Cannot be null if thread is running
			//	var needsSync = !_frameRendered.IsSet;

			//	if (needsSync)
			//	{
			//		request.SetWaitForPresent(true);
			//	}

			//	request.SyncAndDraw();

			//	if (needsSync)
			//	{
			//		_frameRendered.Set();
			//	}
			//}
		}
#endregion

		private int _count;
		private bool _nativeFrameRequestd;

		internal void RequestFrame()
		{
			if (!_nativeFrameRequestd)
			{
				Console.WriteLine($"================== DO NOT REQUEST FRAME BEFORE THE NATIVE");
				return;
			}

			var ct = ++_count;
			Console.WriteLine($"================== RequestFrame - step 1 #{ct}");

			//_handler?.PostAtFrontOfQueue(() =>
			//{
			//	Console.WriteLine($"================== RequestFrame - step 2 #{ct}");

			//	_choreographer?.PostFrameCallback(this);
			//});
		}
	}
}

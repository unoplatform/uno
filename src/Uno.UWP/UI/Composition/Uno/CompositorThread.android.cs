#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
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

		public static void Start(ICompositionRoot activity)
			=> _current ??= new CompositorThread(activity);

		private readonly RenderNode _visualTree = new RenderNode("visual-tree");
		private readonly ManualResetEventSlim _frameRendered = new ManualResetEventSlim(false);
		private readonly ICompositionRoot _activity;

		private HardwareRenderer? _hwRender;
		private Thread? _thread;
		private Looper? _looper;

		public CompositorThread(ICompositionRoot activity)
		{
			_activity = activity;

			_activity.Window!.TakeSurface(this);
		}
		
		void ISurfaceHolderCallback.SurfaceCreated(ISurfaceHolder? holder)
		{
			_hwRender = new HardwareRenderer();
			_hwRender.SetContentRoot(_visualTree);
			_hwRender.SetSurface(holder!.Surface);

			// Even if the SurfaceChanged will be invoke right after this SurfaceCreated,
			// we set the position now so the compositor thread can already render a frame at the right size.
			SetBounds(holder.SurfaceFrame!.Width(), holder.SurfaceFrame.Height());

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
			_frameRendered.Reset();
			_frameRendered.Wait();
		}

		// Note: We don't make it an explicit interface implementation as it's not available yet on the Xamarin.Android version used by the CI
		public void /*ISurfaceHolderCallback2.*/SurfaceRedrawNeededAsync(ISurfaceHolder? holder, Java.Lang.IRunnable drawingFinished)
		{
			_frameRendered.Reset();

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
			_visualTree.SetPosition(new Rect(new Point(), new Size(width, height)));
		}

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
				_looper = Looper.MyLooper();

				Choreographer.Instance!.PostFrameCallback(this);

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

#if !NET6_0_OR_GREATER
				&& !TryStop(() => thread.Abort())
#endif
			)
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

		void Choreographer.IFrameCallback.DoFrame(long frameTimeNanos)
		{
			try
			{
				RenderFrame(frameTimeNanos);

				Choreographer.Instance!.PostFrameCallback(this);
			}
			catch (ThreadAbortException)
			{
#if !NET6_0_OR_GREATER
				Thread.ResetAbort();
#endif
			}
		}

		private void RenderFrame(long frameTimeNanos)
		{
			RecordingCanvas canvas;
			try
			{
				canvas = _visualTree.BeginRecording();
			}
			catch (Java.Lang.IllegalStateException)
			{
				// Cannot start recording (most probably recording is already running somehow).
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("Failed to begin frame rendering.");
				}

				return;
			}

			try
			{
				_activity.Content.Draw(canvas);
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("Failed to render frame.", e);
				}
			}
			finally
			{
				_visualTree.EndRecording();

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
		}
	}
}

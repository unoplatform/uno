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
using Uno.Extensions;
using Uno.Logging;
using Exception = System.Exception;
using Point = Windows.Foundation.Point;
using Rect = Windows.Foundation.Rect;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Uno.UI.Composition
{
	internal interface ICompositionRoot
	{
		Window Window { get; }

		View Content { get; }
	}

	internal class CompositorThread : Java.Lang.Object, ISurfaceHolderCallback2, Choreographer.IFrameCallback
	{
		[ThreadStatic] // Static to the related UI Thread !!!
		private static CompositorThread? _current;

		public static void Start(ICompositionRoot activity)
			=> _current ??= new CompositorThread(activity);

		//private RenderNode _animationsNode;
		//private List<(RenderNode node, double x, double y)> _animatedNodes = new List<(RenderNode node, double x, double y)>();

		private readonly HardwareRenderer _hwRender = new HardwareRenderer();
		private readonly RenderNode _visualTree = new RenderNode("visual-tree");
		private readonly ManualResetEventSlim _frameRendered = new ManualResetEventSlim(false);
		private readonly ICompositionRoot _activity;

		private Thread? _thread;

		public CompositorThread(ICompositionRoot activity)
		{
			_activity = activity;

			_hwRender.SetContentRoot(_visualTree);
			_activity.Window!.TakeSurface(this);
		}
		
		void ISurfaceHolderCallback.SurfaceCreated(ISurfaceHolder? holder)
		{
			_hwRender.SetSurface(holder!.Surface);

			Start();
		}

		void ISurfaceHolderCallback.SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
		{
			_visualTree.SetPosition(new Rect(new Point(), new Size(width, height)));
		}

		void ISurfaceHolderCallback.SurfaceDestroyed(ISurfaceHolder? holder)
		{
			Stop();

			_hwRender?.Stop();
			_hwRender?.Destroy(); // Note: This only release the surface, we can restore it by setting a new surface
		}

		void ISurfaceHolderCallback2.SurfaceRedrawNeeded(ISurfaceHolder? holder)
		{
			_frameRendered.Reset();
			_frameRendered.Wait();
		}

		void ISurfaceHolderCallback2.SurfaceRedrawNeededAsync(ISurfaceHolder holder, Java.Lang.IRunnable drawingFinished)
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
					this.Log().Error("Async redraw failed.", e);
				}
			});
		}

		private void Start()
		{
			if (_thread is {})
			{
				this.Log().Warn("Try to start an already running compositor thread.");
				return;
			}

			_thread = new Thread(() =>
			{
				_hwRender.Start();

				Looper.Prepare(); // Required to get a Choreographer.Instance
				Choreographer.Instance!.PostFrameCallback(this);

				Looper.Loop(); // Start the looper so the Choreographer.Instance will invoke our callback
			})
			{
				Name = "Compositor",
				Priority = ThreadPriority.AboveNormal
			};

			_thread.Start();
		}

		private void Stop()
		{
			try
			{
				_thread?.Abort();
			}
			catch (Exception e)
			{
				this.Log().Warn("Failed to abort compositor thread.", e);
			}
			_thread = null;
		}

		void Choreographer.IFrameCallback.DoFrame(long frameTimeNanos)
		{
			RenderFrame(frameTimeNanos);

			Choreographer.Instance!.PostFrameCallback(this);
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
				return;
			}

			try
			{
				_activity.Content.Draw(canvas);
			}
			catch (Exception e)
			{
				this.Log().Error("Failed to render frame.", e);
			}
			finally
			{
				_visualTree.EndRecording();

				var request = _hwRender.CreateRenderRequest();
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

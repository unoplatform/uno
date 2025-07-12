using System;
using System.Threading.Tasks;
using Android.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Extensibility;
using Uno.UI.Dispatching;
using Uno.UI.Hosting;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia.Android;

internal class AndroidChoreographerHandler
{
	private Choreographer _choreographer;
	private FrameCallbackImplementor _animationImplementor;
	private bool _pendingCallback;

	public AndroidChoreographerHandler()
	{
		_choreographer = Choreographer.Instance ?? throw new InvalidOperationException("AndroidSkiaXamlRootHost must be created on the dispatcher");
		_animationImplementor = new FrameCallbackImplementor(OnChoreographerTick);

		CompositionTarget.RenderingActiveChanged +=
			() => DispatchNextChoreographerTick();
	}

	private void OnChoreographerTick()
	{
		_pendingCallback = false;

		if (NativeDispatcher.Main.IsRendering)
		{
			// We're invoking the dispatcher synchronously as we're already
			// on the UI thread and running in continuous mode.
			NativeDispatcher.Main.SynchronousDispatchRendering();

			DispatchNextChoreographerTick();
		}
	}

	private void DispatchNextChoreographerTick()
	{
		if (!_pendingCallback)
		{
			_pendingCallback = true;
			_choreographer.PostFrameCallback(_animationImplementor);
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

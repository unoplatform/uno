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

	public AndroidChoreographerHandler()
	{
		_choreographer = Choreographer.Instance ?? throw new InvalidOperationException("AndroidSkiaXamlRootHost must be created on the dispatcher");
		_animationImplementor = new FrameCallbackImplementor(OnChoreographerTick);

		CompositionTarget.RenderingActiveChanged +=
			() => DispatchNextChoreographerTick();
	}

	private void OnChoreographerTick()
	{
		if (NativeDispatcher.Main.IsRendering)
		{
			// We're invoking the dispatcher synchronously as we're already
			// on the UI thread and running in continuous mode.

			NativeDispatcher.Main.SynchronousDispatchRendering();
			ApplicationActivity.Instance?.InvalidateRender();

			DispatchNextChoreographerTick();
		}
	}

	private void DispatchNextChoreographerTick()
		=> _choreographer.PostFrameCallback(_animationImplementor);

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

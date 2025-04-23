using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android.OS;
using Android.Views;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml;

internal sealed class CompositionTargetChoreographer : Java.Lang.Object, Choreographer.IFrameCallback
{
	private readonly Thread _thread;
	private readonly TaskCompletionSource<Choreographer> _choreographer = new();

	public CompositionTargetChoreographer()
	{
		CompositionTarget.RenderingActiveChanged += OnRenderingActiveChanged;

		_thread = new Thread(Loop);
		_thread.Start();
	}

	private void OnRenderingActiveChanged()
	{
		if (CompositionTarget.IsRenderingActive)
		{
			Choreographer.Instance!.PostFrameCallback(this);
		}
	}

	public bool RunsInBackground => true;

	private void Loop()
	{
		Looper.Prepare();
		_choreographer.SetResult(Choreographer.Instance!);
		Looper.Loop();
	}

	public void DoFrame(long frameTimeNanos)
	{
		NativeDispatcher.Main.DispatchRendering();

		if (CompositionTarget.IsRenderingActive)
		{
			Choreographer.Instance!.PostFrameCallback(this);
		}
	}
}

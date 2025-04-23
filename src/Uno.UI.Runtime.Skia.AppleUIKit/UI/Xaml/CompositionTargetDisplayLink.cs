using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.UI.Xaml;

internal sealed class CompositionTargetDisplayLink
{
	public CompositionTargetDisplayLink()
	{
		CompositionTarget.RenderingActiveChanged += OnRenderingActiveChanged;
	}

	private void OnRenderingActiveChanged()
	{
		if (CompositionTarget.IsRenderingActive)
		{
			_choreographer.Task.Result.PostFrameCallback(this);
		}
	}

	public bool RunsInBackground => true;

	private void Loop()
	{
		Looper.Prepare();
		_choreographer.TrySetResult(Choreographer.Instance!);
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

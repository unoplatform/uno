#nullable enable

using System;
using System.Threading;
using Uno.UI;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml.Media;

/// <summary>
/// Use a generic frame timer instead of the native one, generally 
/// in the context of desktop targets.
/// </summary>
internal class CompositionTargetTimer
{
	private static Timer? _renderTimer;

	internal static void Start()
	{
#if __SKIA__
		CompositionTarget.RenderingActiveChanged += UpdateTimer;
#else
		throw new PlatformNotSupportedException();
#endif
	}

#if __SKIA__
	private static void UpdateTimer()
	{
		if (CompositionTarget.IsRenderingActive)
		{
			_renderTimer ??= new Timer(_ => NativeDispatcher.Main.DispatchRendering());
			_renderTimer.Change(TimeSpan.Zero, TimeSpan.FromSeconds(1 / FeatureConfiguration.CompositionTarget.FrameRate));
		}
		else
		{
			_renderTimer?.Change(Timeout.Infinite, Timeout.Infinite);
		}
	}
#endif
}

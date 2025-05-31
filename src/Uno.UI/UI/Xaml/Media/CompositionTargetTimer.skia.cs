﻿#nullable enable

using System;
using System.Threading;
using Uno.UI;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml.Media;

/// <summary>
/// Use a generic frame timer instead of the native one, generally 
/// in the context of desktop targets.
/// </summary>
internal partial class CompositionTargetTimer
{
	private static Timer? _renderTimer;

	private static void StartInternal()
	{
		CompositionTarget.RenderingActiveChanged += UpdateTimer;
	}

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
}

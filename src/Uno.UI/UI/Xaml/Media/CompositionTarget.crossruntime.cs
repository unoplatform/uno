using System;
using System.Diagnostics;
using Uno.UI.Composition;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	private static readonly long _start = Stopwatch.GetTimestamp();
	private static Action _renderingActiveChanged;
	private static bool _isRenderingActive;

	private static event EventHandler<object> _rendering;

	public static event EventHandler<object> Rendering
	{
		add
		{
			NativeDispatcher.CheckThreadAccess();
			_rendering += value;
			IsRenderingActive = true;
		}
		remove
		{
			NativeDispatcher.CheckThreadAccess();
			_rendering -= value;
			IsRenderingActive = _rendering is not null;
		}
	}

	internal static event Action RenderingActiveChanged
	{
		add => _renderingActiveChanged += value;
		remove => _renderingActiveChanged -= value;
	}

	/// <summary>
	/// Determines if the CompositionTarget rendering is active.
	/// </summary>
	internal static bool IsRenderingActive
	{
		get => _isRenderingActive;
		set
		{
			if (value != _isRenderingActive)
			{
				_isRenderingActive = value;
				_renderingActiveChanged?.Invoke();
			}
		}
	}

	internal static void InvokeRendering()
	{
		if (NativeDispatcher.Main.HasThreadAccess)
		{
			_rendering?.Invoke(null, new RenderingEventArgs(Stopwatch.GetElapsedTime(_start)));
		}
		else
		{
			NativeDispatcher.Main.Enqueue(() =>
			{
				_rendering?.Invoke(null, new RenderingEventArgs(Stopwatch.GetElapsedTime(_start)));
			}, NativeDispatcherPriority.High);
		}
	}

#if __SKIA__
	void ICompositionTarget.RequestNewFrame() => ContentRoot?.XamlRoot?.RequestNewFrame();
#endif
}

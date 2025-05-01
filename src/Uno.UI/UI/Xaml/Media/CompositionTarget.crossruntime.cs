using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.UI;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
	private static Action _renderingActiveChanged;
	private static bool _isRenderingActive;

	public static event EventHandler<object> Rendering
	{
		add
		{
			NativeDispatcher.CheckThreadAccess();

			var currentlyRaisingEvents = NativeDispatcher.Main.IsRendering;
			NativeDispatcher.Main.Rendering += value;
			NativeDispatcher.Main.RenderingEventArgsGenerator ??= (d => new RenderingEventArgs(d));

			IsRenderingActive = NativeDispatcher.Main.IsRendering;

			if (!currentlyRaisingEvents)
			{
				// Queue the first render, so that the native surfaces
				// timers can pick up the subsquent renders.
				NativeDispatcher.Main.DispatchRendering();
			}
		}
		remove
		{
			NativeDispatcher.CheckThreadAccess();

			NativeDispatcher.Main.Rendering -= value;

			IsRenderingActive = NativeDispatcher.Main.IsRendering;
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
}

using System;
using Uno.UI.Dispatching;

namespace Microsoft.UI.Xaml.Media;

public partial class CompositionTarget
{
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
}

#nullable enable

using System.Collections.Concurrent;
using Windows.Foundation;

namespace Microsoft.UI.Input;

/// <summary>
/// Tracks the input activation state of a window.
/// </summary>
public partial class InputActivationListener : InputObject
{
	private static readonly ConcurrentDictionary<WindowId, InputActivationListener> _listeners = new();

	private InputActivationListener()
	{
	}

	/// <summary>
	/// Gets the current input activation state.
	/// </summary>
	public InputActivationState State { get; private set; } = InputActivationState.Deactivated;

	/// <summary>
	/// Occurs when the input activation state changes.
	/// </summary>
	public event TypedEventHandler<InputActivationListener, InputActivationListenerActivationChangedEventArgs>? InputActivationChanged;

	/// <summary>
	/// Gets the InputActivationListener for the specified window.
	/// </summary>
	public static InputActivationListener GetForWindowId(WindowId windowId) =>
		_listeners.GetOrAdd(windowId, static _ => new InputActivationListener());

	internal static void SetStateForWindowId(WindowId windowId, InputActivationState state)
	{
		var listener = GetForWindowId(windowId);
		if (listener.State != state)
		{
			listener.State = state;
			listener.InputActivationChanged?.Invoke(listener, new InputActivationListenerActivationChangedEventArgs());
		}
	}

	internal static void DestroyForWindowId(WindowId windowId) => _listeners.TryRemove(windowId, out _);
}

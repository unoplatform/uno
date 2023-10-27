using Uno.UI.RemoteControl.HotReload;
using Windows.UI.Xaml;

namespace Uno.UI;

/// <summary>
/// Extension methods for the Window class
/// </summary>
public static class WindowExtensions
{
	/// <summary>
	/// Enables the UI Update cycle of HotReload to be handled by Uno
	/// </summary>
	/// <param name="window">The window of the application where UI updates will be applied</param>
	public static void EnableHotReload(this Window window) => ClientHotReloadProcessor.CurrentWindow = window;
}

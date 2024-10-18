using System;
using Uno.UI.RemoteControl.HotReload;
using Windows.UI.Xaml;
using Uno.Diagnostics.UI;

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
	public static void EnableHotReload(this Window window)
		=> ClientHotReloadProcessor.SetWindow(window, false);

	/// <summary>
	/// Enables the UI Update cycle of HotReload to be handled by Uno
	/// </summary>
	/// <param name="window">The window of the application where UI updates will be applied</param>
	/// <param name="disableIndicator">Request to not show the on-canvas indicator by default.</param>
	public static void EnableHotReload(this Window window, bool disableIndicator)
		=> ClientHotReloadProcessor.SetWindow(window, disableIndicator);

	/// <summary>
	/// Forces the layout of the window to be update with any HotReload changes
	/// that may have occurred when Hot Reload updating was paused
	/// </summary>
	/// <param name="window">The window of the application to be updated</param>
	/// <remarks>Currently this method doesn't use the window instance. However, with the addition of multi-window
	/// support it's likely that the instance will be needed to determine the window where updates will be applied</remarks>
	public static void ForceHotReloadUpdate(this Window window)
		=> ClientHotReloadProcessor.ForceHotReloadUpdate();
}

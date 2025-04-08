using System;
using System.ComponentModel;

namespace Windows.UI.Core.Preview;

/// <summary>
/// Provides a way for an app to respond to system provided close events.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public partial class SystemNavigationManagerPreview
{
	/// <summary>
	/// Returns the SystemNavigationManagerPreview object associated with the current window.
	/// </summary>
	/// <returns>The SystemNavigationManagerPreview object associated with the current window.</returns>
	public static SystemNavigationManagerPreview GetForCurrentView() =>
		throw new NotSupportedException("SystemNavigationManagerPreview is no longer supported. Use Window.AppWindow.Closing instead.");
}

#if !HAS_UNO_WINUI
#nullable enable

using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml;

/// <summary>
/// Provides data for the OnWindowCreated method.
/// </summary>
public sealed partial class WindowCreatedEventArgs
{
	internal WindowCreatedEventArgs(Window window) => Window = window;

	/// <summary>
	/// Gets the window that was created.
	/// </summary>
	public Window Window { get; }
}
#endif

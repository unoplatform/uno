#nullable enable

using System;
using Windows.Graphics;

namespace Microsoft.UI.Windowing;

/// <summary>
/// Represents the title bar of an app window.
/// </summary>
public partial class AppWindowTitleBar
{
	private bool _extendsContentIntoTitleBar;

	internal AppWindowTitleBar(AppWindow appWindow)
	{
	}

	internal event Action<bool>? ExtendsContentIntoTitleBarChanged;

	/// <summary>
	/// Gets or sets a value that specifies whether app content extends into the title bar area.
	/// </summary>
	public bool ExtendsContentIntoTitleBar
	{
		get => _extendsContentIntoTitleBar;
		set
		{
			if (_extendsContentIntoTitleBar != value)
			{
				_extendsContentIntoTitleBar = value;
				ExtendsContentIntoTitleBarChanged?.Invoke(value);
			}
		}
	}

	/// <summary>
	/// Gets a value that indicates whether the title bar can be customized.
	/// </summary>
	/// <returns>True if the title bar can be customized; otherwise, false.</returns>
	/// <remarks>Title bar customization is currently not available on any Uno Platform target except WinAppSDK.</remarks>
	public static bool IsCustomizationSupported() => OperatingSystem.IsWindows();

    /// <summary>
    /// Sets the drag regions for the window.
    /// </summary>
    /// <param name="value">An array of RectInt32, where each rectangle must be within the client area of the window to which the title bar belongs.</param>
	public void SetDragRectangles(RectInt32[] value) =>
		DragRectanglesChanged?.Invoke(this, value);

	internal event EventHandler<RectInt32[]>? DragRectanglesChanged;
}

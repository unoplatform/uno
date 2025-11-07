#nullable enable

using System;
using Uno.Foundation.Logging;
using Windows.Graphics;

namespace Microsoft.UI.Windowing;

/// <summary>
/// Represents the title bar of an app window.
/// </summary>
public partial class AppWindowTitleBar
{
	private bool _extendsContentIntoTitleBar;
	private TitleBarHeightOption _preferredHeightOption;
	private readonly AppWindow _appWindow;

	internal AppWindowTitleBar(AppWindow appWindow)
	{
		_appWindow = appWindow;
	}

	internal event Action<bool>? ExtendsContentIntoTitleBarChanged;

	internal event EventHandler? Changed;

	/// <summary>
	/// Gets or sets a value that specifies whether app content extends into the title bar area.
	/// </summary>
	/// <remarks>
	/// Currently only has effect on Desktop Windows and WinAppSDK targets.
	/// </remarks>
	public bool ExtendsContentIntoTitleBar
	{
		get => _extendsContentIntoTitleBar;
		set
		{
			if (!IsCustomizationSupported() && value)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning("AppWindowTitleBar customization is not supported on this platform.");
				}
			}

			if (_extendsContentIntoTitleBar != value)
			{
				_extendsContentIntoTitleBar = value;
				ExtendsContentIntoTitleBarChanged?.Invoke(value);
				Changed?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	/// <summary>
	/// Gets the height of the title bar in client coordinates.
	/// </summary>
	public int Height
	{
		get
		{
			var rasterizationScale = _appWindow.NativeAppWindow.RasterizationScale;
			var scaleIndependentHeight = PreferredHeightOption switch
			{
				TitleBarHeightOption.Collapsed => 0,
				TitleBarHeightOption.Standard => 32,
				TitleBarHeightOption.Tall => 48,
				_ => throw new NotSupportedException("The specified TitleBarHeightOption is not supported."),
			};
			return (int)(scaleIndependentHeight * rasterizationScale);
		}
	}

	/// <summary>
	/// Gets or sets a value that specifies the height options of the title bar
	/// when the AppWindowTitleBar.ExtendsContentIntoTitleBar property is true.
	/// </summary>
	public TitleBarHeightOption PreferredHeightOption
	{
		get => _preferredHeightOption;
		set
		{
			if (_preferredHeightOption != value)
			{
				_preferredHeightOption = value;
				Changed?.Invoke(this, EventArgs.Empty);
			}
		}
	}

	internal RectInt32[] DragRectangles { get; private set; } = [];

	/// <summary>
	/// Gets a value that indicates whether the title bar can be customized.
	/// </summary>
	/// <returns>True if the title bar can be customized; otherwise, false.</returns>
	/// <remarks>Title bar customization is currently not available on any Uno Platform target except WinAppSDK.</remarks>
	public static bool IsCustomizationSupported() => OperatingSystem.IsWindows();

	/// <summary>
	/// Resets the title bar to its default state.
	/// </summary>

	public void ResetToDefault()
	{
		SetDragRectangles([]);
		PreferredHeightOption = TitleBarHeightOption.Standard;
		ExtendsContentIntoTitleBar = false;
	}

	/// <summary>
	/// Sets the drag regions for the window.
	/// </summary>
	/// <param name="value">An array of RectInt32, where each rectangle must be within the client area of the window to which the title bar belongs.</param>
	public void SetDragRectangles(RectInt32[] value)
	{
		DragRectangles = value ?? [];
		Changed?.Invoke(this, EventArgs.Empty);
	}
}

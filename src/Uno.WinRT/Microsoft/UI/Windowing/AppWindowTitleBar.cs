#nullable enable

using System;
using Uno.Foundation.Logging;
using Windows.Graphics;
using Windows.UI;

namespace Microsoft.UI.Windowing;

/// <summary>
/// Represents the title bar of an app window.
/// </summary>
public partial class AppWindowTitleBar
{
	private bool _extendsContentIntoTitleBar;
	private TitleBarHeightOption _preferredHeightOption;
	private readonly AppWindow _appWindow;
	private Color? _buttonBackgroundColor;
	private Color? _buttonForegroundColor;
	private Color? _buttonHoverBackgroundColor;
	private Color? _buttonHoverForegroundColor;
	private Color? _buttonInactiveBackgroundColor;
	private Color? _buttonInactiveForegroundColor;
	private Color? _buttonPressedBackgroundColor;
	private Color? _buttonPressedForegroundColor;

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
	/// Currently only has effect on Desktop Windows, Desktop macOS and WinAppSDK targets.
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

	/// <summary>
	/// Gets the width of the system-reserved region of the upper-left corner of the app window.
	/// </summary>
	public int LeftInset { get; private set; }

	/// <summary>
	/// Gets the width of the system-reserved region of the upper-right corner of the app window.
	/// </summary>
	public int RightInset { get; private set; }

	internal event EventHandler? InsetsChanged;

	internal void SetInsets(int leftInset, int rightInset)
	{
		if (LeftInset != leftInset || RightInset != rightInset)
		{
			LeftInset = leftInset;
			RightInset = rightInset;
			InsetsChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	internal RectInt32[] DragRectangles { get; private set; } = [];

	internal event EventHandler? ButtonColorsChanged;

	private void SetButtonColor(ref Color? field, Color? value)
	{
		if (!Nullable.Equals(field, value))
		{
			field = value;
			ButtonColorsChanged?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>
	/// Gets or sets the background color of the title bar.
	/// </summary>
	/// <remarks>Stored only: Skia desktop hosts do not apply it (the macOS title bar and traffic lights are drawn by the system).</remarks>
	public Color? BackgroundColor { get; set; }

	/// <summary>
	/// Gets or sets the button background color of the title bar.
	/// </summary>
	/// <remarks>Applied to the caption buttons on Desktop Windows. Stored only on Desktop macOS, where the system draws the traffic lights.</remarks>
	public Color? ButtonBackgroundColor
	{
		get => _buttonBackgroundColor;
		set => SetButtonColor(ref _buttonBackgroundColor, value);
	}

	/// <summary>
	/// Gets or sets the button foreground color of the title bar.
	/// </summary>
	/// <remarks>Applied to the caption buttons on Desktop Windows. Stored only on Desktop macOS, where the system draws the traffic lights.</remarks>
	public Color? ButtonForegroundColor
	{
		get => _buttonForegroundColor;
		set => SetButtonColor(ref _buttonForegroundColor, value);
	}

	/// <summary>
	/// Gets or sets the button hover background color of the title bar.
	/// </summary>
	/// <remarks>Applied to the caption buttons on Desktop Windows. Stored only on Desktop macOS, where the system draws the traffic lights.</remarks>
	public Color? ButtonHoverBackgroundColor
	{
		get => _buttonHoverBackgroundColor;
		set => SetButtonColor(ref _buttonHoverBackgroundColor, value);
	}

	/// <summary>
	/// Gets or sets the button hover foreground color of the title bar.
	/// </summary>
	/// <remarks>Applied to the caption buttons on Desktop Windows. Stored only on Desktop macOS, where the system draws the traffic lights.</remarks>
	public Color? ButtonHoverForegroundColor
	{
		get => _buttonHoverForegroundColor;
		set => SetButtonColor(ref _buttonHoverForegroundColor, value);
	}

	/// <summary>
	/// Gets or sets the button inactive background color of the title bar.
	/// </summary>
	/// <remarks>Applied to the caption buttons on Desktop Windows. Stored only on Desktop macOS, where the system draws the traffic lights.</remarks>
	public Color? ButtonInactiveBackgroundColor
	{
		get => _buttonInactiveBackgroundColor;
		set => SetButtonColor(ref _buttonInactiveBackgroundColor, value);
	}

	/// <summary>
	/// Gets or sets the button inactive foreground color of the title bar.
	/// </summary>
	/// <remarks>Applied to the caption buttons on Desktop Windows. Stored only on Desktop macOS, where the system draws the traffic lights.</remarks>
	public Color? ButtonInactiveForegroundColor
	{
		get => _buttonInactiveForegroundColor;
		set => SetButtonColor(ref _buttonInactiveForegroundColor, value);
	}

	/// <summary>
	/// Gets or sets the button pressed background color of the title bar.
	/// </summary>
	/// <remarks>Applied to the caption buttons on Desktop Windows. Stored only on Desktop macOS, where the system draws the traffic lights.</remarks>
	public Color? ButtonPressedBackgroundColor
	{
		get => _buttonPressedBackgroundColor;
		set => SetButtonColor(ref _buttonPressedBackgroundColor, value);
	}

	/// <summary>
	/// Gets or sets the button pressed foreground color of the title bar.
	/// </summary>
	/// <remarks>Applied to the caption buttons on Desktop Windows. Stored only on Desktop macOS, where the system draws the traffic lights.</remarks>
	public Color? ButtonPressedForegroundColor
	{
		get => _buttonPressedForegroundColor;
		set => SetButtonColor(ref _buttonPressedForegroundColor, value);
	}

	/// <summary>
	/// Gets or sets the foreground color of the title bar.
	/// </summary>
	/// <remarks>Stored only: Skia desktop hosts do not apply it (the macOS title bar and traffic lights are drawn by the system).</remarks>
	public Color? ForegroundColor { get; set; }

	/// <summary>
	/// Gets or sets the inactive background color of the title bar.
	/// </summary>
	/// <remarks>Stored only: Skia desktop hosts do not apply it (the macOS title bar and traffic lights are drawn by the system).</remarks>
	public Color? InactiveBackgroundColor { get; set; }

	/// <summary>
	/// Gets or sets the inactive foreground color of the title bar.
	/// </summary>
	/// <remarks>Stored only: Skia desktop hosts do not apply it (the macOS title bar and traffic lights are drawn by the system).</remarks>
	public Color? InactiveForegroundColor { get; set; }

	/// <summary>
	/// Gets or sets a value that specifies how the window icon is shown in the title bar.
	/// </summary>
	/// <remarks>Stored only: Skia desktop hosts do not apply it.</remarks>
	public IconShowOptions IconShowOptions { get; set; }

	/// <summary>
	/// Gets a value that indicates whether the title bar can be customized.
	/// </summary>
	/// <returns>True if the title bar can be customized; otherwise, false.</returns>
	/// <remarks>Title bar customization is currently available on WinAppSDK, Desktop Windows and Desktop macOS.</remarks>
	public static bool IsCustomizationSupported() => OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

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

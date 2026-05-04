using System;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;
using Windows.UI;

namespace UITests.Microsoft_UI_Windowing;

[Sample("Windowing", Name = "AppWindowTitleBar Properties", ViewModelType = typeof(AppWindowTitleBarPropertiesViewModel), Description = "Demonstrates setting AppWindowTitleBar properties.")]
public sealed partial class AppWindowTitleBarProperties : Page
{
	public AppWindowTitleBarProperties()
	{
		this.InitializeComponent();
	}
}

internal class AppWindowTitleBarPropertiesViewModel : ViewModelBase
{
	private readonly AppWindowTitleBar _titleBar;

	public AppWindowTitleBarPropertiesViewModel()
	{
		var appWindow = Microsoft.UI.Xaml.Window.Current?.AppWindow;
		_titleBar = appWindow?.TitleBar;
		// Probe all properties for availability
		IsBackgroundColorAvailable = Probe(() => { var _ = _titleBar.BackgroundColor; });
		IsInactiveBackgroundColorAvailable = Probe(() => { var _ = _titleBar.InactiveBackgroundColor; });
		IsForegroundColorAvailable = Probe(() => { var _ = _titleBar.ForegroundColor; });
		IsInactiveForegroundColorAvailable = Probe(() => { var _ = _titleBar.InactiveForegroundColor; });
		IsButtonBackgroundColorAvailable = Probe(() => { var _ = _titleBar.ButtonBackgroundColor; });
		IsButtonInactiveBackgroundColorAvailable = Probe(() => { var _ = _titleBar.ButtonInactiveBackgroundColor; });
		IsButtonForegroundColorAvailable = Probe(() => { var _ = _titleBar.ButtonForegroundColor; });
		IsButtonInactiveForegroundColorAvailable = Probe(() => { var _ = _titleBar.ButtonInactiveForegroundColor; });
		IsButtonHoverBackgroundColorAvailable = Probe(() => { var _ = _titleBar.ButtonHoverBackgroundColor; });
		IsButtonHoverForegroundColorAvailable = Probe(() => { var _ = _titleBar.ButtonHoverForegroundColor; });
		IsButtonPressedBackgroundColorAvailable = Probe(() => { var _ = _titleBar.ButtonPressedBackgroundColor; });
		IsButtonPressedForegroundColorAvailable = Probe(() => { var _ = _titleBar.ButtonPressedForegroundColor; });
	}

	private bool Probe(Action getter)
	{
		try { getter(); return true; } catch { return false; }
	}

	public bool IsBackgroundColorAvailable { get; }
	public bool IsInactiveBackgroundColorAvailable { get; }
	public bool IsForegroundColorAvailable { get; }
	public bool IsInactiveForegroundColorAvailable { get; }
	public bool IsButtonBackgroundColorAvailable { get; }
	public bool IsButtonInactiveBackgroundColorAvailable { get; }
	public bool IsButtonForegroundColorAvailable { get; }
	public bool IsButtonInactiveForegroundColorAvailable { get; }
	public bool IsButtonHoverBackgroundColorAvailable { get; }
	public bool IsButtonHoverForegroundColorAvailable { get; }
	public bool IsButtonPressedBackgroundColorAvailable { get; }
	public bool IsButtonPressedForegroundColorAvailable { get; }

	public Color BackgroundColor
	{
		get
		{
			try { return _titleBar?.BackgroundColor ?? Colors.Transparent; } catch { return Colors.Transparent; }
		}
		set
		{
			try { if (_titleBar != null && _titleBar.BackgroundColor != value) { _titleBar.BackgroundColor = value; RaisePropertyChanged(); } } catch { }
		}
	}

	public Color InactiveBackgroundColor
	{
		get { try { return _titleBar?.InactiveBackgroundColor ?? Colors.Transparent; } catch { return Colors.Transparent; } }
		set { try { if (_titleBar != null && _titleBar.InactiveBackgroundColor != value) { _titleBar.InactiveBackgroundColor = value; RaisePropertyChanged(); } } catch { } }
	}

	public Color ForegroundColor
	{
		get { try { return _titleBar?.ForegroundColor ?? Colors.Transparent; } catch { return Colors.Transparent; } }
		set { try { if (_titleBar != null && _titleBar.ForegroundColor != value) { _titleBar.ForegroundColor = value; RaisePropertyChanged(); } } catch { } }
	}

	public Color InactiveForegroundColor
	{
		get { try { return _titleBar?.InactiveForegroundColor ?? Colors.Transparent; } catch { return Colors.Transparent; } }
		set { try { if (_titleBar != null && _titleBar.InactiveForegroundColor != value) { _titleBar.InactiveForegroundColor = value; RaisePropertyChanged(); } } catch { } }
	}

	public Color ButtonBackgroundColor
	{
		get { try { return _titleBar?.ButtonBackgroundColor ?? Colors.Transparent; } catch { return Colors.Transparent; } }
		set { try { if (_titleBar != null && _titleBar.ButtonBackgroundColor != value) { _titleBar.ButtonBackgroundColor = value; RaisePropertyChanged(); } } catch { } }
	}

	public Color ButtonInactiveBackgroundColor
	{
		get { try { return _titleBar?.ButtonInactiveBackgroundColor ?? Colors.Transparent; } catch { return Colors.Transparent; } }
		set { try { if (_titleBar != null && _titleBar.ButtonInactiveBackgroundColor != value) { _titleBar.ButtonInactiveBackgroundColor = value; RaisePropertyChanged(); } } catch { } }
	}

	public Color ButtonForegroundColor
	{
		get { try { return _titleBar?.ButtonForegroundColor ?? Colors.Transparent; } catch { return Colors.Transparent; } }
		set { try { if (_titleBar != null && _titleBar.ButtonForegroundColor != value) { _titleBar.ButtonForegroundColor = value; RaisePropertyChanged(); } } catch { } }
	}

	public Color ButtonInactiveForegroundColor
	{
		get { try { return _titleBar?.ButtonInactiveForegroundColor ?? Colors.Transparent; } catch { return Colors.Transparent; } }
		set { try { if (_titleBar != null && _titleBar.ButtonInactiveForegroundColor != value) { _titleBar.ButtonInactiveForegroundColor = value; RaisePropertyChanged(); } } catch { } }
	}

	public Color ButtonHoverBackgroundColor
	{
		get { try { return _titleBar?.ButtonHoverBackgroundColor ?? Colors.Transparent; } catch { return Colors.Transparent; } }
		set { try { if (_titleBar != null && _titleBar.ButtonHoverBackgroundColor != value) { _titleBar.ButtonHoverBackgroundColor = value; RaisePropertyChanged(); } } catch { } }
	}

	public Color ButtonHoverForegroundColor
	{
		get { try { return _titleBar?.ButtonHoverForegroundColor ?? Colors.Transparent; } catch { return Colors.Transparent; } }
		set { try { if (_titleBar != null && _titleBar.ButtonHoverForegroundColor != value) { _titleBar.ButtonHoverForegroundColor = value; RaisePropertyChanged(); } } catch { } }
	}

	public Color ButtonPressedBackgroundColor
	{
		get { try { return _titleBar?.ButtonPressedBackgroundColor ?? Colors.Transparent; } catch { return Colors.Transparent; } }
		set { try { if (_titleBar != null && _titleBar.ButtonPressedBackgroundColor != value) { _titleBar.ButtonPressedBackgroundColor = value; RaisePropertyChanged(); } } catch { } }
	}

	public Color ButtonPressedForegroundColor
	{
		get { try { return _titleBar?.ButtonPressedForegroundColor ?? Colors.Transparent; } catch { return Colors.Transparent; } }
		set { try { if (_titleBar != null && _titleBar.ButtonPressedForegroundColor != value) { _titleBar.ButtonPressedForegroundColor = value; RaisePropertyChanged(); } } catch { } }
	}

	public TitleBarHeightOption[] TitleBarHeightOptions { get; } = [TitleBarHeightOption.Collapsed, TitleBarHeightOption.Standard, TitleBarHeightOption.Tall];

	public TitleBarHeightOption PreferredHeightOption
	{
		get => _titleBar?.PreferredHeightOption ?? TitleBarHeightOption.Standard;
		set
		{
			try
			{
				if (_titleBar != null)
				{
					_titleBar.PreferredHeightOption = value;
					RaisePropertyChanged();
				}
			}
			catch { }
		}
	}
}

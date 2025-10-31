using System;
using Microsoft.UI.Windowing.Native;

namespace Microsoft.UI.Windowing;

/// <summary>
/// Displays an app window using an overlapped configuration.
/// </summary>
public partial class OverlappedPresenter : AppWindowPresenter
{
	private bool _isResizable = true;
	private bool _isModal;
	private bool _isMinimizable;
	private bool _isMaximizable;
	private bool _isAlwaysOnTop;
	private int? _preferredMinimumHeight;
	private int? _preferredMinimumWidth;
	private int? _preferredMaximumWidth;
	private int? _preferredMaximumHeight;
	private OverlappedPresenterState? _pendingState;

	internal OverlappedPresenter() : base(AppWindowPresenterKind.Overlapped)
	{
	}

	internal event EventHandler BorderAndTitleBarChanged;

	/// <summary>
	/// Gets a value that indicates whether this window has a border.
	/// </summary>
	public bool HasBorder { get; private set; } = true;

	/// <summary>
	/// Gets a value that indicates whether this window has a title bar.
	/// </summary>
	public bool HasTitleBar { get; private set; } = true;

	/// <summary>
	/// Gets or sets a value that indicates whether this window will be kept on top of other windows.
	/// </summary>
	public bool IsAlwaysOnTop
	{
		get => _isAlwaysOnTop;
		set
		{
			if (_isAlwaysOnTop != value)
			{
				_isAlwaysOnTop = value;
				Native?.SetIsAlwaysOnTop(value);
			}
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether this window can be maximized.
	/// </summary>
	public bool IsMaximizable
	{
		get => _isMaximizable;
		set
		{
			if (_isMaximizable != value)
			{
				_isMaximizable = value;
				Native?.SetIsMaximizable(value);
			}
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether this window can be minimized.
	/// </summary>
	public bool IsMinimizable
	{
		get => _isMinimizable;
		set
		{
			if (_isMinimizable != value)
			{
				_isMinimizable = value;
				Native?.SetIsMinimizable(value);
				NotifyAppWindow();
			}
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether this window is modal.
	/// </summary>
	public bool IsModal
	{
		get => _isModal;
		set
		{
			if (_isModal != value)
			{
				_isModal = value;
				Native?.SetIsModal(value);
				NotifyAppWindow();
			}
		}
	}

	/// <summary>
	/// Gets or sets a value that indicates whether this window can be resized.
	/// </summary>
	public bool IsResizable
	{
		get => _isResizable;
		set
		{
			if (_isResizable != value)
			{
				_isResizable = value;
				Native?.SetIsResizable(value);
				NotifyAppWindow();
			}
		}
	}

	/// <summary>
	/// Gets or sets the preferred minimum width for the window.
	/// </summary>
	public int? PreferredMinimumWidth
	{
		get => _preferredMinimumWidth;
		set
		{
			if (_preferredMinimumWidth != value)
			{
				_preferredMinimumWidth = value;
				NotifyNativeWindowConstrains();
			}
		}
	}

	/// <summary>
	/// Gets or sets the preferred minimum height for the window.
	/// </summary>
	public int? PreferredMinimumHeight
	{
		get => _preferredMinimumHeight;
		set
		{
			if (_preferredMinimumHeight != value)
			{
				_preferredMinimumHeight = value;
				NotifyNativeWindowConstrains();
			}
		}
	}

	/// <summary>
	/// Gets or sets the preferred maximum width for the window.
	/// </summary>
	public int? PreferredMaximumWidth
	{
		get => _preferredMaximumWidth;
		set
		{
			if (_preferredMaximumWidth != value)
			{
				_preferredMaximumWidth = value;
				NotifyNativeWindowConstrains();
			}
		}
	}

	/// <summary>
	/// Gets or sets the preferred maximum height for the window.
	/// </summary>
	public int? PreferredMaximumHeight
	{
		get => _preferredMaximumHeight;
		set
		{
			if (_preferredMaximumHeight != value)
			{
				_preferredMaximumHeight = value;
				NotifyNativeWindowConstrains();
			}
		}
	}

	/// <summary>
	/// Gets a value that specifies the OverlappedPresenterState that the window will assume when AppWindow.ShowOnceWithRequestedStartupState is called.
	/// </summary>
	public static OverlappedPresenterState RequestedStartupState { get; } = OverlappedPresenterState.Restored;

	/// <summary>
	/// Gets the state of the presenter.
	/// </summary>
	public OverlappedPresenterState State => Native?.State ?? _pendingState ?? OverlappedPresenterState.Restored;

	internal INativeOverlappedPresenter Native { get; private set; }

	/// <summary>
	/// Restores the window to the size and position it had before it was minimized or maximized.
	/// </summary>
	public void Restore() => Restore(false);

	/// <summary>
	/// Restores the window that the presenter is applied to and optionally makes it active.
	/// </summary>
	/// <param name="activateWindow">true if the window should be made active; otherwise, false.</param>
	public void Restore(bool activateWindow)
	{
		if (Native is not null)
		{
			Native.Restore(activateWindow);
			NotifyAppWindow();
		}
		else
		{
			_pendingState = OverlappedPresenterState.Restored;
		}
	}

	/// <summary>
	/// Maximizes the window that the presenter is applied to.
	/// </summary>
	public void Maximize()
	{
		if (Native is not null)
		{
			Native.Maximize();
			NotifyAppWindow();
		}
		else
		{
			_pendingState = OverlappedPresenterState.Maximized;
		}
	}

	/// <summary>
	/// Minimizes the window that the presenter is applied to.
	/// </summary>
	public void Minimize() => Minimize(false);

	/// <summary>
	/// Minimizes the window that the presenter is applied to and optionally makes it active.
	/// </summary>
	/// <param name="activateWindow">true if the window should be made active; otherwise, false.</param>
	public void Minimize(bool activateWindow)
	{
		if (Native is not null)
		{
			Native.Minimize(activateWindow);
			NotifyAppWindow();
		}
		else
		{
			_pendingState = OverlappedPresenterState.Minimized;
		}
	}

	/// <summary>
	/// Sets the border and title bar properties of the window.
	/// </summary>
	/// <param name="hasBorder">True if this window has a border; otherwise, false.</param>
	/// <param name="hasTitleBar">True if this window has a title bar; otherwise, false.</param>
	public void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar)
	{
		HasBorder = hasBorder;
		HasTitleBar = hasTitleBar;
		BorderAndTitleBarChanged?.Invoke(this, EventArgs.Empty);
		Native?.SetBorderAndTitleBar(hasBorder, hasTitleBar);
		NotifyAppWindow();
	}

	/// <summary>
	/// Creates a new instance of OverlappedPresenter.
	/// </summary>
	/// <returns>A new instance of OverlappedPresenter.</returns>
	public static OverlappedPresenter Create()
	{
		var presenter = new OverlappedPresenter()
		{
			IsAlwaysOnTop = false,
			IsMaximizable = true,
			IsMinimizable = true,
			IsModal = false,
			IsResizable = true,
		};
		presenter.SetBorderAndTitleBar(true, true);
		return presenter;
	}

	/// <summary>
	/// Creates an OverlappedPresenter object pre-populated with the values for a context menu.
	/// </summary>
	/// <returns>An OverlappedPresenter object pre-populated with the values for a context menu.</returns>
	public static OverlappedPresenter CreateForContextMenu()
	{
		var presenter = new OverlappedPresenter()
		{
			IsAlwaysOnTop = true,
			IsMaximizable = false,
			IsMinimizable = false,
			IsModal = false,
			IsResizable = false,
		};
		presenter.SetBorderAndTitleBar(true, false);
		return presenter;
	}

	/// <summary>
	/// Creates an OverlappedPresenter object pre-populated with the values for a dialog.
	/// </summary>
	/// <returns>An OverlappedPresenter object pre-populated with the values for a dialog.</returns>
	public static OverlappedPresenter CreateForDialog()
	{
		var presenter = new OverlappedPresenter()
		{
			IsAlwaysOnTop = true,
			IsMaximizable = false,
			IsMinimizable = false,
			IsModal = false,
			IsResizable = false,
		};
		presenter.SetBorderAndTitleBar(true, true);
		return presenter;
	}

	/// <summary>
	/// Creates an OverlappedPresenter object pre-populated with the values for a tool window.
	/// </summary> 
	/// <returns>An OverlappedPresenter object pre-populated with the values for a tool window.</returns>
	public static OverlappedPresenter CreateForToolWindow()
	{
		var presenter = new OverlappedPresenter()
		{
			IsAlwaysOnTop = false,
			IsMaximizable = true,
			IsMinimizable = true,
			IsModal = false,
			IsResizable = true,
		};
		presenter.SetBorderAndTitleBar(true, true);
		return presenter;
	}

	internal void SetNative(INativeOverlappedPresenter nativePresenter)
	{
		Native = nativePresenter;
		if (Native is not null)
		{
			// Apply initial values of all properties
			Native.SetIsModal(IsModal);
			Native.SetIsResizable(IsResizable);
			Native.SetIsMinimizable(IsMinimizable);
			Native.SetIsMaximizable(IsMaximizable);
			Native.SetIsAlwaysOnTop(IsAlwaysOnTop);
			Native.SetBorderAndTitleBar(HasBorder, HasTitleBar);

			if (_pendingState is OverlappedPresenterState.Maximized)
			{
				Native.Maximize();
			}
			else if (_pendingState is OverlappedPresenterState.Minimized)
			{
				Native.Minimize(false);
			}
			else
			{
				Native.Restore(false);
			}

			NotifyAppWindow();

			_pendingState = null;
		}
	}

	private void NotifyAppWindow() => Owner?.OnAppWindowChanged(AppWindowChangedEventArgs.PresenterChangedEventArgs);

	private void NotifyNativeWindowConstrains()
	{
		Native?.SetPreferredMaximumSize(GetEffectiveMaxWidth(), GetEffectiveMaxHeight());
		Native?.SetPreferredMinimumSize(PreferredMinimumWidth, PreferredMinimumHeight);
		NotifyAppWindow();
	}

	private int? GetEffectiveMaxWidth()
	{
		if (PreferredMaximumWidth is not null && PreferredMinimumWidth is not null)
		{
			return Math.Max(PreferredMaximumWidth.Value, PreferredMinimumWidth.Value);
		}

		return PreferredMaximumWidth;
	}

	private int? GetEffectiveMaxHeight()
	{
		if (PreferredMaximumHeight is not null && PreferredMinimumHeight is not null)
		{
			return Math.Max(PreferredMaximumHeight.Value, PreferredMinimumHeight.Value);
		}
		return PreferredMaximumHeight;
	}
}

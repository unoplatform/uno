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
	private OverlappedPresenterState? _pendingState;
	private bool _hasBorder;
	private bool _hasTitleBar;
	private int? _preferredMinimumHeight;
	private int? _preferredMinimumWidth;
	private int? _preferredMaximumWidth;
	private int? _preferredMaximumHeight;

	internal OverlappedPresenter() : base(AppWindowPresenterKind.Overlapped)
	{
	}

	internal INativeOverlappedPresenter Native { get; private set; }

	internal event EventHandler BorderAndTitleBarChanged;

	public bool IsResizable
	{
		get => _isResizable;
		set
		{
			_isResizable = value;
			Native?.SetIsResizable(value);
		}
	}

	public bool IsModal
	{
		get => _isModal;
		set
		{
			_isModal = value;
			Native?.SetIsModal(value);
		}
	}

	public bool IsMinimizable
	{
		get => _isMinimizable;
		set
		{
			_isMinimizable = value;
			Native?.SetIsMinimizable(value);
		}
	}

	public bool IsMaximizable
	{
		get => _isMaximizable;
		set
		{
			_isMaximizable = value;
			Native?.SetIsMaximizable(value);
		}
	}

	public bool IsAlwaysOnTop
	{
		get => _isAlwaysOnTop;
		set
		{
			_isAlwaysOnTop = value;
			Native?.SetIsAlwaysOnTop(value);
		}
	}

	public bool HasBorder { get; private set; } = true;

	public bool HasTitleBar { get; private set; } = true;

	public OverlappedPresenterState State => Native?.State ?? _pendingState ?? OverlappedPresenterState.Restored;

	public static OverlappedPresenterState RequestedStartupState { get; } = OverlappedPresenterState.Restored;

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
				Native?.SetPreferredMinimumSize(PreferredMinimumWidth, PreferredMinimumHeight);
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
				Native?.SetPreferredMinimumSize(PreferredMinimumWidth, PreferredMinimumHeight);
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
				Native?.SetPreferredMaximumSize(PreferredMaximumWidth, PreferredMaximumHeight);
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
				Native?.SetPreferredMaximumSize(PreferredMaximumWidth, PreferredMaximumHeight);
			}
		}
	}

	public void Restore() => Restore(false);

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
	/// 
	/// </summary>
	/// <param name="activateWindow"></param>
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
	}

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

	internal void NotifyAppWindow() => Owner?.OnAppWindowChanged(AppWindowChangedEventArgs.PresenterChangedEventArgs);
}

using Microsoft.UI.Windowing.Native;

namespace Microsoft.UI.Windowing;

/// <summary>
/// Displays an app window using an overlapped configuration.
/// </summary>
public partial class OverlappedPresenter : AppWindowPresenter
{
	private bool _isResizable;
	private bool _isModal;
	private bool _isMinimizable;
	private bool _isMaximizable;
	private bool _isAlwaysOnTop;
	private bool _hasBorder;
	private bool _hasTitleBar;
	private bool _pendingMaximize;
	private bool? _pendingMinimizeActivateWindow;

	internal OverlappedPresenter() : base(AppWindowPresenterKind.Overlapped)
	{
	}

	internal INativeOverlappedPresenter Native { get; private set; }

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

	public bool HasBorder => _hasBorder;

	public bool HasTitleBar => _hasTitleBar;

	public OverlappedPresenterState State => Native?.State ?? OverlappedPresenterState.Restored;

	public static OverlappedPresenterState RequestedStartupState { get; } = OverlappedPresenterState.Restored;

	public void Restore() => Restore(false);

	public void Maximize()
	{
		if (Native is not null)
		{
			Native.Maximize();
		}
		else
		{
			_pendingMaximize = true;
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
		}
		else
		{
			_pendingMinimizeActivateWindow = activateWindow;
		}
	}

	/// <summary>
	/// Sets the border and title bar properties of the window.
	/// </summary>
	/// <param name="hasBorder">True if this window has a border; otherwise, false.</param>
	/// <param name="hasTitleBar">True if this window has a title bar; otherwise, false.</param>
	public void SetBorderAndTitleBar(bool hasBorder, bool hasTitleBar)
	{
		_hasBorder = hasBorder;
		_hasTitleBar = hasTitleBar;
		Native?.SetBorderAndTitleBar(hasBorder, hasTitleBar);
	}

	public void Restore(bool activateWindow)
	{
		if (Native is not null)
		{
			Native.Restore(activateWindow);
		}
		else
		{
			_pendingMaximize = false;
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

			// Apply any pending actions
			if (_pendingMaximize)
			{
				Native.Maximize();
				_pendingMaximize = false;
			}

			if (_pendingMinimizeActivateWindow.HasValue)
			{
				Native.Minimize(_pendingMinimizeActivateWindow.Value);
				_pendingMinimizeActivateWindow = null;
			}
		}
	}
}

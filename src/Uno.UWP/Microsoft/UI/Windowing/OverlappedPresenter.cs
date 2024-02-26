namespace Microsoft.UI.Windowing;

/// <summary>
/// Displays an app window using an overlapped configuration.
/// </summary>
public partial class OverlappedPresenter : AppWindowPresenter
{
	internal OverlappedPresenter() : base(AppWindowPresenterKind.Overlapped)
	{
	}

	public bool IsResizable { get; set; }

	public bool IsModal { get; set; }

	public bool IsMinimizable { get; set; }

	public bool IsMaximizable { get; set; }

	public bool IsAlwaysOnTop { get; set; }

	public bool HasBorder { get; internal set; }

	public bool HasTitleBar { get; internal set; }

	public OverlappedPresenterState State { get; internal set; } = OverlappedPresenterState.Restored;

	public static OverlappedPresenterState RequestedStartupState { get; } = OverlappedPresenterState.Restored;

	public void Restore()
	{
	}

	public void Maximize()
	{

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
	}

	public void Restore(bool activateWindow)
	{
	}

	/// <summary>
	/// Creates a new instance of OverlappedPresenter.
	/// </summary>
	/// <returns>A new instance of OverlappedPresenter.</returns>
	public static OverlappedPresenter Create() => new OverlappedPresenter()
	{
		HasBorder = true,
		HasTitleBar = true,
		IsAlwaysOnTop = false,
		IsMaximizable = true,
		IsMinimizable = true,
		IsModal = false,
		IsResizable = true,
	};

	/// <summary>
	/// Creates an OverlappedPresenter object pre-populated with the values for a context menu.
	/// </summary>
	/// <returns>An OverlappedPresenter object pre-populated with the values for a context menu.</returns>
	public static OverlappedPresenter CreateForContextMenu() => new OverlappedPresenter()
	{
		HasBorder = true,
		HasTitleBar = false,
		IsAlwaysOnTop = true,
		IsMaximizable = false,
		IsMinimizable = false,
		IsModal = false,
		IsResizable = false,
	};

	/// <summary>
	/// Creates an OverlappedPresenter object pre-populated with the values for a dialog.
	/// </summary>
	/// <returns>An OverlappedPresenter object pre-populated with the values for a dialog.</returns>
	public static OverlappedPresenter CreateForDialog() => new OverlappedPresenter()
	{
		HasBorder = true,
		HasTitleBar = true,
		IsAlwaysOnTop = true,
		IsMaximizable = false,
		IsMinimizable = false,
		IsModal = false,
		IsResizable = false,
	};

	/// <summary>
	/// Creates an OverlappedPresenter object pre-populated with the values for a tool window.
	/// </summary>
	/// <returns>An OverlappedPresenter object pre-populated with the values for a tool window.</returns>
	public static OverlappedPresenter CreateForToolWindow() => new OverlappedPresenter()
	{
		HasBorder = true,
		HasTitleBar = true,
		IsAlwaysOnTop = false,
		IsMaximizable = true,
		IsMinimizable = true,
		IsModal = false,
		IsResizable = true,
	};
}

#if __SKIA__
namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the ContextMenuOpening event.
/// </summary>
/// <remarks>
/// Ported from CContextMenuEventArgs (ContextMenuEventArgs.h) and
/// TextControlHelper::OnContextMenuOpeningHandler (TextControlHelper.h).
/// </remarks>
public partial class ContextMenuEventArgs
{
	// WinUI: CContextMenuEventArgs has m_cursorLeft, m_cursorTop, m_handled fields
	private double _cursorLeft;
	private double _cursorTop;
	private bool _handled;

	internal ContextMenuEventArgs(double cursorLeft, double cursorTop)
	{
		_cursorLeft = cursorLeft;
		_cursorTop = cursorTop;
	}

	// WinUI: ContextMenuEventArgs.g.h — get_Handled / put_Handled
	public bool Handled
	{
		get => _handled;
		set => _handled = value;
	}

	// WinUI: ContextMenuEventArgs.h — get_CursorLeft (public get-only in IDL)
	public double CursorLeft => _cursorLeft;

	// WinUI: ContextMenuEventArgs.h — get_CursorTop (public get-only in IDL)
	public double CursorTop => _cursorTop;
}
#endif

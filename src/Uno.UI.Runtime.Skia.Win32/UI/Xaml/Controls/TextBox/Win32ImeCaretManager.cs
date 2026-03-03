using System.Drawing;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.UI.Input.Ime;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Manages the Win32 system caret and IMM32 composition/candidate window positioning
/// so that the OS knows where the text caret is located. This enables the Windows emoji
/// picker (Win+.) and clipboard history (Win+V) to appear near the focused text input,
/// and positions CJK IME candidate windows correctly.
/// </summary>
internal sealed class Win32ImeCaretManager
{
	// COMPOSITIONFORM.dwStyle values
	private const uint CFS_POINT = 0x0002;
	// CANDIDATEFORM.dwStyle values
	private const uint CFS_CANDIDATEPOS = 0x0040;

	private readonly HWND _hwnd;
	private bool _isActive;

	internal Win32ImeCaretManager(HWND hwnd)
	{
		_hwnd = hwnd;
	}

	/// <summary>
	/// Activates the system caret and sets the initial IME position.
	/// Called when a TextBox gains focus.
	/// </summary>
	/// <param name="x">Caret X position in client-area physical pixels.</param>
	/// <param name="y">Caret Y position in client-area physical pixels.</param>
	internal void Activate(int x, int y)
	{
		// Create a 1x1 invisible system caret. The system caret is not rendered
		// (Skia draws the visual caret), but its position is queried by the OS
		// for popup placement (emoji picker, clipboard history).
		PInvoke.CreateCaret(_hwnd, HBITMAP.Null, 1, 1);
		PInvoke.SetCaretPos(x, y);
		_isActive = true;

		UpdateImeWindows(x, y);
	}

	/// <summary>
	/// Updates the system caret and IME window positions.
	/// Called when the caret/selection changes within a focused TextBox.
	/// </summary>
	/// <param name="x">Caret X position in client-area physical pixels.</param>
	/// <param name="y">Caret Y position in client-area physical pixels.</param>
	internal void UpdatePosition(int x, int y)
	{
		if (!_isActive)
		{
			return;
		}

		PInvoke.SetCaretPos(x, y);
		UpdateImeWindows(x, y);
	}

	/// <summary>
	/// Destroys the system caret.
	/// Called when a TextBox loses focus.
	/// </summary>
	internal void Deactivate()
	{
		if (!_isActive)
		{
			return;
		}

		_isActive = false;
		PInvoke.DestroyCaret();
	}

	private unsafe void UpdateImeWindows(int x, int y)
	{
		var himc = PInvoke.ImmGetContext(_hwnd);
		if (himc.IsNull)
		{
			return;
		}

		try
		{
			var point = new Point(x, y);

			var compositionForm = new COMPOSITIONFORM
			{
				dwStyle = CFS_POINT,
				ptCurrentPos = point,
			};
			PInvoke.ImmSetCompositionWindow(himc, &compositionForm);

			var candidateForm = new CANDIDATEFORM
			{
				dwIndex = 0,
				dwStyle = CFS_CANDIDATEPOS,
				ptCurrentPos = point,
			};
			PInvoke.ImmSetCandidateWindow(himc, &candidateForm);
		}
		finally
		{
			PInvoke.ImmReleaseContext(_hwnd, himc);
		}
	}
}

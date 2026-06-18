#nullable enable
using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Win32.Foundation;
using Microsoft.UI.Xaml.Controls;
using static Uno.UI.Runtime.Skia.Win32.TsfConstants;

namespace Uno.UI.Runtime.Skia.Win32;

// Activates TSF for the Win32 window and focuses a text store backed by the active TextBox so
// the OS shows the touch keyboard (SIP). See specs/045-software-keyboard-skia-desktop/tsf-plan.md.
internal sealed class Win32TsfTextInput
{
	[DllImport("user32.dll")]
	private static extern int ClientToScreen(IntPtr hWnd, ref System.Drawing.Point lpPoint);

	[DllImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

	private ITfThreadMgr? _threadMgr;
	private uint _clientId;
	private ITfDocumentMgr? _docMgr;
	private ITfContext? _context;
	private UnoTsfTextStore? _store; // field-rooted so the CCW stays alive while focused

	public void OnFocus(TextBox textBox, HWND hwnd)
	{
		try
		{
			EnsureThreadMgr();
			if (_threadMgr is null)
			{
				return;
			}

			TearDownDocument();

			var doc = new TextBoxTsfDocument(textBox, hwnd);
			_store = new UnoTsfTextStore(doc);

			_threadMgr.CreateDocumentMgr(out _docMgr);
			_docMgr.CreateContext(_clientId, 0, _store, out _context, out _);
			_docMgr.Push(_context);
			_threadMgr.SetFocus(_docMgr); // advertises a focused editable doc -> OS shows the SIP
			_threadMgr.AssociateFocus(hwnd, _docMgr, out _); // tie the doc to the HWND's Win32 focus
			System.Console.WriteLine("[SoftKeyboard][TSF] SetFocus(docMgr) + AssociateFocus called"); // DIAG
		}
		catch (Exception e)
		{
			System.Console.WriteLine($"[SoftKeyboard][TSF] OnFocus EXCEPTION: {e}"); // DIAG
		}
	}

	public void OnBlur()
	{
		try
		{
			_threadMgr?.SetFocus(null); // OS hides the SIP
			System.Console.WriteLine("[SoftKeyboard][TSF] SetFocus(null) called"); // DIAG
		}
		catch (Exception e)
		{
			System.Console.WriteLine($"[SoftKeyboard][TSF] OnBlur EXCEPTION: {e.Message}"); // DIAG
		}
	}

	private void EnsureThreadMgr()
	{
		if (_threadMgr is not null)
		{
			return;
		}

		try
		{
			_threadMgr = (ITfThreadMgr)Activator.CreateInstance(Type.GetTypeFromCLSID(CLSID_TF_ThreadMgr)!)!;
			_threadMgr.Activate(out _clientId);
			System.Console.WriteLine($"[SoftKeyboard][TSF] ThreadMgr activated, clientId={_clientId}"); // DIAG
		}
		catch (Exception e)
		{
			System.Console.WriteLine($"[SoftKeyboard][TSF] ThreadMgr activate EXCEPTION: {e}"); // DIAG
			_threadMgr = null;
		}
	}

	private void TearDownDocument()
	{
		try
		{
			_docMgr?.Pop(TF_POPF_ALL);
		}
		catch
		{
			// best-effort
		}

		_context = null;
		_docMgr = null;
		_store = null;
	}

	// Bridges the TSF text store to the focused Uno TextBox (text/selection/caret geometry).
	private sealed class TextBoxTsfDocument : ITsfDocument
	{
		private readonly TextBox _textBox;
		private readonly HWND _hwnd;

		public TextBoxTsfDocument(TextBox textBox, HWND hwnd)
		{
			_textBox = textBox;
			_hwnd = hwnd;
		}

		public IntPtr Hwnd => _hwnd;

		public string Text => _textBox.Text ?? string.Empty;

		public (int start, int end) GetSelection()
		{
			var start = _textBox.SelectionStart;
			return (start, start + _textBox.SelectionLength);
		}

		public void SetSelection(int start, int end) => _textBox.Select(start, Math.Max(0, end - start));

		public void ReplaceText(int start, int end, string text, int caretInText)
		{
			var current = _textBox.Text ?? string.Empty;
			start = Math.Clamp(start, 0, current.Length);
			end = Math.Clamp(end, start, current.Length);
			_textBox.Text = current[..start] + text + current[end..];
			var caret = start + Math.Clamp(caretInText, 0, text.Length);
			_textBox.Select(caret, 0);
		}

		public bool TryGetRangeScreenRect(int acpStart, int acpEnd, out RECT rect)
		{
			rect = default;

			var textBoxView = _textBox.TextBoxView;
			if (textBoxView?.DisplayBlock?.ParsedText is null || _textBox.XamlRoot is null)
			{
				return false;
			}

			var index = Math.Clamp(acpStart, 0, Math.Max(0, (_textBox.Text ?? string.Empty).Length));
			var caretRect = textBoxView.DisplayBlock.ParsedText.GetRectForIndex(index);
			var transform = textBoxView.DisplayBlock.TransformToVisual(null);
			var topLeft = transform.TransformPoint(new Point(caretRect.Left, caretRect.Top));
			var scale = _textBox.XamlRoot.RasterizationScale;
			if (scale <= 0)
			{
				scale = 1;
			}

			var height = caretRect.Height > 0 ? caretRect.Height : _textBox.FontSize;
			var pt = new System.Drawing.Point((int)(topLeft.X * scale), (int)(topLeft.Y * scale));
			ClientToScreen(_hwnd, ref pt);

			rect = new RECT
			{
				left = pt.X,
				top = pt.Y,
				right = pt.X + 1,
				bottom = pt.Y + (int)(height * scale),
			};
			return true;
		}

		public bool TryGetFieldScreenRect(out RECT rect)
		{
			rect = default;
			if (!GetClientRect(_hwnd, out var client))
			{
				return false;
			}

			var topLeft = new System.Drawing.Point(client.left, client.top);
			var bottomRight = new System.Drawing.Point(client.right, client.bottom);
			ClientToScreen(_hwnd, ref topLeft);
			ClientToScreen(_hwnd, ref bottomRight);
			rect = new RECT { left = topLeft.X, top = topLeft.Y, right = bottomRight.X, bottom = bottomRight.Y };
			return true;
		}
	}
}

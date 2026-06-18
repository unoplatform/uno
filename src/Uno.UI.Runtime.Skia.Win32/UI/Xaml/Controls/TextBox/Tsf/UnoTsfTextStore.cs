#nullable enable
using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using static Uno.UI.Runtime.Skia.Win32.TsfConstants;

namespace Uno.UI.Runtime.Skia.Win32;

// The editable document the TSF text store mirrors (the focused Uno TextBox).
internal interface ITsfDocument
{
	IntPtr Hwnd { get; }
	string Text { get; }
	(int start, int end) GetSelection();
	void SetSelection(int start, int end);
	// Replace [start,end) with text; place caret at start+caretInText.
	void ReplaceText(int start, int end, string text, int caretInText);
	// Caret/range bounding rect in screen pixels; false => layout not available (TS_E_NOLAYOUT).
	bool TryGetRangeScreenRect(int acpStart, int acpEnd, out RECT rect);
	bool TryGetFieldScreenRect(out RECT rect);
}

// Minimal ITextStoreACP backed by the focused TextBox. Makes the document editable to TSF so
// the OS shows the touch keyboard, and serves the IME's read/write locks. See tsf-plan.md.
internal sealed class UnoTsfTextStore : ITextStoreACP, ITfContextOwnerCompositionSink
{
	private readonly ITsfDocument _doc;
	private ITextStoreACPSink? _sink;
	private uint _sinkMask;
	private uint _lockFlags;          // 0 = none; otherwise TS_LF_READ or TS_LF_READWRITE
	private uint _pendingAsyncLock;

	public UnoTsfTextStore(ITsfDocument doc) => _doc = doc;

	private bool HasReadLock => _lockFlags != 0;
	private bool HasWriteLock => (_lockFlags & TS_LF_READWRITE) == TS_LF_READWRITE;

	// --- Notifications raised by the host when the app changes the document outside a TSF lock ---

	internal void NotifyTextChange(int oldEnd, int newEnd)
	{
		if (_sink is { } sink && !HasReadLock && (_sinkMask & TS_AS_TEXT_CHANGE) != 0)
		{
			var change = new TS_TEXTCHANGE { acpStart = 0, acpOldEnd = oldEnd, acpNewEnd = newEnd };
			sink.OnTextChange(0, in change);
		}
	}

	internal void NotifySelectionChange()
	{
		if (_sink is { } sink && !HasReadLock && (_sinkMask & TS_AS_SEL_CHANGE) != 0)
		{
			sink.OnSelectionChange();
		}
	}

	internal void NotifyLayoutChange()
	{
		if (_sink is { } sink && (_sinkMask & TS_AS_LAYOUT_CHANGE) != 0)
		{
			sink.OnLayoutChange(TsLayoutCode.TS_LC_CHANGE, EditView);
		}
	}

	// --- ITextStoreACP ---

	public int AdviseSink(in Guid riid, object punk, uint dwMask)
	{
		System.Console.WriteLine($"[SoftKeyboard][TSF-store] AdviseSink called, mask=0x{dwMask:X}, sinkIsAcp={punk is ITextStoreACPSink}"); // DIAG
		if (punk is not ITextStoreACPSink sink)
		{
			return E_INVALIDARG;
		}

		if (_sink is not null && !ReferenceEquals(_sink, sink))
		{
			return CONNECT_E_ADVISELIMIT;
		}

		_sink = sink;
		_sinkMask = dwMask;
		return S_OK;
	}

	public int UnadviseSink(object punk)
	{
		if (_sink is null || !ReferenceEquals(_sink, punk))
		{
			return CONNECT_E_NOCONNECTION;
		}

		_sink = null;
		_sinkMask = 0;
		return S_OK;
	}

	public int RequestLock(uint dwLockFlags, out int phrSession)
	{
		System.Console.WriteLine($"[SoftKeyboard][TSF-store] RequestLock called, flags=0x{dwLockFlags:X}"); // DIAG
		phrSession = E_FAIL;
		if (_sink is not { } sink)
		{
			return E_FAIL;
		}

		if (_lockFlags != 0)
		{
			// Only legal reentrancy: request an async write lock while holding a read lock.
			if ((dwLockFlags & TS_LF_READWRITE) == TS_LF_READWRITE && !HasWriteLock)
			{
				_pendingAsyncLock = dwLockFlags;
				phrSession = TS_S_ASYNC;
				return S_OK;
			}

			phrSession = E_FAIL;
			return E_FAIL;
		}

		_lockFlags = dwLockFlags & TS_LF_READWRITE;
		phrSession = sink.OnLockGranted(dwLockFlags);
		_lockFlags = 0;

		while (_pendingAsyncLock != 0)
		{
			var pending = _pendingAsyncLock;
			_pendingAsyncLock = 0;
			_lockFlags = pending & TS_LF_READWRITE;
			sink.OnLockGranted(pending);
			_lockFlags = 0;
		}

		return S_OK;
	}

	public int GetStatus(out TS_STATUS pdcs)
	{
		pdcs = new TS_STATUS
		{
			dwDynamicFlags = 0,
			dwStaticFlags = TS_SS_TRANSITORY | TS_SS_NOHIDDENTEXT,
		};
		return S_OK;
	}

	public int QueryInsert(int acpTestStart, int acpTestEnd, uint cch, out int pacpResultStart, out int pacpResultEnd)
	{
		var len = _doc.Text.Length;
		pacpResultStart = Math.Clamp(acpTestStart, 0, len);
		pacpResultEnd = Math.Clamp(acpTestEnd, 0, len);
		return S_OK;
	}

	public int GetSelection(uint ulIndex, uint ulCount, IntPtr pSelection, out uint pcFetched)
	{
		pcFetched = 0;
		if (!HasReadLock)
		{
			return TS_E_NOLOCK;
		}

		if (ulCount == 0 || pSelection == IntPtr.Zero)
		{
			return S_OK;
		}

		if (ulIndex != 0 && ulIndex != TS_DEFAULT_SELECTION)
		{
			return TS_E_NOSELECTION;
		}

		var (start, end) = _doc.GetSelection();
		var sel = new TS_SELECTION_ACP
		{
			acpStart = start,
			acpEnd = end,
			style = new TS_SELECTIONSTYLE { ase = TsActiveSelEnd.TS_AE_END, fInterimChar = false },
		};
		Marshal.StructureToPtr(sel, pSelection, false);
		pcFetched = 1;
		return S_OK;
	}

	public int SetSelection(uint ulCount, IntPtr pSelection)
	{
		if (!HasWriteLock)
		{
			return TS_E_NOLOCK;
		}

		if (ulCount == 0 || pSelection == IntPtr.Zero)
		{
			return S_OK;
		}

		var sel = Marshal.PtrToStructure<TS_SELECTION_ACP>(pSelection);
		var len = _doc.Text.Length;
		var start = Math.Clamp(sel.acpStart, 0, len);
		var end = Math.Clamp(sel.acpEnd, 0, len);
		_doc.SetSelection(start, end);
		return S_OK;
	}

	public int GetText(int acpStart, int acpEnd, IntPtr pchPlain, uint cchPlainReq, out uint pcchPlainRet, IntPtr prgRunInfo, uint ulRunInfoReq, out uint pulRunInfoRet, out int pacpNext)
	{
		pcchPlainRet = 0;
		pulRunInfoRet = 0;
		pacpNext = acpStart;

		if (!HasReadLock)
		{
			return TS_E_NOLOCK;
		}

		var text = _doc.Text;
		var len = text.Length;
		var start = Math.Clamp(acpStart, 0, len);
		var end = acpEnd < 0 ? len : Math.Clamp(acpEnd, start, len);

		var available = end - start;
		var toCopy = pchPlain != IntPtr.Zero ? Math.Min(available, (int)cchPlainReq) : 0;
		if (toCopy > 0)
		{
			var slice = text.Substring(start, toCopy);
			Marshal.Copy(slice.ToCharArray(), 0, pchPlain, toCopy);
		}

		pcchPlainRet = (uint)toCopy;
		pacpNext = start + toCopy;

		if (prgRunInfo != IntPtr.Zero && ulRunInfoReq > 0 && toCopy > 0)
		{
			var run = new TS_RUNINFO { uCount = (uint)toCopy, type = TsRunType.TS_RT_PLAIN };
			Marshal.StructureToPtr(run, prgRunInfo, false);
			pulRunInfoRet = 1;
		}

		return S_OK;
	}

	public int SetText(uint dwFlags, int acpStart, int acpEnd, IntPtr pchText, uint cch, out TS_TEXTCHANGE pChange)
	{
		pChange = default;
		if (!HasWriteLock)
		{
			return TS_E_NOLOCK;
		}

		var len = _doc.Text.Length;
		var start = Math.Clamp(acpStart, 0, len);
		var end = Math.Clamp(acpEnd, start, len);
		var text = cch > 0 && pchText != IntPtr.Zero ? Marshal.PtrToStringUni(pchText, (int)cch)! : string.Empty;

		_doc.ReplaceText(start, end, text, text.Length);

		pChange = new TS_TEXTCHANGE { acpStart = start, acpOldEnd = end, acpNewEnd = start + text.Length };
		return S_OK;
	}

	public int InsertTextAtSelection(uint dwFlags, IntPtr pchText, uint cch, out int pacpStart, out int pacpEnd, out TS_TEXTCHANGE pChange)
	{
		pChange = default;
		var (selStart, selEnd) = _doc.GetSelection();
		var text = cch > 0 && pchText != IntPtr.Zero ? Marshal.PtrToStringUni(pchText, (int)cch)! : string.Empty;

		if ((dwFlags & TS_IAS_QUERYONLY) != 0)
		{
			pacpStart = selStart;
			pacpEnd = selStart + text.Length;
			return S_OK;
		}

		if (!HasWriteLock)
		{
			pacpStart = pacpEnd = 0;
			return TS_E_NOLOCK;
		}

		_doc.ReplaceText(selStart, selEnd, text, text.Length);
		pacpStart = selStart;
		pacpEnd = selStart + text.Length;
		pChange = new TS_TEXTCHANGE { acpStart = selStart, acpOldEnd = selEnd, acpNewEnd = selStart + text.Length };
		return S_OK;
	}

	public int GetEndACP(out int pacp)
	{
		pacp = 0;
		if (!HasReadLock)
		{
			return TS_E_NOLOCK;
		}

		pacp = _doc.Text.Length;
		return S_OK;
	}

	public int GetActiveView(out uint pvcView)
	{
		pvcView = EditView;
		return S_OK;
	}

	public int GetTextExt(uint vcView, int acpStart, int acpEnd, out RECT prc, out bool pfClipped)
	{
		prc = default;
		pfClipped = false;
		if (!HasReadLock)
		{
			return TS_E_NOLOCK;
		}

		return _doc.TryGetRangeScreenRect(acpStart, acpEnd, out prc) ? S_OK : TS_E_NOLAYOUT;
	}

	public int GetScreenExt(uint vcView, out RECT prc)
	{
		prc = default;
		return _doc.TryGetFieldScreenRect(out prc) ? S_OK : TS_E_NOLAYOUT;
	}

	public int GetWnd(uint vcView, out IntPtr phwnd)
	{
		phwnd = _doc.Hwnd;
		return S_OK;
	}

	// --- No-op (S_OK) attribute methods: returning E_NOTIMPL here can abort some IMEs ---

	public int RequestSupportedAttrs(uint dwFlags, uint cFilterAttrs, IntPtr paFilterAttrs) => S_OK;

	public int RequestAttrsAtPosition(int acpPos, uint cFilterAttrs, IntPtr paFilterAttrs, uint dwFlags) => S_OK;

	public int RequestAttrsTransitioningAtPosition(int acpPos, uint cFilterAttrs, IntPtr paFilterAttrs, uint dwFlags) => S_OK;

	public int FindNextAttrTransition(int acpStart, int acpHalt, uint cFilterAttrs, IntPtr paFilterAttrs, uint dwFlags, out int pacpNext, out bool pfFound, out int plFoundOffset)
	{
		pacpNext = acpHalt;
		pfFound = false;
		plFoundOffset = 0;
		return S_OK;
	}

	public int RetrieveRequestedAttrs(uint ulCount, IntPtr paAttrVals, out uint pcFetched)
	{
		pcFetched = 0;
		return S_OK;
	}

	// --- Unsupported (embedded objects / hit-testing / formatted text) ---

	public int GetFormattedText(int acpStart, int acpEnd, out IntPtr ppDataObject)
	{
		ppDataObject = IntPtr.Zero;
		return E_NOTIMPL;
	}

	public int GetEmbedded(int acpPos, in Guid rguidService, in Guid riid, out IntPtr ppunk)
	{
		ppunk = IntPtr.Zero;
		return E_NOTIMPL;
	}

	public int QueryInsertEmbedded(in Guid pguidService, IntPtr pFormatEtc, out bool pfInsertable)
	{
		pfInsertable = false;
		return S_OK;
	}

	public int InsertEmbedded(uint dwFlags, int acpStart, int acpEnd, IntPtr pDataObject, out TS_TEXTCHANGE pChange)
	{
		pChange = default;
		return E_NOTIMPL;
	}

	public int InsertEmbeddedAtSelection(uint dwFlags, IntPtr pDataObject, out int pacpStart, out int pacpEnd, out TS_TEXTCHANGE pChange)
	{
		pacpStart = pacpEnd = 0;
		pChange = default;
		return E_NOTIMPL;
	}

	public int GetACPFromPoint(uint vcView, in System.Drawing.Point ptScreen, uint dwFlags, out int pacp)
	{
		pacp = 0;
		return E_NOTIMPL;
	}

	// --- ITfContextOwnerCompositionSink (composition refined in Phase D) ---

	public int OnStartComposition(IntPtr pComposition, out bool pfOk)
	{
		pfOk = true;
		return S_OK;
	}

	public int OnUpdateComposition(IntPtr pComposition, IntPtr pRangeNew) => S_OK;

	public int OnEndComposition(IntPtr pComposition) => S_OK;
}

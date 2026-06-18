#nullable enable
using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;

namespace Uno.UI.Runtime.Skia.Win32;

// Text Services Framework (TSF / msctf) interop for driving the Windows touch keyboard (SIP)
// from the custom-rendered Skia TextBox. All interfaces are classic IUnknown COM (not WinRT),
// declared in exact vtable order. See specs/045-software-keyboard-skia-desktop/tsf-plan.md.
internal static class TsfConstants
{
	internal static readonly Guid CLSID_TF_ThreadMgr = new("529A9E6B-6587-4F23-AB9E-9C7D683E3C50");
	internal static readonly Guid IID_ITextStoreACPSink = new("22D44C94-A419-4542-A272-AE26093ECECF");
	internal static readonly Guid IID_ITfContextOwnerCompositionSink = new("5F084B5E-1C6A-11D3-B6FB-00C04FC9DAA7");

	// Lock flags
	internal const uint TS_LF_SYNC = 0x1;
	internal const uint TS_LF_READ = 0x2;
	internal const uint TS_LF_READWRITE = 0x6;

	// Static status flags
	internal const uint TS_SS_TRANSITORY = 0x4;
	internal const uint TS_SS_NOHIDDENTEXT = 0x8;

	// AdviseSink mask
	internal const uint TS_AS_TEXT_CHANGE = 0x1;
	internal const uint TS_AS_SEL_CHANGE = 0x2;
	internal const uint TS_AS_LAYOUT_CHANGE = 0x4;
	internal const uint TS_AS_ATTR_CHANGE = 0x8;
	internal const uint TS_AS_STATUS_CHANGE = 0x10;
	internal const uint TS_AS_ALL_SINKS = 0x1F;

	// InsertTextAtSelection / SetText flags
	internal const uint TS_IAS_QUERYONLY = 0x1;

	// Selection
	internal const uint TS_DEFAULT_SELECTION = 0xFFFFFFFF;

	// DocumentMgr Pop
	internal const uint TF_POPF_ALL = 0x1;

	// HRESULTs
	internal const int S_OK = 0;
	internal const int E_FAIL = unchecked((int)0x80004005);
	internal const int E_NOTIMPL = unchecked((int)0x80004001);
	internal const int E_INVALIDARG = unchecked((int)0x80070057);
	internal const int TS_S_ASYNC = 0x00040300;
	internal const int TS_E_NOLOCK = unchecked((int)0x80040201);
	internal const int TS_E_NOSELECTION = unchecked((int)0x80040205);
	internal const int TS_E_NOLAYOUT = unchecked((int)0x80040206);
	internal const int TS_E_INVALIDPOS = unchecked((int)0x80040207);
	internal const int CONNECT_E_ADVISELIMIT = unchecked((int)0x80040201);
	internal const int CONNECT_E_NOCONNECTION = unchecked((int)0x80040200);

	// View cookie
	internal const uint EditView = 1;
}

internal enum TsActiveSelEnd
{
	TS_AE_NONE = 0,
	TS_AE_START = 1,
	TS_AE_END = 2,
}

internal enum TsRunType
{
	TS_RT_PLAIN = 0,
	TS_RT_HIDDEN = 1,
	TS_RT_OPAQUE = 2,
}

internal enum TsLayoutCode
{
	TS_LC_CREATE = 0,
	TS_LC_CHANGE = 1,
	TS_LC_DESTROY = 2,
}

[StructLayout(LayoutKind.Sequential)]
internal struct TS_SELECTIONSTYLE
{
	public TsActiveSelEnd ase;
	[MarshalAs(UnmanagedType.U1)] public bool fInterimChar;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TS_SELECTION_ACP
{
	public int acpStart;
	public int acpEnd;
	public TS_SELECTIONSTYLE style;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TS_TEXTCHANGE
{
	public int acpStart;
	public int acpOldEnd;
	public int acpNewEnd;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TS_STATUS
{
	public uint dwDynamicFlags;
	public uint dwStaticFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TS_RUNINFO
{
	public uint uCount;
	public TsRunType type;
}

[StructLayout(LayoutKind.Sequential)]
internal struct TS_ATTRVAL
{
	public Guid idAttr;
	public uint dwOverlapId;
	// VARIANT — opaque here; we never return attrs, so layout padding is sufficient for marshalling arrays we don't fill.
	public IntPtr varValue0;
	public IntPtr varValue1;
}

[ComImport]
[Guid("AA80E7FD-2021-11D2-93E0-0060B067B86E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITfContext
{
	// Opaque for a minimal SIP store — no methods are called.
}

[ComImport]
[Guid("AA80E7F4-2021-11D2-93E0-0060B067B86E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITfDocumentMgr
{
	void CreateContext(uint tidClientId, uint dwFlags, [MarshalAs(UnmanagedType.IUnknown)] object punk, out ITfContext ppic, out uint pecTextStore);
	void Push(ITfContext pic);
	void Pop(uint dwFlags);
	void GetTop(out ITfContext ppic);
	void GetBase(out ITfContext ppic);
	void EnumContexts(out IntPtr ppEnum);
}

[ComImport]
[Guid("AA80E801-2021-11D2-93E0-0060B067B86E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITfThreadMgr
{
	void Activate(out uint ptid);
	void Deactivate();
	void CreateDocumentMgr(out ITfDocumentMgr ppdim);
	void EnumDocumentMgrs(out IntPtr ppEnum);
	void GetFocus(out ITfDocumentMgr ppdimFocus);
	void SetFocus(ITfDocumentMgr? pdimFocus);
	void AssociateFocus(IntPtr hwnd, ITfDocumentMgr? pdimNew, out ITfDocumentMgr ppdimPrev);
	[PreserveSig] int IsThreadFocus([MarshalAs(UnmanagedType.U1)] out bool pfThreadFocus);
	void GetFunctionProvider(in Guid rclsid, out IntPtr ppFuncProv);
	void EnumFunctionProviders(out IntPtr ppEnum);
	void GetGlobalCompartment(out IntPtr ppCompMgr);
}

// TSF-provided sink that the text store calls to notify of app-side changes.
[ComImport]
[Guid("22D44C94-A419-4542-A272-AE26093ECECF")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITextStoreACPSink
{
	void OnTextChange(uint dwFlags, in TS_TEXTCHANGE pChange);
	void OnSelectionChange();
	void OnLayoutChange(TsLayoutCode lcode, uint vcView);
	void OnStatusChange(uint dwFlags);
	void OnAttrsChange(int acpStart, int acpEnd, uint cAttrs, [In] Guid[] paAttrs);
	[PreserveSig] int OnLockGranted(uint dwLockFlags);
	void OnStartEditTransaction();
	void OnEndEditTransaction();
}

// App-implemented text store. Methods use [PreserveSig] so the implementation returns
// HRESULTs directly (TS_E_*, S_OK, E_NOTIMPL) rather than throwing across the COM boundary.
[ComImport]
[Guid("28888FE3-C2A0-483A-A3EA-8CB1CE51FF3D")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITextStoreACP
{
	// Buffer params (pSelection/pchPlain/prgRunInfo/pchText/attr arrays) are raw pointers
	// marshalled manually in the store using the explicit count params — managed-array
	// marshalling of caller-allocated COM buffers is unreliable without size hints.
	[PreserveSig] int AdviseSink(in Guid riid, [MarshalAs(UnmanagedType.IUnknown)] object punk, uint dwMask);
	[PreserveSig] int UnadviseSink([MarshalAs(UnmanagedType.IUnknown)] object punk);
	[PreserveSig] int RequestLock(uint dwLockFlags, out int phrSession);
	[PreserveSig] int GetStatus(out TS_STATUS pdcs);
	[PreserveSig] int QueryInsert(int acpTestStart, int acpTestEnd, uint cch, out int pacpResultStart, out int pacpResultEnd);
	[PreserveSig] int GetSelection(uint ulIndex, uint ulCount, IntPtr pSelection, out uint pcFetched);
	[PreserveSig] int SetSelection(uint ulCount, IntPtr pSelection);
	[PreserveSig] int GetText(int acpStart, int acpEnd, IntPtr pchPlain, uint cchPlainReq, out uint pcchPlainRet, IntPtr prgRunInfo, uint ulRunInfoReq, out uint pulRunInfoRet, out int pacpNext);
	[PreserveSig] int SetText(uint dwFlags, int acpStart, int acpEnd, IntPtr pchText, uint cch, out TS_TEXTCHANGE pChange);
	[PreserveSig] int GetFormattedText(int acpStart, int acpEnd, out IntPtr ppDataObject);
	[PreserveSig] int GetEmbedded(int acpPos, in Guid rguidService, in Guid riid, out IntPtr ppunk);
	[PreserveSig] int QueryInsertEmbedded(in Guid pguidService, IntPtr pFormatEtc, [MarshalAs(UnmanagedType.U1)] out bool pfInsertable);
	[PreserveSig] int InsertEmbedded(uint dwFlags, int acpStart, int acpEnd, IntPtr pDataObject, out TS_TEXTCHANGE pChange);
	[PreserveSig] int InsertTextAtSelection(uint dwFlags, IntPtr pchText, uint cch, out int pacpStart, out int pacpEnd, out TS_TEXTCHANGE pChange);
	[PreserveSig] int InsertEmbeddedAtSelection(uint dwFlags, IntPtr pDataObject, out int pacpStart, out int pacpEnd, out TS_TEXTCHANGE pChange);
	[PreserveSig] int RequestSupportedAttrs(uint dwFlags, uint cFilterAttrs, IntPtr paFilterAttrs);
	[PreserveSig] int RequestAttrsAtPosition(int acpPos, uint cFilterAttrs, IntPtr paFilterAttrs, uint dwFlags);
	[PreserveSig] int RequestAttrsTransitioningAtPosition(int acpPos, uint cFilterAttrs, IntPtr paFilterAttrs, uint dwFlags);
	[PreserveSig] int FindNextAttrTransition(int acpStart, int acpHalt, uint cFilterAttrs, IntPtr paFilterAttrs, uint dwFlags, out int pacpNext, [MarshalAs(UnmanagedType.U1)] out bool pfFound, out int plFoundOffset);
	[PreserveSig] int RetrieveRequestedAttrs(uint ulCount, IntPtr paAttrVals, out uint pcFetched);
	[PreserveSig] int GetEndACP(out int pacp);
	[PreserveSig] int GetActiveView(out uint pvcView);
	[PreserveSig] int GetACPFromPoint(uint vcView, in System.Drawing.Point ptScreen, uint dwFlags, out int pacp);
	[PreserveSig] int GetTextExt(uint vcView, int acpStart, int acpEnd, out RECT prc, [MarshalAs(UnmanagedType.U1)] out bool pfClipped);
	[PreserveSig] int GetScreenExt(uint vcView, out RECT prc);
	[PreserveSig] int GetWnd(uint vcView, out IntPtr phwnd);
}

// Optional app-implemented composition sink.
[ComImport]
[Guid("5F084B5E-1C6A-11D3-B6FB-00C04FC9DAA7")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ITfContextOwnerCompositionSink
{
	[PreserveSig] int OnStartComposition(IntPtr pComposition, [MarshalAs(UnmanagedType.U1)] out bool pfOk);
	[PreserveSig] int OnUpdateComposition(IntPtr pComposition, IntPtr pRangeNew);
	[PreserveSig] int OnEndComposition(IntPtr pComposition);
}

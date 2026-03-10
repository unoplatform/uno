using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Uno.UI.Runtime.Skia.Win32;

// UIA COM Pattern Interfaces
// Method ordering must match the COM vtable layout exactly (from UIAutomationCore.idl).

[ComImport, Guid("54fcb24b-e18e-47a2-b4d3-eccbe77599a2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaInvokeProvider
{
	void Invoke();
}

[ComImport, Guid("56d00bd0-c4f4-433c-a836-1a52a57e0892"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaToggleProvider
{
	void Toggle();
	ToggleState ToggleState { get; }
}

[ComImport, Guid("c7935180-6fb3-4201-b174-7df73adbf64a"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaValueProvider
{
	void SetValue([MarshalAs(UnmanagedType.LPWStr)] string val);
	string Value { get; }
	bool IsReadOnly { get; }
}

[ComImport, Guid("36dc7aef-33e6-4691-afe1-2be7274b3d33"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaRangeValueProvider
{
	void SetValue(double val);
	double Value { get; }
	bool IsReadOnly { get; }
	double Maximum { get; }
	double Minimum { get; }
	double LargeChange { get; }
	double SmallChange { get; }
}

[ComImport, Guid("d847d3a5-cab0-4a98-8c32-ecb45c59ad24"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaExpandCollapseProvider
{
	void Expand();
	void Collapse();
	ExpandCollapseState ExpandCollapseState { get; }
}

[ComImport, Guid("fb8b03af-3bdf-48d4-bd36-1a65793be168"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaSelectionProvider
{
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
	object[]? GetSelection();
	bool CanSelectMultiple { get; }
	bool IsSelectionRequired { get; }
}

[ComImport, Guid("2acad808-b2d4-452d-a407-91ff1ad167b2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaSelectionItemProvider
{
	void Select();
	void AddToSelection();
	void RemoveFromSelection();
	bool IsSelected { get; }
	IRawElementProviderSimple? SelectionContainer { get; }
}

[ComImport, Guid("b38b8077-1fc3-42a5-8cae-d40c2215055a"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaScrollProvider
{
	void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount);
	void SetScrollPercent(double horizontalPercent, double verticalPercent);
	double HorizontalScrollPercent { get; }
	double VerticalScrollPercent { get; }
	double HorizontalViewSize { get; }
	double VerticalViewSize { get; }
	bool HorizontallyScrollable { get; }
	bool VerticallyScrollable { get; }
}

[ComImport, Guid("b17d6187-0907-464b-a168-0ef17a1572b1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaGridProvider
{
	IRawElementProviderSimple? GetItem(int row, int column);
	int RowCount { get; }
	int ColumnCount { get; }
}

[ComImport, Guid("9c860395-97b3-490a-b52a-858cc22af166"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaTableProvider
{
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
	object[]? GetRowHeaders();
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
	object[]? GetColumnHeaders();
	RowOrColumnMajor RowOrColumnMajor { get; }
}

// Wrapper classes that bridge Uno AutomationPeer patterns to UIA COM pattern interfaces.
// Each wrapper is [ComVisible(true)] so the CLR creates a COM Callable Wrapper (CCW)
// that UIA can consume directly.

[ComVisible(true)]
internal sealed class UiaInvokeProviderWrapper : IUiaInvokeProvider
{
	private readonly IInvokeProvider _inner;
	internal UiaInvokeProviderWrapper(IInvokeProvider inner) => _inner = inner;
	public void Invoke() => _inner.Invoke();
}

[ComVisible(true)]
internal sealed class UiaToggleProviderWrapper : IUiaToggleProvider
{
	private readonly IToggleProvider _inner;
	internal UiaToggleProviderWrapper(IToggleProvider inner) => _inner = inner;
	public void Toggle() => _inner.Toggle();
	public ToggleState ToggleState => _inner.ToggleState;
}

[ComVisible(true)]
internal sealed class UiaValueProviderWrapper : IUiaValueProvider
{
	private readonly IValueProvider _inner;
	internal UiaValueProviderWrapper(IValueProvider inner) => _inner = inner;
	public void SetValue(string val) => _inner.SetValue(val);
	public string Value => _inner.Value ?? string.Empty;
	public bool IsReadOnly => _inner.IsReadOnly;
}

[ComVisible(true)]
internal sealed class UiaRangeValueProviderWrapper : IUiaRangeValueProvider
{
	private readonly IRangeValueProvider _inner;
	internal UiaRangeValueProviderWrapper(IRangeValueProvider inner) => _inner = inner;
	public void SetValue(double val) => _inner.SetValue(val);
	public double Value => _inner.Value;
	public bool IsReadOnly => _inner.IsReadOnly;
	public double Maximum => _inner.Maximum;
	public double Minimum => _inner.Minimum;
	public double LargeChange => _inner.LargeChange;
	public double SmallChange => _inner.SmallChange;
}

[ComVisible(true)]
internal sealed class UiaExpandCollapseProviderWrapper : IUiaExpandCollapseProvider
{
	private readonly IExpandCollapseProvider _inner;
	internal UiaExpandCollapseProviderWrapper(IExpandCollapseProvider inner) => _inner = inner;
	public void Expand() => _inner.Expand();
	public void Collapse() => _inner.Collapse();
	public ExpandCollapseState ExpandCollapseState => _inner.ExpandCollapseState;
}

[ComVisible(true)]
internal sealed class UiaSelectionProviderWrapper : IUiaSelectionProvider
{
	private readonly Microsoft.UI.Xaml.Automation.Provider.ISelectionProvider _inner;
	internal UiaSelectionProviderWrapper(Microsoft.UI.Xaml.Automation.Provider.ISelectionProvider inner) => _inner = inner;
	public object[]? GetSelection() => null; // Requires peer→provider resolution; deferred to Phase 4
	public bool CanSelectMultiple => _inner.CanSelectMultiple;
	public bool IsSelectionRequired => _inner.IsSelectionRequired;
}

[ComVisible(true)]
internal sealed class UiaSelectionItemProviderWrapper : IUiaSelectionItemProvider
{
	private readonly ISelectionItemProvider _inner;
	internal UiaSelectionItemProviderWrapper(ISelectionItemProvider inner) => _inner = inner;
	public void Select() => _inner.Select();
	public void AddToSelection() => _inner.AddToSelection();
	public void RemoveFromSelection() => _inner.RemoveFromSelection();
	public bool IsSelected => _inner.IsSelected;
	public IRawElementProviderSimple? SelectionContainer => null; // Requires peer→provider resolution; deferred to Phase 4
}

[ComVisible(true)]
internal sealed class UiaScrollProviderWrapper : IUiaScrollProvider
{
	private readonly IScrollProvider _inner;
	internal UiaScrollProviderWrapper(IScrollProvider inner) => _inner = inner;
	public void Scroll(ScrollAmount horizontalAmount, ScrollAmount verticalAmount) => _inner.Scroll(horizontalAmount, verticalAmount);
	public void SetScrollPercent(double horizontalPercent, double verticalPercent) => _inner.SetScrollPercent(horizontalPercent, verticalPercent);
	public double HorizontalScrollPercent => _inner.HorizontalScrollPercent;
	public double VerticalScrollPercent => _inner.VerticalScrollPercent;
	public double HorizontalViewSize => _inner.HorizontalViewSize;
	public double VerticalViewSize => _inner.VerticalViewSize;
	public bool HorizontallyScrollable => _inner.HorizontallyScrollable;
	public bool VerticallyScrollable => _inner.VerticallyScrollable;
}

[ComVisible(true)]
internal sealed class UiaGridProviderWrapper : IUiaGridProvider
{
	private readonly IGridProvider _inner;
	internal UiaGridProviderWrapper(IGridProvider inner) => _inner = inner;
	public IRawElementProviderSimple? GetItem(int row, int column) => null; // Requires peer→provider resolution; deferred to Phase 4
	public int RowCount => _inner.RowCount;
	public int ColumnCount => _inner.ColumnCount;
}

[ComVisible(true)]
internal sealed class UiaTableProviderWrapper : IUiaTableProvider
{
	private readonly ITableProvider _inner;
	internal UiaTableProviderWrapper(ITableProvider inner) => _inner = inner;
	public object[]? GetRowHeaders() => null; // Requires peer→provider resolution; deferred to Phase 4
	public object[]? GetColumnHeaders() => null; // Requires peer→provider resolution; deferred to Phase 4
	public RowOrColumnMajor RowOrColumnMajor => _inner.RowOrColumnMajor;
}

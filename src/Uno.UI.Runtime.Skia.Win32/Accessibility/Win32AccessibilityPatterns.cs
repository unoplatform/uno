using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;

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

[ComImport, Guid("d02541f1-fb81-4d64-ae32-f520f8a6dbd1"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaGridItemProvider
{
	int Row { get; }
	int Column { get; }
	int RowSpan { get; }
	int ColumnSpan { get; }
	IRawElementProviderSimple? ContainingGrid { get; }
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

[ComImport, Guid("2360c714-4bf1-4b26-ba65-9b21316127eb"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaScrollItemProvider
{
	void ScrollIntoView();
}

[ComImport, Guid("987df77b-db06-4d77-8f8a-86a9c3bb90b9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaWindowProvider
{
	void SetVisualState(WindowVisualState state);
	void Close();
	[return: MarshalAs(UnmanagedType.Bool)]
	bool WaitForInputIdle(int milliseconds);
	bool Maximizable { get; }
	bool Minimizable { get; }
	bool IsModal { get; }
	bool IsTopmost { get; }
	WindowVisualState VisualState { get; }
	WindowInteractionState InteractionState { get; }
}

[ComImport, Guid("6829ddc4-4f91-4ffa-b86f-bd3e2987cb4c"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaTransformProvider
{
	void Move(double x, double y);
	void Resize(double width, double height);
	void Rotate(double degrees);
	bool CanMove { get; }
	bool CanResize { get; }
	bool CanRotate { get; }
}

[ComImport, Guid("159bc72c-4ad3-485e-9637-d7052edf0146"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaDockProvider
{
	void SetDockPosition(DockPosition dockPosition);
	DockPosition DockPosition { get; }
}

[ComImport, Guid("6278cab1-b556-4a1a-b4e0-418acc523201"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaMultipleViewProvider
{
	[return: MarshalAs(UnmanagedType.LPWStr)]
	string GetViewName(int viewId);
	void SetCurrentView(int viewId);
	int CurrentView { get; }
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)]
	int[] GetSupportedViews();
}

[ComImport, Guid("3589c92c-63f3-4367-99bb-ada653b77cf2"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaTextProvider
{
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
	object[]? GetSelection();
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
	object[]? GetVisibleRanges();
	IUiaTextRangeProvider? RangeFromChild(IRawElementProviderSimple childElement);
	IUiaTextRangeProvider? RangeFromPoint(UiaPoint point);
	IUiaTextRangeProvider? DocumentRange { get; }
	SupportedTextSelection SupportedTextSelection { get; }
}

[ComImport, Guid("5347ad7b-c355-46f8-aff5-909033582f63"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaTextRangeProvider
{
	IUiaTextRangeProvider Clone();
	[return: MarshalAs(UnmanagedType.Bool)]
	bool Compare(IUiaTextRangeProvider range);
	int CompareEndpoints(TextPatternRangeEndpoint endpoint, IUiaTextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint);
	void ExpandToEnclosingUnit(TextUnit unit);
	IUiaTextRangeProvider? FindAttribute(int attributeId, object value, [MarshalAs(UnmanagedType.Bool)] bool backward);
	IUiaTextRangeProvider? FindText([MarshalAs(UnmanagedType.BStr)] string text, [MarshalAs(UnmanagedType.Bool)] bool backward, [MarshalAs(UnmanagedType.Bool)] bool ignoreCase);
	object? GetAttributeValue(int attributeId);
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_R8)]
	double[] GetBoundingRectangles();
	IRawElementProviderSimple? GetEnclosingElement();
	string GetText(int maxLength);
	int Move(TextUnit unit, int count);
	int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count);
	void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, IUiaTextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint);
	void Select();
	void AddToSelection();
	void RemoveFromSelection();
	void ScrollIntoView([MarshalAs(UnmanagedType.Bool)] bool alignToTop);
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
	object[]? GetChildren();
}

[StructLayout(LayoutKind.Sequential)]
internal struct UiaPoint
{
	public double X;
	public double Y;
}

[ComImport, Guid("e747770b-39ce-4382-ab30-d8fb3f336f24"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaItemContainerProvider
{
	IRawElementProviderSimple? FindItemByProperty(IRawElementProviderSimple? startAfter, int propertyId, object value);
}

[ComImport, Guid("cb98b665-2d35-4fac-ad35-f3c60d0c0b8b"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaVirtualizedItemProvider
{
	void Realize();
}

[ComImport, Guid("0dc5e6ed-3e16-4bf1-8f9a-a979878bc195"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaTextProvider2 // : IUiaTextProvider — flattened for COM vtable
{
	// IUiaTextProvider members (must come first, vtable order)
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
	object[]? GetSelection();
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
	object[]? GetVisibleRanges();
	IUiaTextRangeProvider? RangeFromChild(IRawElementProviderSimple childElement);
	IUiaTextRangeProvider? RangeFromPoint(UiaPoint point);
	IUiaTextRangeProvider? DocumentRange { get; }
	SupportedTextSelection SupportedTextSelection { get; }

	// IUiaTextProvider2-specific members
	IUiaTextRangeProvider? RangeFromAnnotation(IRawElementProviderSimple annotationElement);
	IUiaTextRangeProvider? GetCaretRange([MarshalAs(UnmanagedType.Bool)] out bool isActive);
}

[ComImport, Guid("ea3605b4-3a05-400e-b5f9-4e91b40f6176"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaTextEditProvider // : IUiaTextProvider — flattened for COM vtable
{
	// IUiaTextProvider members
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
	object[]? GetSelection();
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
	object[]? GetVisibleRanges();
	IUiaTextRangeProvider? RangeFromChild(IRawElementProviderSimple childElement);
	IUiaTextRangeProvider? RangeFromPoint(UiaPoint point);
	IUiaTextRangeProvider? DocumentRange { get; }
	SupportedTextSelection SupportedTextSelection { get; }

	// IUiaTextEditProvider-specific members
	IUiaTextRangeProvider? GetActiveComposition();
	IUiaTextRangeProvider? GetConversionTarget();
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
	private readonly Win32Accessibility _accessibility;

	internal UiaSelectionProviderWrapper(
		Microsoft.UI.Xaml.Automation.Provider.ISelectionProvider inner,
		Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public object[]? GetSelection()
	{
		var selected = _inner.GetSelection();
		if (selected is null || selected.Length == 0)
		{
			return null;
		}

		var result = new object[selected.Length];
		var count = 0;
		foreach (var rep in selected)
		{
			// In Uno, IRawElementProviderSimple wraps an AutomationPeer
			if (rep?.AutomationPeer is { } automationPeer)
			{
				var provider = _accessibility.GetProviderForPeer(automationPeer);
				if (provider is not null)
				{
					result[count++] = provider;
				}
			}
		}

		if (count == 0)
		{
			return null;
		}

		if (count < result.Length)
		{
			Array.Resize(ref result, count);
		}

		return result;
	}

	public bool CanSelectMultiple => _inner.CanSelectMultiple;
	public bool IsSelectionRequired => _inner.IsSelectionRequired;
}

[ComVisible(true)]
internal sealed class UiaSelectionItemProviderWrapper : IUiaSelectionItemProvider
{
	private readonly ISelectionItemProvider _inner;
	private readonly Win32Accessibility _accessibility;

	internal UiaSelectionItemProviderWrapper(
		ISelectionItemProvider inner,
		Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public void Select() => _inner.Select();
	public void AddToSelection() => _inner.AddToSelection();
	public void RemoveFromSelection() => _inner.RemoveFromSelection();
	public bool IsSelected => _inner.IsSelected;

	public IRawElementProviderSimple? SelectionContainer
	{
		get
		{
			var container = _inner.SelectionContainer;
			if (container?.AutomationPeer is { } peer)
			{
				return _accessibility.GetProviderForPeer(peer);
			}
			return null;
		}
	}
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
	private readonly Win32Accessibility _accessibility;

	internal UiaGridProviderWrapper(IGridProvider inner, Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public IRawElementProviderSimple? GetItem(int row, int column)
	{
		var item = _inner.GetItem(row, column);
		if (item?.AutomationPeer is { } peer)
		{
			return _accessibility.GetProviderForPeer(peer);
		}
		return null;
	}

	public int RowCount => _inner.RowCount;
	public int ColumnCount => _inner.ColumnCount;
}

[ComVisible(true)]
internal sealed class UiaGridItemProviderWrapper : IUiaGridItemProvider
{
	private readonly IGridItemProvider _inner;
	private readonly Win32Accessibility _accessibility;

	internal UiaGridItemProviderWrapper(IGridItemProvider inner, Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public int Row => _inner.Row;
	public int Column => _inner.Column;
	public int RowSpan => _inner.RowSpan;
	public int ColumnSpan => _inner.ColumnSpan;

	public IRawElementProviderSimple? ContainingGrid
	{
		get
		{
			var grid = _inner.ContainingGrid;
			if (grid?.AutomationPeer is { } peer)
			{
				return _accessibility.GetProviderForPeer(peer);
			}
			return null;
		}
	}
}

[ComVisible(true)]
internal sealed class UiaTableProviderWrapper : IUiaTableProvider
{
	private readonly ITableProvider _inner;
	private readonly Win32Accessibility _accessibility;

	internal UiaTableProviderWrapper(ITableProvider inner, Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public object[]? GetRowHeaders()
	{
		var headers = _inner.GetRowHeaders();
		return ResolveProviders(headers);
	}

	public object[]? GetColumnHeaders()
	{
		var headers = _inner.GetColumnHeaders();
		return ResolveProviders(headers);
	}

	public RowOrColumnMajor RowOrColumnMajor => _inner.RowOrColumnMajor;

	private object[]? ResolveProviders(Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple[]? elements)
	{
		if (elements is null || elements.Length == 0)
		{
			return null;
		}

		var result = new object[elements.Length];
		var count = 0;
		foreach (var rep in elements)
		{
			if (rep?.AutomationPeer is { } peer)
			{
				var provider = _accessibility.GetProviderForPeer(peer);
				if (provider is not null)
				{
					result[count++] = provider;
				}
			}
		}

		if (count == 0)
		{
			return null;
		}

		if (count < result.Length)
		{
			Array.Resize(ref result, count);
		}

		return result;
	}
}

[ComVisible(true)]
internal sealed class UiaScrollItemProviderWrapper : IUiaScrollItemProvider
{
	private readonly IScrollItemProvider _inner;
	internal UiaScrollItemProviderWrapper(IScrollItemProvider inner) => _inner = inner;
	public void ScrollIntoView() => _inner.ScrollIntoView();
}

[ComVisible(true)]
internal sealed class UiaWindowProviderWrapper : IUiaWindowProvider
{
	private readonly IWindowProvider _inner;
	internal UiaWindowProviderWrapper(IWindowProvider inner) => _inner = inner;
	public void SetVisualState(WindowVisualState state) => _inner.SetVisualState(state);
	public void Close() => _inner.Close();
	public bool WaitForInputIdle(int milliseconds) => _inner.WaitForInputIdle(milliseconds);
	public bool Maximizable => _inner.Maximizable;
	public bool Minimizable => _inner.Minimizable;
	public bool IsModal => _inner.IsModal;
	public bool IsTopmost => _inner.IsTopmost;
	public WindowVisualState VisualState => _inner.VisualState;
	public WindowInteractionState InteractionState => _inner.InteractionState;
}

[ComVisible(true)]
internal sealed class UiaTransformProviderWrapper : IUiaTransformProvider
{
	private readonly ITransformProvider _inner;
	internal UiaTransformProviderWrapper(ITransformProvider inner) => _inner = inner;
	public void Move(double x, double y) => _inner.Move(x, y);
	public void Resize(double width, double height) => _inner.Resize(width, height);
	public void Rotate(double degrees) => _inner.Rotate(degrees);
	public bool CanMove => _inner.CanMove;
	public bool CanResize => _inner.CanResize;
	public bool CanRotate => _inner.CanRotate;
}

[ComVisible(true)]
internal sealed class UiaDockProviderWrapper : IUiaDockProvider
{
	private readonly IDockProvider _inner;
	internal UiaDockProviderWrapper(IDockProvider inner) => _inner = inner;
	public void SetDockPosition(DockPosition dockPosition) => _inner.SetDockPosition(dockPosition);
	public DockPosition DockPosition => _inner.DockPosition;
}

[ComVisible(true)]
internal sealed class UiaMultipleViewProviderWrapper : IUiaMultipleViewProvider
{
	private readonly IMultipleViewProvider _inner;
	internal UiaMultipleViewProviderWrapper(IMultipleViewProvider inner) => _inner = inner;
	public string GetViewName(int viewId) => _inner.GetViewName(viewId) ?? string.Empty;
	public void SetCurrentView(int viewId) => _inner.SetCurrentView(viewId);
	public int CurrentView => _inner.CurrentView;
	public int[] GetSupportedViews() => _inner.GetSupportedViews() ?? Array.Empty<int>();
}

[ComVisible(true)]
internal sealed class UiaTextProviderWrapper : IUiaTextProvider
{
	private readonly ITextProvider _inner;
	private readonly Win32Accessibility _accessibility;

	internal UiaTextProviderWrapper(ITextProvider inner, Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public object[]? GetSelection() => WrapRanges(_inner.GetSelection());

	public object[]? GetVisibleRanges() => WrapRanges(_inner.GetVisibleRanges());

	public IUiaTextRangeProvider? RangeFromChild(IRawElementProviderSimple childElement)
	{
		// childElement arrives as a COM provider. Map it to its managed peer-backed
		// counterpart so the managed ITextProvider can interpret it; if the inner
		// can't make sense of it, fall back to DocumentRange.
		var managedChild = ToManagedProvider(childElement);
		var range = managedChild is not null
			? _inner.RangeFromChild(managedChild)
			: _inner.DocumentRange;
		return range is not null ? new UiaTextRangeProviderWrapper(range, _accessibility) : null;
	}

	private static Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple? ToManagedProvider(IRawElementProviderSimple? comProvider)
	{
		if (comProvider is Win32RawElementProvider w32
			&& w32.RepresentedPeer is { } peer)
		{
			return new Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple(peer);
		}

		return null;
	}

	public IUiaTextRangeProvider? RangeFromPoint(UiaPoint point)
	{
		var range = _inner.RangeFromPoint(new Windows.Foundation.Point(point.X, point.Y));
		return range is not null ? new UiaTextRangeProviderWrapper(range, _accessibility) : null;
	}

	public IUiaTextRangeProvider? DocumentRange
	{
		get
		{
			var range = _inner.DocumentRange;
			return range is not null ? new UiaTextRangeProviderWrapper(range, _accessibility) : null;
		}
	}

	public SupportedTextSelection SupportedTextSelection => _inner.SupportedTextSelection;

	private object[]? WrapRanges(ITextRangeProvider[]? ranges)
	{
		if (ranges is null || ranges.Length == 0)
		{
			return null;
		}

		var result = new object[ranges.Length];
		for (var i = 0; i < ranges.Length; i++)
		{
			result[i] = new UiaTextRangeProviderWrapper(ranges[i], _accessibility);
		}
		return result;
	}
}

[ComVisible(true)]
internal sealed class UiaTextRangeProviderWrapper : IUiaTextRangeProvider
{
	private readonly ITextRangeProvider _inner;
	private readonly Win32Accessibility _accessibility;

	internal UiaTextRangeProviderWrapper(ITextRangeProvider inner, Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	internal ITextRangeProvider Inner => _inner;

	public IUiaTextRangeProvider Clone() => new UiaTextRangeProviderWrapper(_inner.Clone(), _accessibility);

	public bool Compare(IUiaTextRangeProvider range)
		=> range is UiaTextRangeProviderWrapper other && _inner.Compare(other._inner);

	public int CompareEndpoints(TextPatternRangeEndpoint endpoint, IUiaTextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
		=> targetRange is UiaTextRangeProviderWrapper other
			? _inner.CompareEndpoints(endpoint, other._inner, targetEndpoint)
			: 0;

	public void ExpandToEnclosingUnit(TextUnit unit) => _inner.ExpandToEnclosingUnit(unit);

	public IUiaTextRangeProvider? FindAttribute(int attributeId, object value, bool backward)
	{
		var range = _inner.FindAttribute(attributeId, value, backward);
		return range is not null ? new UiaTextRangeProviderWrapper(range, _accessibility) : null;
	}

	public IUiaTextRangeProvider? FindText(string text, bool backward, bool ignoreCase)
	{
		var range = _inner.FindText(text, backward, ignoreCase);
		return range is not null ? new UiaTextRangeProviderWrapper(range, _accessibility) : null;
	}

	public object? GetAttributeValue(int attributeId) => _inner.GetAttributeValue(attributeId);

	public double[] GetBoundingRectangles()
	{
		_inner.GetBoundingRectangles(out var rects);
		return rects ?? Array.Empty<double>();
	}

	public IRawElementProviderSimple? GetEnclosingElement()
	{
		// The inner returns a managed IRawElementProviderSimple (peer-backed).
		// Translate to the COM provider so UIA can navigate via QueryInterface.
		var managed = _inner.GetEnclosingElement();
		if (managed?.AutomationPeer is { } peer)
		{
			return _accessibility.GetProviderForPeer(peer);
		}
		return null;
	}

	public string GetText(int maxLength) => _inner.GetText(maxLength) ?? string.Empty;

	public int Move(TextUnit unit, int count) => _inner.Move(unit, count);

	public int MoveEndpointByUnit(TextPatternRangeEndpoint endpoint, TextUnit unit, int count)
		=> _inner.MoveEndpointByUnit(endpoint, unit, count);

	public void MoveEndpointByRange(TextPatternRangeEndpoint endpoint, IUiaTextRangeProvider targetRange, TextPatternRangeEndpoint targetEndpoint)
	{
		if (targetRange is UiaTextRangeProviderWrapper other)
		{
			_inner.MoveEndpointByRange(endpoint, other._inner, targetEndpoint);
		}
	}

	public void Select() => _inner.Select();
	public void AddToSelection() => _inner.AddToSelection();
	public void RemoveFromSelection() => _inner.RemoveFromSelection();
	public void ScrollIntoView(bool alignToTop) => _inner.ScrollIntoView(alignToTop);

	public object[]? GetChildren()
	{
		var children = _inner.GetChildren();
		if (children is null || children.Length == 0)
		{
			return null;
		}

		var result = new object[children.Length];
		for (var i = 0; i < children.Length; i++)
		{
			result[i] = children[i];
		}
		return result;
	}
}

[ComVisible(true)]
internal sealed class UiaItemContainerProviderWrapper : IUiaItemContainerProvider
{
	private readonly IItemContainerProvider _inner;
	private readonly Win32Accessibility _accessibility;

	internal UiaItemContainerProviderWrapper(IItemContainerProvider inner, Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public IRawElementProviderSimple? FindItemByProperty(IRawElementProviderSimple? startAfter, int propertyId, object value)
	{
		// Resolve propertyId back to the managed AutomationProperty if possible.
		var property = MapPropertyId(propertyId);
		if (property is null)
		{
			return null;
		}

		// startAfter arrives as a COM provider — translate via its peer so the
		// managed ItemContainer can compare item peers.
		Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple? managedStartAfter = null;
		if (startAfter is Win32RawElementProvider w32 && w32.RepresentedPeer is { } peer)
		{
			managedStartAfter = new Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple(peer);
		}

		var match = _inner.FindItemByProperty(managedStartAfter, property, value);
		if (match?.AutomationPeer is { } matchPeer)
		{
			return _accessibility.GetProviderForPeer(matchPeer);
		}

		return null;
	}

	private static AutomationProperty? MapPropertyId(int propertyId) => propertyId switch
	{
		Win32UIAutomationInterop.UIA_NamePropertyId => AutomationElementIdentifiers.NameProperty,
		Win32UIAutomationInterop.UIA_AutomationIdPropertyId => AutomationElementIdentifiers.AutomationIdProperty,
		Win32UIAutomationInterop.UIA_ControlTypePropertyId => AutomationElementIdentifiers.ControlTypeProperty,
		Win32UIAutomationInterop.UIA_SelectionItemIsSelectedPropertyId => SelectionItemPatternIdentifiers.IsSelectedProperty,
		_ => null,
	};
}

[ComVisible(true)]
internal sealed class UiaVirtualizedItemProviderWrapper : IUiaVirtualizedItemProvider
{
	private readonly IVirtualizedItemProvider _inner;
	internal UiaVirtualizedItemProviderWrapper(IVirtualizedItemProvider inner) => _inner = inner;
	public void Realize() => _inner.Realize();
}

[ComVisible(true)]
internal sealed class UiaTextProvider2Wrapper : IUiaTextProvider2
{
	private readonly ITextProvider2 _inner;
	private readonly Win32Accessibility _accessibility;

	internal UiaTextProvider2Wrapper(ITextProvider2 inner, Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public object[]? GetSelection() => WrapRanges(_inner.GetSelection(), _accessibility);
	public object[]? GetVisibleRanges() => WrapRanges(_inner.GetVisibleRanges(), _accessibility);

	public IUiaTextRangeProvider? RangeFromChild(IRawElementProviderSimple childElement)
		=> Wrap(_inner.RangeFromChild(ToManagedProvider(childElement) ?? new Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple()), _accessibility);

	public IUiaTextRangeProvider? RangeFromPoint(UiaPoint point)
		=> Wrap(_inner.RangeFromPoint(new Windows.Foundation.Point(point.X, point.Y)), _accessibility);

	public IUiaTextRangeProvider? DocumentRange => Wrap(_inner.DocumentRange, _accessibility);
	public SupportedTextSelection SupportedTextSelection => _inner.SupportedTextSelection;

	public IUiaTextRangeProvider? RangeFromAnnotation(IRawElementProviderSimple annotationElement)
		=> Wrap(_inner.RangeFromAnnotation(ToManagedProvider(annotationElement) ?? new Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple()), _accessibility);

	public IUiaTextRangeProvider? GetCaretRange(out bool isActive)
		=> Wrap(_inner.GetCaretRange(out isActive), _accessibility);

	internal static IUiaTextRangeProvider? Wrap(ITextRangeProvider? range, Win32Accessibility accessibility)
		=> range is not null ? new UiaTextRangeProviderWrapper(range, accessibility) : null;

	internal static object[]? WrapRanges(ITextRangeProvider[]? ranges, Win32Accessibility accessibility)
	{
		if (ranges is null || ranges.Length == 0) return null;
		var result = new object[ranges.Length];
		for (var i = 0; i < ranges.Length; i++)
		{
			result[i] = new UiaTextRangeProviderWrapper(ranges[i], accessibility);
		}
		return result;
	}

	internal static Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple? ToManagedProvider(IRawElementProviderSimple? comProvider)
	{
		if (comProvider is Win32RawElementProvider w32 && w32.RepresentedPeer is { } peer)
		{
			return new Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple(peer);
		}
		return null;
	}
}

[ComVisible(true)]
internal sealed class UiaTextEditProviderWrapper : IUiaTextEditProvider
{
	private readonly ITextEditProvider _inner;
	private readonly Win32Accessibility _accessibility;

	internal UiaTextEditProviderWrapper(ITextEditProvider inner, Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public object[]? GetSelection() => UiaTextProvider2Wrapper.WrapRanges(_inner.GetSelection(), _accessibility);
	public object[]? GetVisibleRanges() => UiaTextProvider2Wrapper.WrapRanges(_inner.GetVisibleRanges(), _accessibility);

	public IUiaTextRangeProvider? RangeFromChild(IRawElementProviderSimple childElement)
		=> UiaTextProvider2Wrapper.Wrap(_inner.RangeFromChild(UiaTextProvider2Wrapper.ToManagedProvider(childElement) ?? new Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple()), _accessibility);

	public IUiaTextRangeProvider? RangeFromPoint(UiaPoint point)
		=> UiaTextProvider2Wrapper.Wrap(_inner.RangeFromPoint(new Windows.Foundation.Point(point.X, point.Y)), _accessibility);

	public IUiaTextRangeProvider? DocumentRange => UiaTextProvider2Wrapper.Wrap(_inner.DocumentRange, _accessibility);
	public SupportedTextSelection SupportedTextSelection => _inner.SupportedTextSelection;

	public IUiaTextRangeProvider? GetActiveComposition() => UiaTextProvider2Wrapper.Wrap(_inner.GetActiveComposition(), _accessibility);
	public IUiaTextRangeProvider? GetConversionTarget() => UiaTextProvider2Wrapper.Wrap(_inner.GetConversionTarget(), _accessibility);
}

// --- TableItem ---

[ComImport, Guid("b9734fa6-771f-4d78-9c90-2517999349cd"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaTableItemProvider
{
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
	object[]? GetRowHeaderItems();
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
	object[]? GetColumnHeaderItems();
}

[ComVisible(true)]
internal sealed class UiaTableItemProviderWrapper : IUiaTableItemProvider
{
	private readonly ITableItemProvider _inner;
	private readonly Win32Accessibility _accessibility;

	internal UiaTableItemProviderWrapper(ITableItemProvider inner, Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public object[]? GetRowHeaderItems() => ResolveProviders(_inner.GetRowHeaderItems());

	public object[]? GetColumnHeaderItems() => ResolveProviders(_inner.GetColumnHeaderItems());

	private object[]? ResolveProviders(Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple[]? elements)
	{
		if (elements is null || elements.Length == 0)
		{
			return null;
		}

		var result = new object[elements.Length];
		var count = 0;
		foreach (var rep in elements)
		{
			if (rep?.AutomationPeer is { } peer)
			{
				var provider = _accessibility.GetProviderForPeer(peer);
				if (provider is not null)
				{
					result[count++] = provider;
				}
			}
		}

		if (count == 0)
		{
			return null;
		}

		if (count < result.Length)
		{
			Array.Resize(ref result, count);
		}

		return result;
	}
}

// --- TextChild ---

[ComImport, Guid("4c2de2b9-c88f-4f88-a111-f1d336b7d1a9"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaTextChildProvider
{
	IRawElementProviderSimple? TextContainer { get; }
	IUiaTextRangeProvider? TextRange { get; }
}

[ComVisible(true)]
internal sealed class UiaTextChildProviderWrapper : IUiaTextChildProvider
{
	private readonly ITextChildProvider _inner;
	private readonly Win32Accessibility _accessibility;

	internal UiaTextChildProviderWrapper(ITextChildProvider inner, Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public IRawElementProviderSimple? TextContainer
	{
		get
		{
			var container = _inner.TextContainer;
			if (container?.AutomationPeer is { } peer)
			{
				return _accessibility.GetProviderForPeer(peer);
			}
			return null;
		}
	}

	public IUiaTextRangeProvider? TextRange
	{
		get
		{
			var range = _inner.TextRange;
			return range is not null ? new UiaTextRangeProviderWrapper(range, _accessibility) : null;
		}
	}
}

// --- Annotation ---

[ComImport, Guid("f95c7e80-bd63-4601-9782-445ebff011fc"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaAnnotationProvider
{
	// Default marshaling for `string` on a COM property getter is BSTR.
	int AnnotationTypeId { get; }
	string AnnotationTypeName { get; }
	string Author { get; }
	string DateTime { get; }
	IRawElementProviderSimple? Target { get; }
}

[ComVisible(true)]
internal sealed class UiaAnnotationProviderWrapper : IUiaAnnotationProvider
{
	private readonly IAnnotationProvider _inner;
	private readonly Win32Accessibility _accessibility;

	internal UiaAnnotationProviderWrapper(IAnnotationProvider inner, Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public int AnnotationTypeId => _inner.AnnotationTypeId;
	public string AnnotationTypeName => _inner.AnnotationTypeName ?? string.Empty;
	public string Author => _inner.Author ?? string.Empty;
	public string DateTime => _inner.DateTime ?? string.Empty;

	public IRawElementProviderSimple? Target
	{
		get
		{
			var target = _inner.Target;
			if (target?.AutomationPeer is { } peer)
			{
				return _accessibility.GetProviderForPeer(peer);
			}
			return null;
		}
	}
}

// --- Drag ---

[ComImport, Guid("6aa7bbbb-7ff9-497d-904f-d20b897929d8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaDragProvider
{
	// COM vtable order: IsGrabbed, DropEffect, DropEffects, GetGrabbedItems
	bool IsGrabbed { get; }
	string DropEffect { get; }
	string[]? DropEffects { [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] get; }
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
	object[]? GetGrabbedItems();
}

[ComVisible(true)]
internal sealed class UiaDragProviderWrapper : IUiaDragProvider
{
	private readonly IDragProvider _inner;
	private readonly Win32Accessibility _accessibility;

	internal UiaDragProviderWrapper(IDragProvider inner, Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public bool IsGrabbed => _inner.IsGrabbed;
	public string DropEffect => _inner.DropEffect ?? string.Empty;
	public string[]? DropEffects => _inner.DropEffects;

	public object[]? GetGrabbedItems()
	{
		var grabbed = _inner.GetGrabbedItems();
		if (grabbed is null || grabbed.Length == 0)
		{
			return null;
		}

		var result = new object[grabbed.Length];
		var count = 0;
		foreach (var rep in grabbed)
		{
			if (rep?.AutomationPeer is { } peer)
			{
				var provider = _accessibility.GetProviderForPeer(peer);
				if (provider is not null)
				{
					result[count++] = provider;
				}
			}
		}

		if (count == 0)
		{
			return null;
		}

		if (count < result.Length)
		{
			Array.Resize(ref result, count);
		}

		return result;
	}
}

// --- DropTarget ---

[ComImport, Guid("bae82bfd-358a-481c-85a0-d8b4d90a5d61"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaDropTargetProvider
{
	// COM vtable names DropTargetEffect / DropTargetEffects (managed: DropEffect / DropEffects)
	string DropTargetEffect { get; }
	string[]? DropTargetEffects { [return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_BSTR)] get; }
}

[ComVisible(true)]
internal sealed class UiaDropTargetProviderWrapper : IUiaDropTargetProvider
{
	private readonly IDropTargetProvider _inner;
	internal UiaDropTargetProviderWrapper(IDropTargetProvider inner) => _inner = inner;
	public string DropTargetEffect => _inner.DropEffect ?? string.Empty;
	public string[]? DropTargetEffects => _inner.DropEffects;
}

// --- ObjectModel ---

[ComImport, Guid("3ad86ebd-f5ef-483d-bb18-b1042a475d64"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaObjectModelProvider
{
	[return: MarshalAs(UnmanagedType.IUnknown)]
	object? GetUnderlyingObjectModel();
}

[ComVisible(true)]
internal sealed class UiaObjectModelProviderWrapper : IUiaObjectModelProvider
{
	private readonly IObjectModelProvider _inner;
	internal UiaObjectModelProviderWrapper(IObjectModelProvider inner) => _inner = inner;
	public object? GetUnderlyingObjectModel() => _inner.GetUnderlyingObjectModel();
}

// --- Spreadsheet ---

[ComImport, Guid("6f6b5d35-5525-4f80-b758-85473832ffc7"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaSpreadsheetProvider
{
	IRawElementProviderSimple? GetItemByName([MarshalAs(UnmanagedType.LPWStr)] string name);
}

[ComVisible(true)]
internal sealed class UiaSpreadsheetProviderWrapper : IUiaSpreadsheetProvider
{
	private readonly ISpreadsheetProvider _inner;
	private readonly Win32Accessibility _accessibility;

	internal UiaSpreadsheetProviderWrapper(ISpreadsheetProvider inner, Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public IRawElementProviderSimple? GetItemByName(string name)
	{
		var item = _inner.GetItemByName(name);
		if (item?.AutomationPeer is { } peer)
		{
			return _accessibility.GetProviderForPeer(peer);
		}
		return null;
	}
}

// --- SpreadsheetItem ---

[ComImport, Guid("eaed4660-7b3d-4879-a2e6-365ce603f3d0"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaSpreadsheetItemProvider
{
	string Formula { get; }
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
	object[]? GetAnnotationObjects();
	[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)]
	int[]? GetAnnotationTypes();
}

[ComVisible(true)]
internal sealed class UiaSpreadsheetItemProviderWrapper : IUiaSpreadsheetItemProvider
{
	private readonly ISpreadsheetItemProvider _inner;
	private readonly Win32Accessibility _accessibility;

	internal UiaSpreadsheetItemProviderWrapper(ISpreadsheetItemProvider inner, Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public string Formula => _inner.Formula ?? string.Empty;

	public object[]? GetAnnotationObjects()
	{
		var annotations = _inner.GetAnnotationObjects();
		if (annotations is null || annotations.Length == 0)
		{
			return null;
		}

		var result = new object[annotations.Length];
		var count = 0;
		foreach (var rep in annotations)
		{
			if (rep?.AutomationPeer is { } peer)
			{
				var provider = _accessibility.GetProviderForPeer(peer);
				if (provider is not null)
				{
					result[count++] = provider;
				}
			}
		}

		if (count == 0)
		{
			return null;
		}

		if (count < result.Length)
		{
			Array.Resize(ref result, count);
		}

		return result;
	}

	public int[]? GetAnnotationTypes()
	{
		var types = _inner.GetAnnotationTypes();
		if (types is null || types.Length == 0)
		{
			return null;
		}

		var result = new int[types.Length];
		for (var i = 0; i < types.Length; i++)
		{
			result[i] = (int)types[i];
		}
		return result;
	}
}

// --- Styles ---

[ComImport, Guid("19b6b649-f5d7-4a6d-bdcb-129252be588a"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaStylesProvider
{
	// COM vtable order: StyleId, StyleName, FillColor, FillPatternStyle, Shape, FillPatternColor, ExtendedProperties
	int StyleId { get; }
	string StyleName { get; }
	int FillColor { get; }
	string FillPatternStyle { get; }
	string Shape { get; }
	int FillPatternColor { get; }
	string ExtendedProperties { get; }
}

[ComVisible(true)]
internal sealed class UiaStylesProviderWrapper : IUiaStylesProvider
{
	private readonly IStylesProvider _inner;
	internal UiaStylesProviderWrapper(IStylesProvider inner) => _inner = inner;

	public int StyleId => _inner.StyleId;
	public string StyleName => _inner.StyleName ?? string.Empty;
	public int FillColor => ToColorRef(_inner.FillColor);
	public string FillPatternStyle => _inner.FillPatternStyle ?? string.Empty;
	public string Shape => _inner.Shape ?? string.Empty;
	public int FillPatternColor => ToColorRef(_inner.FillPatternColor);
	public string ExtendedProperties => _inner.ExtendedProperties ?? string.Empty;

	// UIA Styles colors are reported as COLORREF (0x00BBGGRR). Convert from Windows.UI.Color (ARGB).
	private static int ToColorRef(Windows.UI.Color color)
		=> (color.B << 16) | (color.G << 8) | color.R;
}

// --- SynchronizedInput ---

[ComImport, Guid("29db1a06-02ce-4cf7-9b42-565d4fab20ee"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaSynchronizedInputProvider
{
	// COM vtable: StartListening then Cancel (managed order is reversed)
	void StartListening(SynchronizedInputType inputType);
	void Cancel();
}

[ComVisible(true)]
internal sealed class UiaSynchronizedInputProviderWrapper : IUiaSynchronizedInputProvider
{
	private readonly ISynchronizedInputProvider _inner;
	internal UiaSynchronizedInputProviderWrapper(ISynchronizedInputProvider inner) => _inner = inner;
	public void StartListening(SynchronizedInputType inputType) => _inner.StartListening(inputType);
	public void Cancel() => _inner.Cancel();
}

// --- CustomNavigation ---

[ComImport, Guid("2062A28A-8C07-4B94-8E12-7037C622AEB8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaCustomNavigationProvider
{
	IRawElementProviderSimple? Navigate(NavigateDirection direction);
}

[ComVisible(true)]
internal sealed class UiaCustomNavigationProviderWrapper : IUiaCustomNavigationProvider
{
	private readonly ICustomNavigationProvider _inner;
	private readonly Win32Accessibility _accessibility;

	internal UiaCustomNavigationProviderWrapper(ICustomNavigationProvider inner, Win32Accessibility accessibility)
	{
		_inner = inner;
		_accessibility = accessibility;
	}

	public IRawElementProviderSimple? Navigate(NavigateDirection direction)
	{
		// COM NavigateDirection matches the managed AutomationNavigationDirection ordering (Parent=0, NextSibling=1, ...).
		var result = _inner.NavigateCustom((AutomationNavigationDirection)(int)direction);

		// The managed NavigateCustom returns object — accept either an AutomationPeer or
		// a managed IRawElementProviderSimple. Translate to the COM provider.
		switch (result)
		{
			case AutomationPeer peer:
				return _accessibility.GetProviderForPeer(peer);
			case Microsoft.UI.Xaml.Automation.Provider.IRawElementProviderSimple managed when managed.AutomationPeer is { } p:
				return _accessibility.GetProviderForPeer(p);
			default:
				return null;
		}
	}
}

// --- Transform2 (inherits Transform) ---

[ComImport, Guid("4758742f-7ac2-460c-bc48-09fc09308a93"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IUiaTransformProvider2 // : IUiaTransformProvider — flattened for COM vtable
{
	// IUiaTransformProvider members (must come first, vtable order)
	void Move(double x, double y);
	void Resize(double width, double height);
	void Rotate(double degrees);
	bool CanMove { get; }
	bool CanResize { get; }
	bool CanRotate { get; }

	// IUiaTransformProvider2-specific members. SDK header names are ZoomMinimum/ZoomMaximum.
	void Zoom(double zoom);
	bool CanZoom { get; }
	double ZoomLevel { get; }
	double ZoomMinimum { get; }
	double ZoomMaximum { get; }
	void ZoomByUnit(ZoomUnit zoomUnit);
}

[ComVisible(true)]
internal sealed class UiaTransformProvider2Wrapper : IUiaTransformProvider2
{
	private readonly ITransformProvider2 _inner;
	internal UiaTransformProvider2Wrapper(ITransformProvider2 inner) => _inner = inner;

	public void Move(double x, double y) => _inner.Move(x, y);
	public void Resize(double width, double height) => _inner.Resize(width, height);
	public void Rotate(double degrees) => _inner.Rotate(degrees);
	public bool CanMove => _inner.CanMove;
	public bool CanResize => _inner.CanResize;
	public bool CanRotate => _inner.CanRotate;

	public void Zoom(double zoom) => _inner.Zoom(zoom);
	public bool CanZoom => _inner.CanZoom;
	public double ZoomLevel => _inner.ZoomLevel;
	public double ZoomMinimum => _inner.MinZoom;
	public double ZoomMaximum => _inner.MaxZoom;
	public void ZoomByUnit(ZoomUnit zoomUnit) => _inner.ZoomByUnit(zoomUnit);
}

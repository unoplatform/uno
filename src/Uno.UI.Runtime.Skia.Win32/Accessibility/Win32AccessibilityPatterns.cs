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

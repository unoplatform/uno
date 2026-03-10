using System;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// UIA provider for a single UIElement in the accessibility tree.
/// Implements IRawElementProviderSimple and IRawElementProviderFragment.
/// The root element additionally implements IRawElementProviderFragmentRoot.
/// </summary>
[ComVisible(true)]
internal class Win32RawElementProvider :
	IRawElementProviderSimple,
	IRawElementProviderFragment,
	IRawElementProviderFragmentRoot
{
	private static int _nextRuntimeId = 1;

	private readonly UIElement _owner;
	private readonly nint _hwnd;
	private readonly int _runtimeId;
	private readonly bool _isRoot;
	private readonly Win32Accessibility _accessibility;

	internal Win32RawElementProvider? Parent { get; set; }
	internal UIElement Owner => _owner;

	internal Win32RawElementProvider(
		UIElement owner,
		nint hwnd,
		bool isRoot,
		Win32Accessibility accessibility,
		Win32RawElementProvider? parent)
	{
		_owner = owner;
		_hwnd = hwnd;
		_isRoot = isRoot;
		_accessibility = accessibility;
		_runtimeId = _nextRuntimeId++;
		Parent = parent;
	}

	// IRawElementProviderSimple

	public ProviderOptions ProviderOptions =>
		ProviderOptions.ServerSideProvider | ProviderOptions.UseComThreading;

	public object? GetPatternProvider(int patternId)
	{
		try
		{
			var peer = _owner.GetOrCreateAutomationPeer();
			if (peer is null)
			{
				return null;
			}

			return patternId switch
			{
				Win32UIAutomationInterop.UIA_InvokePatternId
					when peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invoke
					=> new UiaInvokeProviderWrapper(invoke),
				Win32UIAutomationInterop.UIA_TogglePatternId
					when peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggle
					=> new UiaToggleProviderWrapper(toggle),
				Win32UIAutomationInterop.UIA_ValuePatternId
					when peer.GetPattern(PatternInterface.Value) is IValueProvider value
					=> new UiaValueProviderWrapper(value),
				Win32UIAutomationInterop.UIA_RangeValuePatternId
					when peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeValue
					=> new UiaRangeValueProviderWrapper(rangeValue),
				Win32UIAutomationInterop.UIA_ExpandCollapsePatternId
					when peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expandCollapse
					=> new UiaExpandCollapseProviderWrapper(expandCollapse),
				Win32UIAutomationInterop.UIA_SelectionPatternId
					when peer.GetPattern(PatternInterface.Selection) is Microsoft.UI.Xaml.Automation.Provider.ISelectionProvider selection
					=> new UiaSelectionProviderWrapper(selection),
				Win32UIAutomationInterop.UIA_SelectionItemPatternId
					when peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selectionItem
					=> new UiaSelectionItemProviderWrapper(selectionItem),
				Win32UIAutomationInterop.UIA_ScrollPatternId
					when peer.GetPattern(PatternInterface.Scroll) is IScrollProvider scroll
					=> new UiaScrollProviderWrapper(scroll),
				Win32UIAutomationInterop.UIA_GridPatternId
					when peer.GetPattern(PatternInterface.Grid) is IGridProvider grid
					=> new UiaGridProviderWrapper(grid),
				Win32UIAutomationInterop.UIA_TablePatternId
					when peer.GetPattern(PatternInterface.Table) is ITableProvider table
					=> new UiaTableProviderWrapper(table),
				_ => null,
			};
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"GetPatternProvider({patternId}) failed: {ex.Message}");
			}
			return null;
		}
	}

	public object? GetPropertyValue(int propertyId)
	{
		try
		{
			var peer = _owner.GetOrCreateAutomationPeer();

			return propertyId switch
			{
				Win32UIAutomationInterop.UIA_NamePropertyId => peer?.GetName(),
				Win32UIAutomationInterop.UIA_AutomationIdPropertyId => GetAutomationId(),
				Win32UIAutomationInterop.UIA_ClassNamePropertyId => peer?.GetClassName(),
				Win32UIAutomationInterop.UIA_ControlTypePropertyId => GetControlTypeId(peer),
				Win32UIAutomationInterop.UIA_LocalizedControlTypePropertyId => peer?.GetLocalizedControlType(),
				Win32UIAutomationInterop.UIA_IsEnabledPropertyId => _owner is Control c ? c.IsEnabled : true,
				Win32UIAutomationInterop.UIA_IsKeyboardFocusablePropertyId => peer?.IsKeyboardFocusable() ?? false,
				Win32UIAutomationInterop.UIA_HasKeyboardFocusPropertyId => peer?.HasKeyboardFocus() ?? false,
				Win32UIAutomationInterop.UIA_HelpTextPropertyId => peer?.GetHelpText(),
				Win32UIAutomationInterop.UIA_IsOffscreenPropertyId => peer?.IsOffscreen() ?? false,
				Win32UIAutomationInterop.UIA_IsPasswordPropertyId => peer?.IsPassword() ?? false,
				Win32UIAutomationInterop.UIA_IsRequiredForFormPropertyId => peer?.IsRequiredForForm() ?? false,
				Win32UIAutomationInterop.UIA_IsControlElementPropertyId => GetIsControlElement(),
				Win32UIAutomationInterop.UIA_IsContentElementPropertyId => GetIsContentElement(),
				Win32UIAutomationInterop.UIA_HeadingLevelPropertyId => MapHeadingLevel(AutomationProperties.GetHeadingLevel(_owner)),
				Win32UIAutomationInterop.UIA_LandmarkTypePropertyId => MapLandmarkType(AutomationProperties.GetLandmarkType(_owner)),
				Win32UIAutomationInterop.UIA_LocalizedLandmarkTypePropertyId => GetLocalizedLandmarkType(),
				Win32UIAutomationInterop.UIA_LiveSettingPropertyId => (int)AutomationProperties.GetLiveSetting(_owner),
				Win32UIAutomationInterop.UIA_PositionInSetPropertyId => GetPositionInSet(),
				Win32UIAutomationInterop.UIA_SizeOfSetPropertyId => GetSizeOfSet(),
				_ => null,
			};
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"GetPropertyValue({propertyId}) failed: {ex.Message}");
			}
			return null;
		}
	}

	public IRawElementProviderSimple? HostRawElementProvider
	{
		get
		{
			if (_isRoot)
			{
				_ = Win32UIAutomationInterop.UiaHostProviderFromHwnd(_hwnd, out var hostProvider);
				return hostProvider;
			}
			return null;
		}
	}

	// IRawElementProviderFragment

	public IRawElementProviderFragment? Navigate(NavigateDirection direction)
	{
		try
		{
			return direction switch
			{
				NavigateDirection.Parent => _isRoot ? null : Parent,
				NavigateDirection.FirstChild => GetFirstChild(),
				NavigateDirection.LastChild => GetLastChild(),
				NavigateDirection.NextSibling => GetNextSibling(),
				NavigateDirection.PreviousSibling => GetPreviousSibling(),
				_ => null,
			};
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Navigate({direction}) failed: {ex.Message}");
			}
			return null;
		}
	}

	public int[]? GetRuntimeId()
	{
		return [Win32UIAutomationInterop.UiaAppendRuntimeId, _runtimeId];
	}

	public UiaRect BoundingRectangle
	{
		get
		{
			try
			{
				var visual = _owner.Visual;
				var totalMatrix = visual.TotalMatrix;
				var size = visual.Size;

				// Extract position from the full transformation matrix (window logical coords)
				double logicalX = totalMatrix.M41;
				double logicalY = totalMatrix.M42;

				// Convert logical pixels to physical screen coordinates
				float dpiScale = Win32UIAutomationInterop.GetDpiForWindow(_hwnd)
					/ (float)Win32UIAutomationInterop.USER_DEFAULT_SCREEN_DPI;
				if (dpiScale <= 0)
				{
					dpiScale = 1.0f;
				}

				var clientOrigin = new System.Drawing.Point(0, 0);
				Win32UIAutomationInterop.ClientToScreen(_hwnd, ref clientOrigin);

				return new UiaRect
				{
					Left = clientOrigin.X + logicalX * dpiScale,
					Top = clientOrigin.Y + logicalY * dpiScale,
					Width = size.X * dpiScale,
					Height = size.Y * dpiScale,
				};
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"BoundingRectangle failed: {ex.Message}");
				}
				return default;
			}
		}
	}

	public IRawElementProviderFragment[]? GetEmbeddedFragmentRoots() => null;

	public void SetFocus()
	{
		if (_owner is Control control)
		{
			control.Focus(FocusState.Programmatic);
		}
	}

	public IRawElementProviderFragmentRoot? FragmentRoot
		=> _accessibility.RootProvider;

	// IRawElementProviderFragmentRoot (only called on root element)

	public IRawElementProviderFragment? ElementProviderFromPoint(double x, double y)
	{
		try
		{
			return FindDeepestProviderAtPoint(x, y) ?? this;
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"ElementProviderFromPoint({x}, {y}) failed: {ex.Message}");
			}
			return this;
		}
	}

	public IRawElementProviderFragment? GetFocus()
	{
		try
		{
			var xamlRoot = _owner.XamlRoot;
			if (xamlRoot is null)
			{
				return null;
			}

			var focusedElement = FocusManager.GetFocusedElement(xamlRoot);
			if (focusedElement is UIElement uiElement)
			{
				return _accessibility.GetProvider(uiElement);
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"GetFocus failed: {ex.Message}");
			}
		}
		return null;
	}

	// Navigation helpers

	private IRawElementProviderFragment? GetFirstChild()
	{
		foreach (var child in _owner.GetChildren())
		{
			if (_accessibility.GetProvider(child) is { } provider)
			{
				return provider;
			}
		}
		return null;
	}

	private IRawElementProviderFragment? GetLastChild()
	{
		Win32RawElementProvider? last = null;
		foreach (var child in _owner.GetChildren())
		{
			if (_accessibility.GetProvider(child) is { } provider)
			{
				last = provider;
			}
		}
		return last;
	}

	private IRawElementProviderFragment? GetNextSibling()
	{
		if (Parent is null)
		{
			return null;
		}

		bool foundSelf = false;
		foreach (var sibling in Parent._owner.GetChildren())
		{
			if (foundSelf)
			{
				if (_accessibility.GetProvider(sibling) is { } provider)
				{
					return provider;
				}
			}
			else if (ReferenceEquals(sibling, _owner))
			{
				foundSelf = true;
			}
		}
		return null;
	}

	private IRawElementProviderFragment? GetPreviousSibling()
	{
		if (Parent is null)
		{
			return null;
		}

		Win32RawElementProvider? previous = null;
		foreach (var sibling in Parent._owner.GetChildren())
		{
			if (ReferenceEquals(sibling, _owner))
			{
				return previous;
			}
			if (_accessibility.GetProvider(sibling) is { } provider)
			{
				previous = provider;
			}
		}
		return null;
	}

	// Hit testing helper

	private Win32RawElementProvider? FindDeepestProviderAtPoint(double screenX, double screenY)
	{
		// Search children in reverse order (topmost Z-index first)
		var children = _owner.GetChildren();
		for (var i = children.Count - 1; i >= 0; i--)
		{
			if (_accessibility.GetProvider(children[i]) is Win32RawElementProvider childProvider)
			{
				var found = childProvider.FindDeepestProviderAtPoint(screenX, screenY);
				if (found is not null)
				{
					return found;
				}
			}
		}

		// Check if point is within this element's bounds
		var bounds = BoundingRectangle;
		if (screenX >= bounds.Left && screenX < bounds.Left + bounds.Width &&
			screenY >= bounds.Top && screenY < bounds.Top + bounds.Height)
		{
			return this;
		}

		return null;
	}

	// Property helpers

	private int GetControlTypeId(AutomationPeer? peer)
	{
		if (peer is null)
		{
			return _isRoot
				? Win32UIAutomationInterop.UIA_WindowControlTypeId
				: Win32UIAutomationInterop.UIA_CustomControlTypeId;
		}

		return MapControlType(peer.GetAutomationControlType());
	}

	private string? GetAutomationId()
	{
		var id = AutomationProperties.GetAutomationId(_owner);
		return string.IsNullOrEmpty(id) ? null : id;
	}

	private bool GetIsControlElement()
	{
		var view = AutomationProperties.GetAccessibilityView(_owner);
		return view != AccessibilityView.Raw;
	}

	private bool GetIsContentElement()
	{
		var view = AutomationProperties.GetAccessibilityView(_owner);
		return view == AccessibilityView.Content;
	}

	private string? GetLocalizedLandmarkType()
	{
		var value = AutomationProperties.GetLocalizedLandmarkType(_owner);
		return string.IsNullOrEmpty(value) ? null : value;
	}

	private int? GetPositionInSet()
	{
		var pos = AutomationProperties.GetPositionInSet(_owner);
		return pos > 0 ? pos : null;
	}

	private int? GetSizeOfSet()
	{
		var size = AutomationProperties.GetSizeOfSet(_owner);
		return size > 0 ? size : null;
	}

	internal static int MapHeadingLevel(AutomationHeadingLevel level) => level switch
	{
		AutomationHeadingLevel.None => Win32UIAutomationInterop.HeadingLevel_None,
		AutomationHeadingLevel.Level1 => Win32UIAutomationInterop.HeadingLevel1,
		AutomationHeadingLevel.Level2 => Win32UIAutomationInterop.HeadingLevel2,
		AutomationHeadingLevel.Level3 => Win32UIAutomationInterop.HeadingLevel3,
		AutomationHeadingLevel.Level4 => Win32UIAutomationInterop.HeadingLevel4,
		AutomationHeadingLevel.Level5 => Win32UIAutomationInterop.HeadingLevel5,
		AutomationHeadingLevel.Level6 => Win32UIAutomationInterop.HeadingLevel6,
		AutomationHeadingLevel.Level7 => Win32UIAutomationInterop.HeadingLevel7,
		AutomationHeadingLevel.Level8 => Win32UIAutomationInterop.HeadingLevel8,
		AutomationHeadingLevel.Level9 => Win32UIAutomationInterop.HeadingLevel9,
		_ => Win32UIAutomationInterop.HeadingLevel_None,
	};

	internal static int? MapLandmarkType(AutomationLandmarkType type) => type switch
	{
		AutomationLandmarkType.None => null,
		AutomationLandmarkType.Custom => Win32UIAutomationInterop.UIA_CustomLandmarkTypeId,
		AutomationLandmarkType.Form => Win32UIAutomationInterop.UIA_FormLandmarkTypeId,
		AutomationLandmarkType.Main => Win32UIAutomationInterop.UIA_MainLandmarkTypeId,
		AutomationLandmarkType.Navigation => Win32UIAutomationInterop.UIA_NavigationLandmarkTypeId,
		AutomationLandmarkType.Search => Win32UIAutomationInterop.UIA_SearchLandmarkTypeId,
		_ => null,
	};

	internal static int MapControlType(AutomationControlType controlType) => controlType switch
	{
		AutomationControlType.Button => Win32UIAutomationInterop.UIA_ButtonControlTypeId,
		AutomationControlType.Calendar => Win32UIAutomationInterop.UIA_CalendarControlTypeId,
		AutomationControlType.CheckBox => Win32UIAutomationInterop.UIA_CheckBoxControlTypeId,
		AutomationControlType.ComboBox => Win32UIAutomationInterop.UIA_ComboBoxControlTypeId,
		AutomationControlType.Edit => Win32UIAutomationInterop.UIA_EditControlTypeId,
		AutomationControlType.Hyperlink => Win32UIAutomationInterop.UIA_HyperlinkControlTypeId,
		AutomationControlType.Image => Win32UIAutomationInterop.UIA_ImageControlTypeId,
		AutomationControlType.ListItem => Win32UIAutomationInterop.UIA_ListItemControlTypeId,
		AutomationControlType.List => Win32UIAutomationInterop.UIA_ListControlTypeId,
		AutomationControlType.Menu => Win32UIAutomationInterop.UIA_MenuControlTypeId,
		AutomationControlType.MenuBar => Win32UIAutomationInterop.UIA_MenuBarControlTypeId,
		AutomationControlType.MenuItem => Win32UIAutomationInterop.UIA_MenuItemControlTypeId,
		AutomationControlType.ProgressBar => Win32UIAutomationInterop.UIA_ProgressBarControlTypeId,
		AutomationControlType.RadioButton => Win32UIAutomationInterop.UIA_RadioButtonControlTypeId,
		AutomationControlType.ScrollBar => Win32UIAutomationInterop.UIA_ScrollBarControlTypeId,
		AutomationControlType.Slider => Win32UIAutomationInterop.UIA_SliderControlTypeId,
		AutomationControlType.Spinner => Win32UIAutomationInterop.UIA_SpinnerControlTypeId,
		AutomationControlType.StatusBar => Win32UIAutomationInterop.UIA_StatusBarControlTypeId,
		AutomationControlType.Tab => Win32UIAutomationInterop.UIA_TabControlTypeId,
		AutomationControlType.TabItem => Win32UIAutomationInterop.UIA_TabItemControlTypeId,
		AutomationControlType.Text => Win32UIAutomationInterop.UIA_TextControlTypeId,
		AutomationControlType.ToolBar => Win32UIAutomationInterop.UIA_ToolBarControlTypeId,
		AutomationControlType.ToolTip => Win32UIAutomationInterop.UIA_ToolTipControlTypeId,
		AutomationControlType.Tree => Win32UIAutomationInterop.UIA_TreeControlTypeId,
		AutomationControlType.TreeItem => Win32UIAutomationInterop.UIA_TreeItemControlTypeId,
		AutomationControlType.Custom => Win32UIAutomationInterop.UIA_CustomControlTypeId,
		AutomationControlType.Group => Win32UIAutomationInterop.UIA_GroupControlTypeId,
		AutomationControlType.Thumb => Win32UIAutomationInterop.UIA_ThumbControlTypeId,
		AutomationControlType.DataGrid => Win32UIAutomationInterop.UIA_DataGridControlTypeId,
		AutomationControlType.DataItem => Win32UIAutomationInterop.UIA_DataItemControlTypeId,
		AutomationControlType.Document => Win32UIAutomationInterop.UIA_DocumentControlTypeId,
		AutomationControlType.SplitButton => Win32UIAutomationInterop.UIA_SplitButtonControlTypeId,
		AutomationControlType.Window => Win32UIAutomationInterop.UIA_WindowControlTypeId,
		AutomationControlType.Pane => Win32UIAutomationInterop.UIA_PaneControlTypeId,
		AutomationControlType.Header => Win32UIAutomationInterop.UIA_HeaderControlTypeId,
		AutomationControlType.HeaderItem => Win32UIAutomationInterop.UIA_HeaderItemControlTypeId,
		AutomationControlType.Table => Win32UIAutomationInterop.UIA_TableControlTypeId,
		AutomationControlType.Separator => Win32UIAutomationInterop.UIA_SeparatorControlTypeId,
		AutomationControlType.SemanticZoom => Win32UIAutomationInterop.UIA_SemanticZoomControlTypeId,
		AutomationControlType.AppBar => Win32UIAutomationInterop.UIA_AppBarControlTypeId,
		AutomationControlType.FlipView => Win32UIAutomationInterop.UIA_FlipViewControlTypeId,
		_ => Win32UIAutomationInterop.UIA_CustomControlTypeId,
	};
}

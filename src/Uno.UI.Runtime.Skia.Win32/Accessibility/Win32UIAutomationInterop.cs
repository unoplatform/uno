using System;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace Uno.UI.Runtime.Skia.Win32;

// UIA COM Interfaces
// These are the raw COM interfaces that UIA uses to communicate with providers.
// Method ordering must match the COM vtable layout exactly.

[ComImport, Guid("d6dd68d1-86fd-4332-8666-9abedea2d24c"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IRawElementProviderSimple
{
	ProviderOptions ProviderOptions { get; }

	[return: MarshalAs(UnmanagedType.IUnknown)]
	object? GetPatternProvider(int patternId);

	object? GetPropertyValue(int propertyId);

	IRawElementProviderSimple? HostRawElementProvider { get; }
}

[ComImport, Guid("f7063da8-8359-439c-9297-bbc5299a7d87"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IRawElementProviderFragment
{
	IRawElementProviderFragment? Navigate(NavigateDirection direction);

	int[]? GetRuntimeId();

	UiaRect BoundingRectangle { get; }

	IRawElementProviderFragment[]? GetEmbeddedFragmentRoots();

	void SetFocus();

	IRawElementProviderFragmentRoot? FragmentRoot { get; }
}

[ComImport, Guid("620ce2a5-ab8f-40a9-86cb-de3c75599b58"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IRawElementProviderFragmentRoot
{
	IRawElementProviderFragment? ElementProviderFromPoint(double x, double y);

	IRawElementProviderFragment? GetFocus();
}

// Enums

[Flags]
internal enum ProviderOptions
{
	ClientSideProvider = 0x1,
	ServerSideProvider = 0x2,
	NonClientAreaProvider = 0x4,
	OverrideProvider = 0x8,
	ProviderOwnsSetFocus = 0x10,
	UseComThreading = 0x20,
	RefuseNonClientSupport = 0x40,
	HasNativeIAccessible = 0x80,
	UseClientCoordinates = 0x100,
}

internal enum NavigateDirection
{
	Parent = 0,
	NextSibling = 1,
	PreviousSibling = 2,
	FirstChild = 3,
	LastChild = 4,
}

internal enum StructureChangeType
{
	ChildAdded = 0,
	ChildRemoved = 1,
	ChildrenInvalidated = 2,
	ChildrenBulkAdded = 3,
	ChildrenBulkRemoved = 4,
	ChildrenReordered = 5,
}

// UIA rect structure returned by BoundingRectangle
[StructLayout(LayoutKind.Sequential)]
internal struct UiaRect
{
	public double Left;
	public double Top;
	public double Width;
	public double Height;
}

// P/Invoke declarations for UIAutomation Core API
internal static class Win32UIAutomationInterop
{
	internal const int UiaRootObjectId = -25;
	internal const int UiaAppendRuntimeId = 3;

	// WM_GETOBJECT message constant
	internal const uint WM_GETOBJECT = 0x003D;

	// UIA Property IDs
	internal const int UIA_AutomationIdPropertyId = 30011;
	internal const int UIA_BoundingRectanglePropertyId = 30001;
	internal const int UIA_ClassNamePropertyId = 30012;
	internal const int UIA_ControlTypePropertyId = 30003;
	internal const int UIA_HasKeyboardFocusPropertyId = 30008;
	internal const int UIA_HeadingLevelPropertyId = 30114;
	internal const int UIA_HelpTextPropertyId = 30013;
	internal const int UIA_IsContentElementPropertyId = 30017;
	internal const int UIA_IsControlElementPropertyId = 30016;
	internal const int UIA_IsEnabledPropertyId = 30010;
	internal const int UIA_IsKeyboardFocusablePropertyId = 30009;
	internal const int UIA_IsOffscreenPropertyId = 30022;
	internal const int UIA_IsPasswordPropertyId = 30019;
	internal const int UIA_IsRequiredForFormPropertyId = 30025;
	internal const int UIA_LandmarkTypePropertyId = 30157;
	internal const int UIA_LiveSettingPropertyId = 30135;
	internal const int UIA_LocalizedControlTypePropertyId = 30004;
	internal const int UIA_LocalizedLandmarkTypePropertyId = 30158;
	internal const int UIA_NamePropertyId = 30005;
	internal const int UIA_PositionInSetPropertyId = 30152;
	internal const int UIA_SizeOfSetPropertyId = 30153;

	// UIA Control Type IDs
	internal const int UIA_ButtonControlTypeId = 50000;
	internal const int UIA_CalendarControlTypeId = 50001;
	internal const int UIA_CheckBoxControlTypeId = 50002;
	internal const int UIA_ComboBoxControlTypeId = 50003;
	internal const int UIA_CustomControlTypeId = 50025;
	internal const int UIA_DataGridControlTypeId = 50028;
	internal const int UIA_DataItemControlTypeId = 50029;
	internal const int UIA_DocumentControlTypeId = 50030;
	internal const int UIA_EditControlTypeId = 50004;
	internal const int UIA_GroupControlTypeId = 50026;
	internal const int UIA_HeaderControlTypeId = 50034;
	internal const int UIA_HeaderItemControlTypeId = 50035;
	internal const int UIA_HyperlinkControlTypeId = 50005;
	internal const int UIA_ImageControlTypeId = 50006;
	internal const int UIA_ListControlTypeId = 50008;
	internal const int UIA_ListItemControlTypeId = 50007;
	internal const int UIA_MenuControlTypeId = 50009;
	internal const int UIA_MenuBarControlTypeId = 50010;
	internal const int UIA_MenuItemControlTypeId = 50011;
	internal const int UIA_PaneControlTypeId = 50033;
	internal const int UIA_ProgressBarControlTypeId = 50012;
	internal const int UIA_RadioButtonControlTypeId = 50013;
	internal const int UIA_ScrollBarControlTypeId = 50014;
	internal const int UIA_SemanticZoomControlTypeId = 50039;
	internal const int UIA_SeparatorControlTypeId = 50038;
	internal const int UIA_SliderControlTypeId = 50015;
	internal const int UIA_SpinnerControlTypeId = 50016;
	internal const int UIA_SplitButtonControlTypeId = 50031;
	internal const int UIA_StatusBarControlTypeId = 50017;
	internal const int UIA_TabControlTypeId = 50018;
	internal const int UIA_TabItemControlTypeId = 50019;
	internal const int UIA_TableControlTypeId = 50036;
	internal const int UIA_TextControlTypeId = 50020;
	internal const int UIA_ThumbControlTypeId = 50027;
	internal const int UIA_ToolBarControlTypeId = 50021;
	internal const int UIA_ToolTipControlTypeId = 50022;
	internal const int UIA_TreeControlTypeId = 50023;
	internal const int UIA_TreeItemControlTypeId = 50024;
	internal const int UIA_WindowControlTypeId = 50032;
	internal const int UIA_AppBarControlTypeId = 50040;
	internal const int UIA_FlipViewControlTypeId = 50041;

	// UIA Pattern IDs
	internal const int UIA_ExpandCollapsePatternId = 10005;
	internal const int UIA_GridPatternId = 10006;
	internal const int UIA_InvokePatternId = 10000;
	internal const int UIA_RangeValuePatternId = 10003;
	internal const int UIA_ScrollPatternId = 10004;
	internal const int UIA_SelectionItemPatternId = 10010;
	internal const int UIA_SelectionPatternId = 10001;
	internal const int UIA_TablePatternId = 10012;
	internal const int UIA_TogglePatternId = 10015;
	internal const int UIA_ValuePatternId = 10002;

	// UIA Event IDs
	internal const int UIA_AutomationFocusChangedEventId = 20005;
	internal const int UIA_Invoke_InvokedEventId = 20009;
	internal const int UIA_Selection_InvalidatedEventId = 20013;
	internal const int UIA_StructureChangedEventId = 20002;
	internal const int UIA_LiveRegionChangedEventId = 20024;

	// UIA Property Changed Event IDs (for UiaRaiseAutomationPropertyChangedEvent)
	internal const int UIA_ExpandCollapseExpandCollapseStatePropertyId = 30070;
	internal const int UIA_RangeValueValuePropertyId = 30047;
	internal const int UIA_SelectionItemIsSelectedPropertyId = 30079;
	internal const int UIA_ToggleToggleStatePropertyId = 30086;
	internal const int UIA_ValueValuePropertyId = 30045;

	// UIA Heading Level IDs
	internal const int HeadingLevel_None = 80050;
	internal const int HeadingLevel1 = 80051;
	internal const int HeadingLevel2 = 80052;
	internal const int HeadingLevel3 = 80053;
	internal const int HeadingLevel4 = 80054;
	internal const int HeadingLevel5 = 80055;
	internal const int HeadingLevel6 = 80056;
	internal const int HeadingLevel7 = 80057;
	internal const int HeadingLevel8 = 80058;
	internal const int HeadingLevel9 = 80059;

	// UIA Landmark Type IDs
	internal const int UIA_CustomLandmarkTypeId = 80000;
	internal const int UIA_FormLandmarkTypeId = 80001;
	internal const int UIA_MainLandmarkTypeId = 80002;
	internal const int UIA_NavigationLandmarkTypeId = 80003;
	internal const int UIA_SearchLandmarkTypeId = 80004;

	// UIA Live Setting values
	internal const int Off = 0;
	internal const int Polite = 1;
	internal const int Assertive = 2;

	// UIA Notification kinds
	internal const int AutomationNotificationKind_ItemAdded = 0;
	internal const int AutomationNotificationKind_ItemRemoved = 1;
	internal const int AutomationNotificationKind_ActionCompleted = 2;
	internal const int AutomationNotificationKind_ActionAborted = 3;
	internal const int AutomationNotificationKind_Other = 4;

	// UIA Notification processing
	internal const int AutomationNotificationProcessing_ImportantAll = 0;
	internal const int AutomationNotificationProcessing_ImportantMostRecent = 1;
	internal const int AutomationNotificationProcessing_All = 2;
	internal const int AutomationNotificationProcessing_MostRecent = 3;
	internal const int AutomationNotificationProcessing_CurrentThenMostRecent = 4;

	// Win32 helpers for coordinate conversion

	internal const uint USER_DEFAULT_SCREEN_DPI = 96;

	[DllImport("user32.dll")]
	internal static extern bool ClientToScreen(nint hWnd, ref System.Drawing.Point lpPoint);

	[DllImport("user32.dll")]
	internal static extern bool ScreenToClient(nint hWnd, ref System.Drawing.Point lpPoint);

	[DllImport("user32.dll")]
	internal static extern uint GetDpiForWindow(nint hwnd);

	// Core UIA functions

	[DllImport("uiautomationcore.dll")]
	internal static extern nint UiaReturnRawElementProvider(
		nint hwnd,
		nint wParam,
		nint lParam,
		[MarshalAs(UnmanagedType.Interface)] IRawElementProviderSimple? el);

	[DllImport("uiautomationcore.dll")]
	internal static extern int UiaHostProviderFromHwnd(
		nint hwnd,
		[MarshalAs(UnmanagedType.Interface)] out IRawElementProviderSimple? provider);

	[DllImport("uiautomationcore.dll")]
	internal static extern int UiaRaiseAutomationPropertyChangedEvent(
		[MarshalAs(UnmanagedType.Interface)] IRawElementProviderSimple provider,
		int propertyId,
		[MarshalAs(UnmanagedType.Struct)] object? oldValue,
		[MarshalAs(UnmanagedType.Struct)] object? newValue);

	[DllImport("uiautomationcore.dll")]
	internal static extern int UiaRaiseAutomationEvent(
		[MarshalAs(UnmanagedType.Interface)] IRawElementProviderSimple provider,
		int eventId);

	[DllImport("uiautomationcore.dll")]
	internal static extern int UiaRaiseStructureChangedEvent(
		[MarshalAs(UnmanagedType.Interface)] IRawElementProviderSimple provider,
		StructureChangeType structureChangeType,
		[MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_I4)] int[]? runtimeId);

	[DllImport("uiautomationcore.dll")]
	internal static extern int UiaRaiseNotificationEvent(
		[MarshalAs(UnmanagedType.Interface)] IRawElementProviderSimple provider,
		int notificationKind,
		int notificationProcessing,
		[MarshalAs(UnmanagedType.BStr)] string? displayString,
		[MarshalAs(UnmanagedType.BStr)] string? activityId);

	/// <summary>
	/// Handles WM_GETOBJECT by returning the root UIA provider to the automation framework.
	/// </summary>
	internal static LRESULT HandleGetObject(HWND hwnd, WPARAM wParam, LPARAM lParam, Win32RawElementProvider? rootProvider)
	{
		if (rootProvider is null)
		{
			return PInvoke.DefWindowProc(hwnd, WM_GETOBJECT, wParam, lParam);
		}

		var result = UiaReturnRawElementProvider(hwnd.Value, (nint)wParam.Value, lParam.Value, rootProvider);
		return new LRESULT(result);
	}
}

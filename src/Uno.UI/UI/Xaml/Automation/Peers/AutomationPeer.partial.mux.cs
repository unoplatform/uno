// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference AutomationPeer_Partial.cpp, tag winui3/release/1.8.2

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Automation.Text;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Automation.Peers;

internal class AutomationPeer
{

	//	static private void BoxEnumValueHelper(
	//		out Automation.object result,
	//		 uint value)
	//	{
	//		CValue temp;
	//		CValueBoxer.BoxEnumValue(&temp, value);
	//		result.ConvertFrom(temp);
	//		return S_OK;
	//	}

	//	static private void UnboxEnumValueHelper(
	//		  Automation.object box,
	//		  CClassInfo* pSourceType,
	//		out uint* result)
	//	{
	//		CValue temp;
	//		box.ConvertTo(temp);
	//		CValueBoxer.UnboxEnumValue(&temp, pSourceType, result);
	//		return S_OK;
	//	}

	//	template<typename T>
	//static private void BoxValueHelper(
	//	out Automation.object result,
	//	 T value)
	//	{
	//		CValue temp;
	//		CValueBoxer.BoxValue(&temp, value);
	//		result.ConvertFrom(temp);
	//		return S_OK;
	//	}

	//	static private void BoxObjectValueHelper(
	//		out Automation.object result,
	//		  CClassInfo* pSourceType,
	//		 object value,
	//		 BoxerBuffer* buffer,
	//		out result_maybenull_ DependencyObject** ppMOR,
	//		bool bPreserveObjectIdentity)
	//	{
	//		CValue temp;
	//		CValueBoxer.BoxObjectValue(&temp, pSourceType, value, buffer, ppMOR, bPreserveObjectIdentity);
	//		result.ConvertFrom(temp);
	//		return S_OK;
	//	}

	//	static private void UnboxObjectValueHelper(
	//		  Automation.object box,
	//		  CClassInfo* pTargetType,
	//		out object* result)
	//	{
	//		CValue temp;
	//		box.ConvertTo(temp);
	//		CValueBoxer.UnboxObjectValue(&temp, pTargetType, result);
	//		return S_OK;
	//	}

	//	// Initializes a new instance of the AutomationPeer class.
	//	AutomationPeer.AutomationPeer()
	//{
	//}

	//// Deructor
	//AutomationPeer.~AutomationPeer()
	//{
	//}

	//private void QueryInterfaceImpl(REFIID iid, out void** ppObject)
	//{
	//	*ppObject = null;

	//	// This is to support XAML UIA merged tree, with this change XAML now supports native UIA nodes directly
	//	// hosted in UIA tree for XAML app. native nodes can exist independently in XAML UIA tree and they can
	//	// host XAML APs as well. The native UIA nodes will be directly dealing with UIACore calls like navigation,
	//	// if they are directly connected with an AP, they will be required to return native node associated with
	//	// that AP. To suffice that requirement XAML AP now allows external code to QI for IREPF and returns the
	//	// associated CUIAWrapper which is basically XAML AP's representation in UIA world.
	//	// Currently XAML AP and CUIAWrapper have each others' weak reference presence, this design needs a re-visit
	//	// and XAML APs life time can be bound to CUIAWrapper TFS Id: 579423.
	//	if (InlineIsEqualGUID(iid, __uuidof(IRawElementProviderFragment)))
	//	{
	//		IUIAWrapper* pObjectNoRef = null;
	//		IRawElementProviderFragment spRawProviderFrag;

	//		pObjectNoRef = ((CAutomationPeer*)(GetHandle())).GetUIAWrapper();

	//		if (pObjectNoRef == null)
	//		{
	//			DXamlCore* pCore = DXamlCore.GetFromDependencyObject(this);
	//			if (pCore)
	//			{
	//				xref_ptr<CUIAWrapper> spUIAWrapper;
	//				pCore.CreateProviderForAP((CAutomationPeer*)(GetHandle()), spUIAWrapper.ReleaseAndGetAddressOf());
	//				if (spUIAWrapper)
	//				{
	//					IFC_NOTRACE_RETURN(spUIAWrapper.QueryInterface(__uuidof(IRawElementProviderFragment), &spRawProviderFrag));
	//				}
	//			}
	//		}

	//		if (pObjectNoRef)
	//		{
	//			IFC_NOTRACE_RETURN(((CUIAWrapper*)(pObjectNoRef)).QueryInterface(__uuidof(IRawElementProviderFragment), &spRawProviderFrag));
	//		}

	//		if (!spRawProviderFrag)
	//		{
	//			IFC_NOTRACE_RETURN(E_NOINTERFACE);
	//		}

	//		*ppObject = spRawProviderFrag.Detach();

	//		return S_OK;
	//	}

	//	return DirectUI.AutomationPeerGenerated.QueryInterfaceImpl(iid, ppObject);
	//}

	//private void SetParentImpl(IAutomationPeer* parent)
	//{

	//	(CoreImports.SetAutomationPeerParent((CAutomationPeer*)(GetHandle()),
	//		(CAutomationPeer*)(((AutomationPeer*)(parent)).GetHandle())));
	//Cleanup:
	//	RRETURN(hr);
	//}
	protected virtual object GetPatternCore(PatternInterface patternInterface) => null;

	public IList<AutomationPeer> GetChildren()
	{
		var pAPChildren = GetChildrenCore();
		if (pAPChildren is not null)
		{
			var nCount = pAPChildren.Count;

			// Defining a set of nodes as children implies that all the children must target this node as their parent. We ensure that
			// relationship here for managed peer objects.
			for (uint i = 0; i < nCount; i++)
			{
				var pAP = pAPChildren[i];
				if (pAP is not null)
				{
					(CoreImports.SetAutomationPeerParent((AutomationPeer)pAP, this);
				}
			}
		}

		return pAPChildren;
	}

	public IReadOnlyList<AutomationPeer> GetControlledPeers() => GetControlledPeersCore();

	public void ShowContextMenu() => ShowContextMenuCore();

	public AutomationPeer GetPeerFromPoint(Point point) => GetPeerFromPointCore(point);

	protected virtual string GetAcceleratorKeyCore() => string.Empty;

	protected virtual string GetAccessKeyCore() => string.Empty;

	protected virtual AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Custom;

	protected virtual string GetAutomationIdCore() => string.Empty;

	protected virtual Rect GetBoundingRectangleCore() => default;

	protected virtual IList<AutomationPeer> GetChildrenCore() => null;

	// Custom APs can override NavigateCoreImpl to manage the navigation of APs completley by themselves.
	// In addition to that they can also use it to return native UIA nodes which is why the return
	// type is object instead of an IAutomationPeer*. The default implementation still uses
	// GetChildren and GetParent to have backward compatibility. This method also deprecates GetParent.
	protected virtual object NavigateCore(AutomationNavigationDirection direction)
	{
		IList<AutomationPeer> spAPChildren;
		AutomationPeer spAP = null;
		int nCount = 0;

		switch (direction)
		{
			case AutomationNavigationDirection.FirstChild:
				{
					spAPChildren = GetChildren();
					if (spAPChildren is not null)
					{
						nCount = spAPChildren.Count;
						if (nCount > 0)
						{
							spAP = spAPChildren[0];
						}
					}
					break;
				}
			case AutomationNavigationDirection.LastChild:
				{
					spAPChildren = GetChildren();
					if (spAPChildren is not null)
					{
						nCount = spAPChildren.Count;
						if (nCount > 0)
						{
							spAP = spAPChildren[nCount - 1];
						}
					}
					break;
				}
			case AutomationNavigationDirection.PreviousSibling:
				{
					// Prev/Next needs to make sure to handle case where parent is root window, GetParent will be null in that case.
					AutomationPeer spAPParent;

					spAPParent = GetParent();
					if (spAPParent is not null)
					{
						spAPChildren = spAPParent.GetChildren();
						if (spAPChildren is not null)
						{
							var index = spAPChildren.IndexOf(this);
							if (index != -1 && index > 0)
							{
								spAP = spAPChildren[index - 1];
							}
						}
					}
					break;
				}
			case AutomationNavigationDirection.NextSibling:
				{
					AutomationPeer spAPParent;

					spAPParent = GetParent();
					if (spAPParent is not null)
					{
						spAPChildren = spAPParent.GetChildren();
						if (spAPChildren is not null)
						{
							nCount = spAPChildren.Count;
							var index = spAPChildren.IndexOf(this);
							MUX_ASSERT(nCount == 0 ? index == -1 : true);
							if (index != -1 && index < nCount - 1)
							{
								spAP = spAPChildren[index + 1];
							}
						}
					}
					break;
				}
			case AutomationNavigationDirection.Parent:
				{
					spAP = GetParent();
					break;
				}
			default:
				throw new NotSupportedException("Unsupported AutomationNavigationDirection");
		}

		return spAP;
	}

	protected virtual string GetClassNameCore() => "";

	protected virtual Point GetClickablePointCore() => default;

	protected virtual string GetHelpTextCore() => string.Empty;

	protected virtual string GetItemStatusCore() => string.Empty;

	protected virtual string GetItemTypeCore() => string.Empty;

	protected virtual AutomationPeer GetLabeledByCore() => null;

	protected virtual string GetLocalizedControlTypeCore()
	{
		AutomationControlType apType;
		try
		{
			apType = GetAutomationControlType();
		}
		catch (Exception)
		{
			// PopupRoot only has a control type if a light dismiss popup is on top. Otherwise it's not a control and
			// returns S_false. Allow it and return a null string.
			return null;
		}

		var core = DXamlCore.GetCurrentNoCreate();

		return apType switch
		{
			AutomationControlType.Button => core?.GetLocalizedResourceString("UIA_AP_BUTTON"),
			AutomationControlType.Calendar => core?.GetLocalizedResourceString("UIA_AP_CALENDAR"),
			AutomationControlType.CheckBox => core?.GetLocalizedResourceString("UIA_AP_CHECKBOX"),
			AutomationControlType.ComboBox => core?.GetLocalizedResourceString("UIA_AP_COMBOBOX"),
			AutomationControlType.Edit => core?.GetLocalizedResourceString("UIA_AP_EDIT"),
			AutomationControlType.Hyperlink => core?.GetLocalizedResourceString("UIA_AP_HYPERLINK"),
			AutomationControlType.Image => core?.GetLocalizedResourceString("UIA_AP_IMAGE"),
			AutomationControlType.ListItem => core?.GetLocalizedResourceString("UIA_AP_LISTITEM"),
			AutomationControlType.List => core?.GetLocalizedResourceString("UIA_AP_LIST"),
			AutomationControlType.Menu => core?.GetLocalizedResourceString("UIA_AP_MENU"),
			AutomationControlType.MenuBar => core?.GetLocalizedResourceString("UIA_AP_MENUBAR"),
			AutomationControlType.MenuItem => core?.GetLocalizedResourceString("UIA_AP_MENUITEM"),
			AutomationControlType.ProgressBar => core?.GetLocalizedResourceString("UIA_AP_PROGRESSBAR"),
			AutomationControlType.RadioButton => core?.GetLocalizedResourceString("UIA_AP_RADIOBUTTON"),
			AutomationControlType.ScrollBar => core?.GetLocalizedResourceString("UIA_AP_SCROLLBAR"),
			AutomationControlType.Slider => core?.GetLocalizedResourceString("UIA_AP_SLIDER"),
			AutomationControlType.Spinner => core?.GetLocalizedResourceString("UIA_AP_SPINNER"),
			AutomationControlType.StatusBar => core?.GetLocalizedResourceString("UIA_AP_STATUSBAR"),
			AutomationControlType.Tab => core?.GetLocalizedResourceString("UIA_AP_TAB"),
			AutomationControlType.TabItem => core?.GetLocalizedResourceString("UIA_AP_TABITEM"),
			AutomationControlType.Text => core?.GetLocalizedResourceString("UIA_AP_TEXT"),
			AutomationControlType.ToolBar => core?.GetLocalizedResourceString("UIA_AP_TOOLBAR"),
			AutomationControlType.ToolTip => core?.GetLocalizedResourceString("UIA_AP_TOOLTIP"),
			AutomationControlType.Tree => core?.GetLocalizedResourceString("UIA_AP_TREE"),
			AutomationControlType.TreeItem => core?.GetLocalizedResourceString("UIA_AP_TREEITEM"),
			AutomationControlType.Custom => core?.GetLocalizedResourceString("UIA_AP_CUSTOM"),
			AutomationControlType.Group => core?.GetLocalizedResourceString("UIA_AP_GROUP"),
			AutomationControlType.Thumb => core?.GetLocalizedResourceString("UIA_AP_THUMB"),
			AutomationControlType.DataGrid => core?.GetLocalizedResourceString("UIA_AP_DATAGRID"),
			AutomationControlType.DataItem => core?.GetLocalizedResourceString("UIA_AP_DATAITEM"),
			AutomationControlType.Document => core?.GetLocalizedResourceString("UIA_AP_DOCUMENT"),
			AutomationControlType.SplitButton => core?.GetLocalizedResourceString("UIA_AP_SPLITBUTTON"),
			AutomationControlType.Window => core?.GetLocalizedResourceString("UIA_AP_WINDOW"),
			AutomationControlType.Pane => core?.GetLocalizedResourceString("UIA_AP_PANE"),
			AutomationControlType.Header => core?.GetLocalizedResourceString("UIA_AP_HEADER"),
			AutomationControlType.HeaderItem => core?.GetLocalizedResourceString("UIA_AP_HEADERITEM"),
			AutomationControlType.Table => core?.GetLocalizedResourceString("UIA_AP_TABLE"),
			AutomationControlType.TitleBar => core?.GetLocalizedResourceString("UIA_AP_TITLEBAR"),
			AutomationControlType.Separator => core?.GetLocalizedResourceString("UIA_AP_SEPARATOR"),
			AutomationControlType.SemanticZoom => core?.GetLocalizedResourceString("UIA_AP_SEMANTICZOOM"),
			AutomationControlType.AppBar => core?.GetLocalizedResourceString("UIA_AP_APPBAR"),
			AutomationControlType.FlipView => core?.GetLocalizedResourceString("UIA_AP_FLIPVIEW"),
			_ => throw new NotSupportedException("Unsupported AutomationControlType"),
		};
	}

	protected virtual string GetNameCore() => "";

	protected virtual AutomationOrientation GetOrientationCore() => AutomationOrientation.None;

	protected virtual AutomationLiveSetting GetLiveSettingCore() => AutomationLiveSetting.Off;

	protected virtual int GetPositionInSetCore() => -1;

	protected virtual int GetSizeOfSetCore() => -1;

	protected virtual int GetLevelCore() => -1;

	protected virtual IReadOnlyList<AutomationPeer> GetControlledPeersCore() => null;

	protected virtual IList<AutomationPeerAnnotation> GetAnnotationsCore() => null;

	protected virtual AutomationLandmarkType GetLandmarkTypeCore() => AutomationLandmarkType.None;

	protected virtual string GetLocalizedLandmarkTypeCore() => string.Empty;

	protected virtual bool HasKeyboardFocusCore() => HasKeyboardFocusHelper();

	protected virtual bool IsContentElementCore() => false;

	protected virtual bool IsControlElementCore() => false;

	protected virtual bool IsEnabledCore() => true;

	protected virtual bool IsKeyboardFocusableCore() => false;

	protected virtual bool IsOffscreenCore() => false;

	protected virtual bool IsPasswordCore() => false;

	protected virtual bool IsRequiredForFormCore() => false;

	protected virtual void SetFocusCore()
	{
		// Lets keep this as it is, that is getting the value from core, Core layer has some exceptional logic that depends upon
		// focus manager.

		SetFocusHelper();
	}

	private void SetAutomationFocus() => SetAutomationFocusHelper();

	protected virtual void ShowContextMenuCore()
	{
	}

	protected virtual bool IsPeripheralCore() => false;

	protected virtual bool IsDataValidForFormCore() => true;

	protected virtual string GetFullDescriptionCore() => string.Empty;

	protected virtual IEnumerable<AutomationPeer> GetDescribedByCore() => null;

	protected virtual IEnumerable<AutomationPeer> GetFlowsToCore() => null;

	protected virtual IEnumerable<AutomationPeer> GetFlowsFromCore() => null;

	protected virtual int GetCultureCore() => GetCultureHelper();

	protected virtual AutomationHeadingLevel GetHeadingLevelCore() => AutomationHeadingLevel.None;

	protected virtual bool IsDialogCore() => false;

	internal AutomationPeer GetParentImpl() => this.GetParent();

	internal void InvalidatePeerImpl() => InvalidatePeer();

	internal void RaiseAutomationEventImpl(AutomationEvents eventId)
	{
		if (EventsSource is not null)
		{
			EventsSource.RaiseAutomationEvent(eventId);
		}
		else
		{
			RaiseAutomationEvent(eventId);
		}
	}

	// internal void RaiseStructureChangedEvent(AutomationStructureChangeType structureChangeType, AutomationPeer child)
	// {
	// 	// In C# logic, we simply raise the event using the internal Uno mechanism.
	// 	// The C++ switch statement maps types to internal Property IDs. 
	// 	// In Uno, RaiseStructureChangedEvent usually handles this directly.

	// 	if (structureChangeType == AutomationStructureChangeType.ChildRemoved)
	// 	{
	// 		if (child == null)
	// 		{
	// 			throw new ArgumentNullException(nameof(child));
	// 		}

	// 		// Logic to get RuntimeId for the removed child would be handled internally here.
	// 	}

	// 	// Direct mapping to Uno's internal event raiser:
	// 	// Uno.UI.Xaml.Automation.AutomationPeer.RaiseStructureChangedEvent(this, structureChangeType, child);
	// }

	internal void RaiseStructureChangedEventImpl(AutomationStructureChangeType structureChangeType, AutomationPeer pChild = null)
	{
		object newValue = null;

		switch (structureChangeType)
		{
			case AutomationStructureChangeType.ChildAdded:
			case AutomationStructureChangeType.ChildrenBulkAdded:
			case AutomationStructureChangeType.ChildrenBulkRemoved:
			case AutomationStructureChangeType.ChildrenInvalidated:
			case AutomationStructureChangeType.ChildrenReordered:
				newValue = null;
				break;

			case AutomationStructureChangeType.ChildRemoved:
				{
					// UIAutomationCore expects runtime id of the removed child to be returned.
					if (pChild == null)
					{
						throw new ArgumentNullException(nameof(pChild));
					}

					newValue = pChild.GetRuntimeId();
				}
				break;

			default:
				// IFC_RETURN(E_UNEXPECTED)
				throw new InvalidOperationException("Unexpected AutomationStructureChangeType");
		}

		this.RaiseStructureChangedEvent(structureChangeType, pChild);
	}

	internal void RaisePropertyChangedEventImpl(
		 AutomationProperty automationProperty,
		 object pPropertyValueOld,
		 object pPropertyValueNew)
	{
		// In C# we skip the CValue boxing and buffer management.
		// Directly call the public or internal RaisePropertyChangedEvent.

		if (automationProperty == AutomationElementIdentifiers.ControlledPeersProperty)
		{
			// Logic from C++: "Ideally, we shall verify and marshall... but Narrator doesn't care... so ignore values."
			this.RaisePropertyChangedEvent(automationProperty, null, null);
		}
		else
		{
			this.RaisePropertyChangedEvent(automationProperty, pPropertyValueOld, pPropertyValueNew);
		}
	}

	// Corresponds to RaiseTextEditTextChangedEvent
	public void RaiseTextEditTextChangedEvent(AutomationTextEditChangeType automationTextEditChangeType, IReadOnlyList<string> changedData)
	{
		if (changedData == null)
		{
			throw new ArgumentNullException(nameof(changedData));
		}

		// In Uno/WinUI C#, this event is strictly for internal text pattern support.
		// If the method exists on the base AutomationPeer in your version:
		// base.RaiseTextEditTextChangedEvent(automationTextEditChangeType, changedData);

		// If implementing the raw event firing logic:
		// AutomationEvents eventId = AutomationEvents.TextEditTextChanged; // Hypothetical mapping
		// FireEvent(eventId, ...);
	}

	// Corresponds to RaiseNotificationEventImpl
	internal void RaiseNotificationEventImpl(
		AutomationNotificationKind notificationKind,
		AutomationNotificationProcessing notificationProcessing,
		string displayString,
		string activityId)
	{
		this.RaiseNotificationEvent(
			notificationKind,
			notificationProcessing,
			displayString,
			activityId);
	}

	// Corresponds to GetPeerFromPointCore (Virtual)
	protected virtual AutomationPeer GetPeerFromPointCore(Point point)
	{
		return this;
	}

	// Corresponds to GetElementFromPointCoreImpl
	// This method deprecates GetPeerFromPoint/Core but maintains compatibility.
	internal object GetElementFromPointCoreImpl(Point point)
	{
		AutomationPeer peer = GetPeerFromPoint(point);
		return peer;
	}

	// Corresponds to GetFocusedElementCore (Virtual)
	protected virtual object GetFocusedElementCore()
	{
		return this;
	}

	// Corresponds to PeerFromProvider
	protected AutomationPeer PeerFromProvider(IRawElementProviderSimple provider)
	{
		return PeerFromProviderStatic(provider);
	}

	// Corresponds to PeerFromProviderStatic
	private static AutomationPeer PeerFromProviderStatic(IRawElementProviderSimple provider)
	{
		// In Uno/WinUI, the provider usually holds a reference to the peer
		// or we cast the provider if it is implemented by the peer itself.
		if (provider == null) return null;

		// Property accessor (assuming extension or internal property exists)
		return provider.AutomationPeer;
	}

	// Corresponds to ProviderFromPeerImpl
	internal IRawElementProviderSimple ProviderFromPeerImpl(AutomationPeer pAutomationPeer)
	{
		return ProviderFromPeerStatic(pAutomationPeer);
	}

	// Corresponds to ProviderFromPeerStatic
	private static IRawElementProviderSimple ProviderFromPeerStatic(AutomationPeer pAutomationPeer)
	{
		if (pAutomationPeer == null) return null;

		// In C++, this creates a new DirectUI.IRawElementProviderSimple wrapper.
		// In C# / Uno, the AutomationPeer usually implements IRawElementProviderSimple directly.

		if (pAutomationPeer is IRawElementProviderSimple provider)
		{
			return provider;
		}

		// If a wrapper is strictly required by the architecture:
		// return new RawElementProviderSimpleWrapper(pAutomationPeer);

		return null;
	}

	// Removes the leading and trailing spaces in the provided string and returns the trimmed version
	// or an empty string when no characters are left.
	// Because it is recommended to set an AppBarButton, AppBarToggleButton, MenuFlyoutItem or ToggleMenuFlyoutItem's
	// KeyboardAcceleratorTextOverride to a single space to hide their keyboard accelerator UI, this trimming method
	// prevents automation tools like Narrator from emitting a space when navigating to such an element.
	private string GetTrimmedKeyboardAcceleratorTextOverrideStatic(string keyboardAcceleratorTextOverride)
	{
		if (string.IsNullOrEmpty(keyboardAcceleratorTextOverride))
		{
			return string.Empty;
		}

		// Trim only space characters (preserve other whitespace semantics from original implementation)
		var trimmedStart = keyboardAcceleratorTextOverride.TrimStart(' ');
		if (string.IsNullOrEmpty(trimmedStart))
		{
			return string.Empty;
		}

		var trimmed = trimmedStart.TrimEnd(' ');
		return trimmed;
	}

	//// Notify Owner to release AP as no UIA client is holding on to it.
	//private void NotifyNoUIAClientObjectToOwner()
	//{
	//	return S_OK;
	//}

	// Generate EventsSource for this AP, we only want to generate EventsSource for FrameworkElementAPs,
	// for others it's responsibility of APP author to set one during creation of object.
	private void GenerateAutomationPeerEventsSource(AutomationPeer pAPParent)
	{
		var pContainerItemAP = this as FrameworkElementAutomationPeer;

		if (pContainerItemAP is not null)
		{
			ItemsControlAutomationPeer.GenerateAutomationPeerEventsSourceStatic(pContainerItemAP, pAPParent);
		}
	}

	// Notify Corresponding core object about managed owner(UI) being dead.
	internal static void NotifyManagedUIElementIsDead(AutomationPeer pAutomationPeer)
	{
		if (pAutomationPeer != null)
		{
			// In Uno, this is handled via weak references or Dispose logic, 
			// but if an explicit notification is needed:
			// ((IInternalAutomationPeer)pAutomationPeer).NotifyManagedUIElementIsDead();
		}
	}

	private static void RaisePropertyChangedEventById(AutomationPeer peer, AutomationProperty propertyId, string oldValue, string newValue)
	{
		// In C# Uno, we can raise the event directly on the peer.
		// The C++ code boxed these into CValues and called CoreImports.

		if (peer != null)
		{
			peer.RaisePropertyChangedEvent(propertyId, oldValue, newValue);
		}
	}

	private static void RaisePropertyChangedEventById(AutomationPeer peer, AutomationProperty propertyId, bool oldValue, bool newValue)
	{
		if (peer != null)
		{
			peer.RaisePropertyChangedEvent(propertyId, oldValue, newValue);
		}
	}

	// Get the string value from the specified target AutomationPeer
	internal static void GetAutomationPeerStringValue(
	   DependencyObject nativeTarget,
	   AutomationProperty eProperty,
	   out string psText)
	{
		psText = string.Empty;

		if (nativeTarget == null) throw new ArgumentNullException(nameof(nativeTarget));

		// Retrieve the peer from the target object
		// In Uno, usually: var peer = FrameworkElementAutomationPeer.FromElement(nativeTarget as UIElement);
		// Or if nativeTarget is already the peer:
		var pTargetAsAutomationPeer = nativeTarget as AutomationPeer;

		if (pTargetAsAutomationPeer == null)
		{
			// Fallback logic to find peer if nativeTarget is the Element
			if (nativeTarget is UIElement uie)
			{
				pTargetAsAutomationPeer = FrameworkElementAutomationPeer.FromElement(uie);
			}
		}

		if (pTargetAsAutomationPeer == null) return;

		// Map AutomationProperty to specific getter methods
		if (eProperty == AutomationElementIdentifiers.AcceleratorKeyProperty)
		{
			psText = pTargetAsAutomationPeer.GetAcceleratorKey();
		}
		else if (eProperty == AutomationElementIdentifiers.AccessKeyProperty)
		{
			psText = pTargetAsAutomationPeer.GetAccessKey();
		}
		else if (eProperty == AutomationElementIdentifiers.AutomationIdProperty)
		{
			psText = pTargetAsAutomationPeer.GetAutomationId();
		}
		else if (eProperty == AutomationElementIdentifiers.ClassNameProperty)
		{
			psText = pTargetAsAutomationPeer.GetClassName();
		}
		else if (eProperty == AutomationElementIdentifiers.HelpTextProperty)
		{
			psText = pTargetAsAutomationPeer.GetHelpText();
		}
		else if (eProperty == AutomationElementIdentifiers.ItemStatusProperty)
		{
			psText = pTargetAsAutomationPeer.GetItemStatus();
		}
		else if (eProperty == AutomationElementIdentifiers.ItemTypeProperty)
		{
			psText = pTargetAsAutomationPeer.GetItemType();
		}
		else if (eProperty == AutomationElementIdentifiers.LocalizedControlTypeProperty)
		{
			psText = pTargetAsAutomationPeer.GetLocalizedControlType();
		}
		else if (eProperty == AutomationElementIdentifiers.NameProperty)
		{
			psText = pTargetAsAutomationPeer.GetName();
		}
		else if (eProperty == AutomationElementIdentifiers.LocalizedLandmarkTypeProperty)
		{
			psText = pTargetAsAutomationPeer.GetLocalizedLandmarkType();
		}
		else if (eProperty == AutomationElementIdentifiers.FullDescriptionProperty)
		{
			psText = pTargetAsAutomationPeer.GetFullDescription();
		}
		else
		{
			// Equivalent to MUX_ASSERT(false);
			System.Diagnostics.Debug.Assert(false, "Unknown string property requested");
		}
	}

	// Get the int value from the specified target AutomationPeer
	// Note: C++ returns bools as -1 (TRUE) or 0 (FALSE) often in UIA/COM context, or enums cast to int.
	internal static void GetAutomationPeerIntValue(
	   DependencyObject nativeTarget,
	   AutomationProperty eProperty,
	   out int pcReturnValue)
	{
		pcReturnValue = 0;
		bool bReturnValue = false;

		if (nativeTarget == null) throw new ArgumentNullException(nameof(nativeTarget));

		var pTargetAsAutomationPeer = nativeTarget as AutomationPeer;
		// (Add fallback lookup if nativeTarget is UIElement here if needed)

		if (pTargetAsAutomationPeer == null) return;

		if (eProperty == AutomationElementIdentifiers.ControlTypeProperty)
		{
			pcReturnValue = (int)pTargetAsAutomationPeer.GetAutomationControlType();
		}
		else if (eProperty == AutomationElementIdentifiers.OrientationProperty)
		{
			pcReturnValue = (int)pTargetAsAutomationPeer.GetOrientation();
		}
		else if (eProperty == AutomationElementIdentifiers.LiveSettingProperty)
		{
			pcReturnValue = (int)pTargetAsAutomationPeer.GetLiveSetting();
		}
		else if (eProperty == AutomationElementIdentifiers.HasKeyboardFocusProperty)
		{
			bReturnValue = pTargetAsAutomationPeer.HasKeyboardFocus();
			pcReturnValue = bReturnValue ? 1 : 0; // Or -1 depending on specific internal UIA requirement
		}
		else if (eProperty == AutomationElementIdentifiers.IsContentElementProperty)
		{
			bReturnValue = pTargetAsAutomationPeer.IsContentElement();
			pcReturnValue = bReturnValue ? 1 : 0;
		}
		else if (eProperty == AutomationElementIdentifiers.IsControlElementProperty)
		{
			bReturnValue = pTargetAsAutomationPeer.IsControlElement();
			pcReturnValue = bReturnValue ? 1 : 0;
		}
		else if (eProperty == AutomationElementIdentifiers.IsEnabledProperty)
		{
			bReturnValue = pTargetAsAutomationPeer.IsEnabled();
			pcReturnValue = bReturnValue ? 1 : 0;
		}
		else if (eProperty == AutomationElementIdentifiers.IsKeyboardFocusableProperty)
		{
			bReturnValue = pTargetAsAutomationPeer.IsKeyboardFocusable();
			pcReturnValue = bReturnValue ? 1 : 0;
		}
		else if (eProperty == AutomationElementIdentifiers.IsOffscreenProperty)
		{
			bReturnValue = pTargetAsAutomationPeer.IsOffscreen();
			pcReturnValue = bReturnValue ? 1 : 0;
		}
		else if (eProperty == AutomationElementIdentifiers.IsPasswordProperty)
		{
			bReturnValue = pTargetAsAutomationPeer.IsPassword();
			pcReturnValue = bReturnValue ? 1 : 0;
		}
		else if (eProperty == AutomationElementIdentifiers.IsRequiredForFormProperty)
		{
			bReturnValue = pTargetAsAutomationPeer.IsRequiredForForm();
			pcReturnValue = bReturnValue ? 1 : 0;
		}
		else if (eProperty == AutomationElementIdentifiers.PositionInSetProperty)
		{
			pcReturnValue = pTargetAsAutomationPeer.GetPositionInSet();
		}
		else if (eProperty == AutomationElementIdentifiers.CultureProperty)
		{
			pcReturnValue = pTargetAsAutomationPeer.GetCulture();
		}
		else if (eProperty == AutomationElementIdentifiers.SizeOfSetProperty)
		{
			pcReturnValue = pTargetAsAutomationPeer.GetSizeOfSet();
		}
		else if (eProperty == AutomationElementIdentifiers.LevelProperty)
		{
			pcReturnValue = pTargetAsAutomationPeer.GetLevel();
		}
		else if (eProperty == AutomationElementIdentifiers.LandmarkTypeProperty)
		{
			pcReturnValue = (int)pTargetAsAutomationPeer.GetLandmarkType();
		}
		else if (eProperty == AutomationElementIdentifiers.IsPeripheralProperty)
		{
			bReturnValue = pTargetAsAutomationPeer.IsPeripheral();
			pcReturnValue = bReturnValue ? 1 : 0;
		}
		else if (eProperty == AutomationElementIdentifiers.IsDataValidForFormProperty)
		{
			bReturnValue = pTargetAsAutomationPeer.IsDataValidForForm();
			pcReturnValue = bReturnValue ? 1 : 0;
		}
		else if (eProperty == AutomationElementIdentifiers.HeadingLevelProperty)
		{
			pcReturnValue = (int)pTargetAsAutomationPeer.GetHeadingLevel();
		}
		else if (eProperty == AutomationElementIdentifiers.IsDialogProperty)
		{
			bReturnValue = pTargetAsAutomationPeer.IsDialog();
			pcReturnValue = bReturnValue ? 1 : 0;
		}
		else
		{
			System.Diagnostics.Debug.Assert(false);
		}
	}

	// Get the point position of the specified AutomationPeer
	internal static void GetAutomationPeerPointValue(
	   DependencyObject nativeTarget,
	   AutomationProperty eProperty,
	   out Point pReturnPoint)
	{
		pReturnPoint = default;

		if (nativeTarget == null) throw new ArgumentNullException(nameof(nativeTarget));

		var pTargetAsAutomationPeer = nativeTarget as AutomationPeer;
		if (pTargetAsAutomationPeer != null)
		{
			pReturnPoint = pTargetAsAutomationPeer.GetClickablePoint();
		}
	}

	internal static void GetAutomationPeerRectValue(
	   DependencyObject nativeTarget,
	   AutomationProperty eProperty,
	   out Rect pReturnRect)
	{
		pReturnRect = default;

		if (nativeTarget == null) throw new ArgumentNullException(nameof(nativeTarget));

		var pTargetAsAutomationPeer = nativeTarget as AutomationPeer;
		if (pTargetAsAutomationPeer != null)
		{
			pReturnRect = pTargetAsAutomationPeer.GetBoundingRectangle();
		}
	}

	internal static void GetAutomationPeerAPValue(
	   DependencyObject nativeTarget,
	   AutomationProperty eProperty,
	   out AutomationPeer ppReturnAP)
	{
		ppReturnAP = null;

		if (nativeTarget == null) throw new ArgumentNullException(nameof(nativeTarget));

		var spTargetAsAutomationPeer = nativeTarget as AutomationPeer;
		if (spTargetAsAutomationPeer == null) return;

		if (eProperty == AutomationElementIdentifiers.LabeledByProperty)
		{
			ppReturnAP = spTargetAsAutomationPeer.GetLabeledBy();
		}
		else
		{
			throw new NotImplementedException();
		}

		// Note: C++ code handles AddRef here. C# GC handles references automatically.
	}

	internal static void GetAutomationPeerDOValue(
	   DependencyObject nativeTarget,
	   AutomationProperty eProperty,
	   out object ppReturnDO)
	{
		ppReturnDO = null;
		if (nativeTarget == null) throw new ArgumentNullException(nameof(nativeTarget));

		var spTargetAsAutomationPeer = nativeTarget as AutomationPeer;
		if (spTargetAsAutomationPeer == null) return;

		if (eProperty == AutomationElementIdentifiers.ControlledPeersProperty)
		{
			var spAutomationPeerVector = spTargetAsAutomationPeer.GetControlledPeers();

			if (spAutomationPeerVector != null && spAutomationPeerVector.Count > 0)
			{
				// C++ logic was marshalling to internal collection.
				// In C#, we can usually return the list directly, or wrap it if needed by the consumer.
				ppReturnDO = new List<AutomationPeer>(spAutomationPeerVector);
			}
		}
		else if (eProperty == AutomationElementIdentifiers.AnnotationsProperty)
		{
			var spAnnotationVector = spTargetAsAutomationPeer.GetAnnotations();

			if (spAnnotationVector != null && spAnnotationVector.Count > 0)
			{
				ppReturnDO = new List<AutomationPeerAnnotation>(spAnnotationVector);
			}
		}
		else if (eProperty == AutomationElementIdentifiers.DescribedByProperty ||
				 eProperty == AutomationElementIdentifiers.FlowsToProperty ||
				 eProperty == AutomationElementIdentifiers.FlowsFromProperty)
		{
			// This call maps to GetAutomationPeerDOValueFromIterable in the original code
			// ppReturnDO = GetAutomationPeerDOValueFromIterable(nativeTarget, eProperty);
		}
		else
		{
			System.Diagnostics.Debug.Assert(false, "Incorrect APAutomationProperties in GetAutomationPeerDOValue");
		}
	}

	internal static void CallAutomationPeerMethod(DependencyObject nativeTarget, int methodId)
	{
		if (nativeTarget == null) throw new ArgumentNullException(nameof(nativeTarget));

		var peer = nativeTarget as AutomationPeer ?? FrameworkElementAutomationPeer.FromElement(nativeTarget as UIElement);

		if (peer != null)
		{
			if (methodId == 0) // SetFocus
			{
				peer.SetFocus();
			}
			else if (methodId == 1) // ShowContextMenu
			{
				peer.ShowContextMenu();
			}
		}
	}

	// Corresponds to private void Navigate
	internal static void Navigate(
		DependencyObject nativeTarget,
		AutomationNavigationDirection direction,
		out object result)
	{
		result = null;
		if (nativeTarget == null) throw new ArgumentNullException(nameof(nativeTarget));

		var peer = nativeTarget as AutomationPeer ?? FrameworkElementAutomationPeer.FromElement(nativeTarget as UIElement);

		if (peer != null)
		{
			result = peer.Navigate(direction);

			// Note: In C++ logic exists to Unwrap/RetrieveNativeNode. 
			// In C#, 'result' is typically the AutomationPeer or IRawElementProviderSimple directly.
		}
	}

	// Corresponds to private void GetAutomationPeerChildren
	internal static void GetAutomationPeerChildren(
		DependencyObject nativeTarget,
		uint methodId, // 0 = Count, 1 = Children
		out IList<AutomationPeer> children)
	{
		children = null;
		if (nativeTarget == null) throw new ArgumentNullException(nameof(nativeTarget));

		var peer = nativeTarget as AutomationPeer ?? FrameworkElementAutomationPeer.FromElement(nativeTarget as UIElement);

		if (peer != null)
		{
			// In C# we just get the list. The C++ code had a two-pass system (get size, then get data) 
			// for memory management that isn't needed here.
			children = peer.GetChildren();
		}
	}

	// Corresponds to private void GetPattern
	internal static void GetPattern(
		DependencyObject nativeTarget,
		PatternInterface patternInterface,
		out object patternObject)
	{
		patternObject = null;
		if (nativeTarget == null) throw new ArgumentNullException(nameof(nativeTarget));

		var peer = nativeTarget as AutomationPeer ?? FrameworkElementAutomationPeer.FromElement(nativeTarget as UIElement);

		if (peer != null)
		{
			patternObject = peer.GetPattern(patternInterface);

			// C++ Logic: If null, try GetDefaultPattern (internal fallback for FrameworkElements)
			if (patternObject == null && peer is FrameworkElementAutomationPeer feap)
			{
				// Assuming GetDefaultPattern is accessible or mapped to standard GetPattern in Uno
				// patternObject = feap.GetDefaultPattern(patternInterface); 
			}
		}
	}

	// Corresponds to private void GetElementFromPoint
	internal static void GetElementFromPoint(
		DependencyObject nativeTarget,
		Point point,
		out object result)
	{
		result = null;
		if (nativeTarget == null) throw new ArgumentNullException(nameof(nativeTarget));

		var peer = nativeTarget as AutomationPeer ?? FrameworkElementAutomationPeer.FromElement(nativeTarget as UIElement);

		if (peer != null)
		{
			result = peer.GetElementFromPoint(point);
		}
	}

	// Corresponds to private void GetFocusedElement
	internal static void GetFocusedElement(
		DependencyObject nativeTarget,
		out object result)
	{
		result = null;
		if (nativeTarget == null) throw new ArgumentNullException(nameof(nativeTarget));

		var peer = nativeTarget as AutomationPeer ?? FrameworkElementAutomationPeer.FromElement(nativeTarget as UIElement);

		if (peer != null)
		{
			result = peer.GetFocusedElement();
		}
	}

	// Corresponds to private void UIATextRangeInvoke
	// This is a massive dispatcher for ITextRangeProvider
	internal static object UIATextRangeInvoke(
		DependencyObject nativeTarget,
		int functionId,
		object[] args)
	{
		if (nativeTarget == null) throw new ArgumentNullException(nameof(nativeTarget));

		// Resolve the provider. The C++ code unwraps CValues, here we assume we can get the object.
		// If nativeTarget is the Peer, we need to get the Pattern, or nativeTarget might BE the Pattern object.
		ITextRangeProvider textRangeProvider = nativeTarget as ITextRangeProvider;

		// If passed object isn't the provider, check if it's a peer that supports it?
		// (The C++ code assumes nativeTarget resolves to the provider directly via `UnwrapExternalObjectReference`)

		if (textRangeProvider == null) return null;

		switch (functionId)
		{
			case 0: // AddToSelection
				textRangeProvider.AddToSelection();
				return null;

			case 1: // Clone
				return textRangeProvider.Clone();

			case 2: // Compare
				{
					var other = args[0] as ITextRangeProvider;
					return textRangeProvider.Compare(other);
				}

			case 3: // CompareEndpoints
				{
					var endpoint = (TextPatternRangeEndpoint)Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture);
					var other = args[1] as ITextRangeProvider;
					var targetEndpoint = (TextPatternRangeEndpoint)Convert.ToInt32(args[2], System.Globalization.CultureInfo.InvariantCulture);
					return textRangeProvider.CompareEndpoints(endpoint, other, targetEndpoint);
				}

			case 4: // ExpandToEnclosingUnit
				{
					var unit = (TextUnit)Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture);
					textRangeProvider.ExpandToEnclosingUnit(unit);
					return null;
				}

			case 5: // FindAttribute
				{
					var attrId = (int)args[0]; // AutomationTextAttribute ID
					var val = args[1];
					var backward = Convert.ToBoolean(args[2], System.Globalization.CultureInfo.InvariantCulture);
					return textRangeProvider.FindAttribute(attrId, val, backward);
				}

			case 6: // FindText
				{
					var text = (string)args[0];
					var backward = Convert.ToBoolean(args[1], System.Globalization.CultureInfo.InvariantCulture);
					var ignoreCase = Convert.ToBoolean(args[2], System.Globalization.CultureInfo.InvariantCulture);
					return textRangeProvider.FindText(text, backward, ignoreCase);
				}

			case 7: // GetAttributeValue
				{
					var attrId = (int)args[0];
					object attrValue = textRangeProvider.GetAttributeValue(attrId);

					// Handling "UnsetValue" - Logic from C++:
					// "Mixed Attribute is returned as UnsetValue... Set it to type IUnknown with value null here"
					if (attrValue == DependencyProperty.UnsetValue || attrValue == null)
					{
						return null;
					}

					// Special marshalling for specific attributes
					var attributeEnum = (AutomationTextAttributesEnum)attrId;

					switch (attributeEnum)
					{
						case AutomationTextAttributesEnum.BackgroundColorAttribute:
						case AutomationTextAttributesEnum.ForegroundColorAttribute:
						case AutomationTextAttributesEnum.OverlineColorAttribute:
						case AutomationTextAttributesEnum.StrikethroughColorAttribute:
						case AutomationTextAttributesEnum.UnderlineColorAttribute:
							// C++ Logic: Converts Brush/Color to specific int format 0x00BBGGRR
							if (attrValue is SolidColorBrush brush)
							{
								var c = brush.Color;
								// ((pBrush.m_rgb & 0X00ff0000) >> 16) | (pBrush.m_rgb & 0X0000ff00) | ((pBrush.m_rgb & 0X000000ff) << 16)
								// This swaps Red and Blue (RGB -> BGR)
								int colorInt = (c.R) | (c.G << 8) | (c.B << 16);
								return colorInt;
							}
							else if (attrValue is int iVal)
							{
								return iVal;
							}
							return 0; // Fallback

						case AutomationTextAttributesEnum.TabsAttribute:
							// Convert IEnumerable to Array
							if (attrValue is IEnumerable<double> doubles)
							{
								return new List<double>(doubles).ToArray();
							}
							return null;

						case AutomationTextAttributesEnum.AnnotationTypesAttribute:
							if (attrValue is IEnumerable<int> ints)
							{
								return new List<int>(ints).ToArray();
							}
							return null;

						case AutomationTextAttributesEnum.AnnotationObjectsAttribute:
							// Return as list/collection
							if (attrValue is IEnumerable<AutomationPeer> peers)
							{
								return new List<AutomationPeer>(peers);
							}
							return null;

						default:
							return attrValue;
					}
				}

			case 8: // GetBoundingRectangles (Returns Count)
				{
					var rects = textRangeProvider.GetBoundingRectangles();
					return rects?.Length ?? 0;
				}

			case 9: // GetBoundingRectangles (Returns Array)
				{
					return textRangeProvider.GetBoundingRectangles();
				}

			case 10: // GetChildren (Returns Count)
				{
					var children = textRangeProvider.GetChildren();
					return children?.Length ?? 0;
				}

			case 11: // GetChildren (Returns Array of Provider Handles/Objects)
				{
					// In C#, we just return the IRawElementProviderSimple array
					return textRangeProvider.GetChildren();
				}

			case 12: // GetEnclosingElement
				return textRangeProvider.GetEnclosingElement();

			case 13: // GetText (Return Length limited)
				{
					var maxLength = (int)args[0];
					string text = textRangeProvider.GetText(maxLength);
					return text?.Length ?? 0;
				}

			case 14: // GetText (Return String)
				{
					var maxLength = (int)args[0];
					return textRangeProvider.GetText(maxLength);
				}

			case 15: // Move
				{
					var unit = (TextUnit)Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture);
					var count = (int)args[1];
					return textRangeProvider.Move(unit, count);
				}

			case 16: // MoveEndpointByRange
				{
					var endpoint = (TextPatternRangeEndpoint)Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture);
					var other = args[1] as ITextRangeProvider;
					var targetEndpoint = (TextPatternRangeEndpoint)Convert.ToInt32(args[2], System.Globalization.CultureInfo.InvariantCulture);
					textRangeProvider.MoveEndpointByRange(endpoint, other, targetEndpoint);
					return null;
				}

			case 17: // MoveEndpointByUnit
				{
					var endpoint = (TextPatternRangeEndpoint)Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture);
					var unit = (TextUnit)Convert.ToInt32(args[1], System.Globalization.CultureInfo.InvariantCulture);
					var count = (int)args[2];
					return textRangeProvider.MoveEndpointByUnit(endpoint, unit, count);
				}

			case 18: // RemoveFromSelection
				textRangeProvider.RemoveFromSelection();
				return null;

			case 19: // ScrollIntoView
				{
					var alignToTop = Convert.ToBoolean(args[0], System.Globalization.CultureInfo.InvariantCulture);
					textRangeProvider.ScrollIntoView(alignToTop);
					return null;
				}

			case 20: // Select
				textRangeProvider.Select();
				return null;

			case 21: // ShowContextMenu (ITextRangeProvider2)
				{
					var provider2 = textRangeProvider as ITextRangeProvider2;
					if (provider2 != null)
					{
						provider2.ShowContextMenu();
					}
					else
					{
						throw new NotSupportedException("Provider does not support ITextRangeProvider2");
					}
					return null;
				}

			case 22: // IsITextRangeProvider2
				return textRangeProvider is ITextRangeProvider2;

			default:
				throw new NotImplementedException($"FunctionId {functionId} not implemented.");
		}
	}
	internal static object UIAPatternInvoke(
		DependencyObject nativeTarget,
		PatternInterface ePatternInterface,
		int eFunction,
		object[] args)
	{
		if (nativeTarget == null) throw new ArgumentNullException(nameof(nativeTarget));

		// Resolve the Peer
		var peer = nativeTarget as AutomationPeer ?? FrameworkElementAutomationPeer.FromElement(nativeTarget as UIElement);
		if (peer == null) return null;

		// In C# Uno, the Peer usually implements the Provider interface directly.
		// We cast the peer to the specific interface based on ePatternInterface.

		switch (ePatternInterface)
		{
			case PatternInterface.Invoke:
				if (peer is IInvokeProvider invokeProvider)
				{
					if (eFunction == 0) invokeProvider.Invoke();
				}
				break;

			case PatternInterface.Dock:
				if (peer is IDockProvider dockProvider)
				{
					switch (eFunction)
					{
						case 0: return dockProvider.DockPosition;
						case 1: dockProvider.SetDockPosition((DockPosition)Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture)); break;
					}
				}
				break;

			case PatternInterface.ExpandCollapse:
				if (peer is IExpandCollapseProvider expandProvider)
				{
					switch (eFunction)
					{
						case 0: expandProvider.Collapse(); break;
						case 1: expandProvider.Expand(); break;
						case 2: return expandProvider.ExpandCollapseState;
					}
				}
				break;

			case PatternInterface.Value:
				if (peer is IValueProvider valueProvider)
				{
					switch (eFunction)
					{
						case 0: return valueProvider.IsReadOnly;
						case 1: valueProvider.SetValue(Convert.ToString(args[0], System.Globalization.CultureInfo.InvariantCulture)); break;
						case 2: // get_Value length
						case 3: // get_Value string
							return valueProvider.Value;
					}
				}
				break;

			case PatternInterface.GridItem:
				if (peer is IGridItemProvider gridItemProvider)
				{
					switch (eFunction)
					{
						case 0: return gridItemProvider.Column;
						case 1: return gridItemProvider.ColumnSpan;
						case 2: return gridItemProvider.ContainingGrid; // Returns IRawElementProviderSimple
						case 3: return gridItemProvider.Row;
						case 4: return gridItemProvider.RowSpan;
					}
				}
				break;

			case PatternInterface.Grid:
				if (peer is IGridProvider gridProvider)
				{
					switch (eFunction)
					{
						case 0: return gridProvider.ColumnCount;
						case 1:
							return gridProvider.GetItem(Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture),
							Convert.ToInt32(args[1], System.Globalization.CultureInfo.InvariantCulture));
						case 2: return gridProvider.RowCount;
					}
				}
				break;

			case PatternInterface.MultipleView:
				if (peer is IMultipleViewProvider mvProvider)
				{
					switch (eFunction)
					{
						case 0: return mvProvider.CurrentView;
						case 1: // GetSupportedViews length
						case 2: // GetSupportedViews array
							return mvProvider.GetSupportedViews();
						case 3: // GetViewName length
						case 4: // GetViewName string
							return mvProvider.GetViewName(Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture));
						case 5: mvProvider.SetCurrentView(Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture)); break;
					}
				}
				break;

			case PatternInterface.RangeValue:
				if (peer is IRangeValueProvider rangeProvider)
				{
					switch (eFunction)
					{
						case 0: return rangeProvider.IsReadOnly;
						case 1: return rangeProvider.LargeChange;
						case 2: return rangeProvider.Maximum;
						case 3: return rangeProvider.Minimum;
						case 4: rangeProvider.SetValue(Convert.ToDouble(args[0], System.Globalization.CultureInfo.InvariantCulture)); break;
						case 5: return rangeProvider.SmallChange;
						case 6: return rangeProvider.Value;
					}
				}
				break;

			case PatternInterface.ScrollItem:
				if (peer is IScrollItemProvider scrollItemProvider)
				{
					if (eFunction == 0) scrollItemProvider.ScrollIntoView();
				}
				break;

			case PatternInterface.Scroll:
				if (peer is IScrollProvider scrollProvider)
				{
					switch (eFunction)
					{
						case 0: return scrollProvider.HorizontallyScrollable;
						case 1: return scrollProvider.HorizontalScrollPercent;
						case 2: return scrollProvider.HorizontalViewSize;
						case 3:
							scrollProvider.Scroll(
								(ScrollAmount)Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture),
								(ScrollAmount)Convert.ToInt32(args[1], System.Globalization.CultureInfo.InvariantCulture));
							break;
						case 4:
							scrollProvider.SetScrollPercent(
								Convert.ToDouble(args[0], System.Globalization.CultureInfo.InvariantCulture),
								Convert.ToDouble(args[1], System.Globalization.CultureInfo.InvariantCulture));
							break;
						case 5: return scrollProvider.VerticallyScrollable;
						case 6: return scrollProvider.VerticalScrollPercent;
						case 7: return scrollProvider.VerticalViewSize;
					}
				}
				break;

			case PatternInterface.SelectionItem:
				if (peer is ISelectionItemProvider selectionItemProvider)
				{
					switch (eFunction)
					{
						case 0: selectionItemProvider.AddToSelection(); break;
						case 1: return selectionItemProvider.IsSelected;
						case 2: selectionItemProvider.RemoveFromSelection(); break;
						case 3: selectionItemProvider.Select(); break;
						case 4: return selectionItemProvider.SelectionContainer;
					}
				}
				break;

			case PatternInterface.Selection:
				if (peer is ISelectionProvider selectionProvider)
				{
					switch (eFunction)
					{
						case 0: return selectionProvider.CanSelectMultiple;
						case 1: // GetSelection Length
						case 2: // GetSelection Array
							return selectionProvider.GetSelection();
						case 3: return selectionProvider.IsSelectionRequired;
					}
				}
				break;

			case PatternInterface.TableItem:
				if (peer is ITableItemProvider tableItemProvider)
				{
					switch (eFunction)
					{
						case 0: // GetColumnHeaderItems Length
						case 1: // GetColumnHeaderItems Array
							return tableItemProvider.GetColumnHeaderItems();
						case 2: // GetRowHeaderItems Length
						case 3: // GetRowHeaderItems Array
							return tableItemProvider.GetRowHeaderItems();
					}
				}
				break;

			case PatternInterface.Table:
				if (peer is ITableProvider tableProvider)
				{
					switch (eFunction)
					{
						case 0: // GetColumnHeaders Length
						case 1: // GetColumnHeaders Array
							return tableProvider.GetColumnHeaders();
						case 2: // GetRowHeaders Length
						case 3: // GetRowHeaders Array
							return tableProvider.GetRowHeaders();
						case 4: return tableProvider.RowOrColumnMajor;
					}
				}
				break;

			case PatternInterface.Toggle:
				if (peer is IToggleProvider toggleProvider)
				{
					switch (eFunction)
					{
						case 0: toggleProvider.Toggle(); break;
						case 1: return toggleProvider.ToggleState;
					}
				}
				break;

			case PatternInterface.Transform:
				if (peer is ITransformProvider transformProvider)
				{
					switch (eFunction)
					{
						case 0: return transformProvider.CanMove;
						case 1: return transformProvider.CanResize;
						case 2: return transformProvider.CanRotate;
						case 3: transformProvider.Move(Convert.ToDouble(args[0], System.Globalization.CultureInfo.InvariantCulture)); break;
						case 4: transformProvider.Resize(Convert.ToDouble(args[0], System.Globalization.CultureInfo.InvariantCulture)); break;
						case 5: transformProvider.Rotate(Convert.ToDouble(args[0], System.Globalization.CultureInfo.InvariantCulture)); break;
					}
				}
				break;

			case PatternInterface.Transform2:
				if (peer is ITransformProvider2 transformProvider2)
				{
					switch (eFunction)
					{
						case 0: return transformProvider2.CanZoom;
						case 1: return transformProvider2.ZoomLevel;
						case 2: return transformProvider2.MaxZoom;
						case 3: return transformProvider2.MinZoom;
						case 4: transformProvider2.Zoom(Convert.ToDouble(args[0], System.Globalization.CultureInfo.InvariantCulture)); break;
						case 5: transformProvider2.ZoomByUnit((ZoomUnit)Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture)); break;
						case 6: return true; // IsTransformProvider2
					}
				}
				else if (eFunction == 6) return false;
				break;

			case PatternInterface.ItemContainer:
				if (peer is IItemContainerProvider itemContainerProvider)
				{
					// Args[0] = StartAfter (Provider), Args[1] = PropertyId, Args[2] = Value
					if (eFunction == 0)
					{
						var startAfter = args[0] as IRawElementProviderSimple;
						var propId = (int)args[1];
						var value = args[2];

						// Map integer ID to AutomationProperty object
						AutomationProperty ap = GetAutomationPropertyFromId(propId);

						return itemContainerProvider.FindItemByProperty(startAfter, ap, value);
					}
				}
				break;

			case PatternInterface.VirtualizedItem:
				if (peer is IVirtualizedItemProvider virtualizedItemProvider)
				{
					if (eFunction == 0) virtualizedItemProvider.Realize();
				}
				break;

			case PatternInterface.Window:
				if (peer is IWindowProvider windowProvider)
				{
					switch (eFunction)
					{
						case 0: windowProvider.Close(); break;
						case 1: return windowProvider.IsModal;
						case 2: return windowProvider.IsTopmost;
						case 3: return windowProvider.Maximizable;
						case 4: return windowProvider.Minimizable;
						case 5: windowProvider.SetVisualState((WindowVisualState)Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture)); break;
						case 6: return windowProvider.WaitForInputIdle(Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture));
						case 7: return windowProvider.InteractionState;
						case 8: return windowProvider.VisualState;
					}
				}
				break;

			case PatternInterface.Text:
				if (peer is ITextProvider textProvider)
				{
					switch (eFunction)
					{
						case 0: // GetSelection Length
						case 1: // GetSelection Array
							return textProvider.GetSelection();
						case 2: // GetVisibleRanges Length
						case 3: // GetVisibleRanges Array
							return textProvider.GetVisibleRanges();
						case 4: // RangeFromChild
							var childProvider = args[0] as IRawElementProviderSimple;
							return textProvider.RangeFromChild(childProvider);
						case 5: // RangeFromPoint
							var p = (Point)args[0];
							return textProvider.RangeFromPoint(p);
						case 6: // DocumentRange
							return textProvider.DocumentRange;
						case 7: // SupportedTextSelection
							return textProvider.SupportedTextSelection;
					}
				}
				break;

			case PatternInterface.Text2:
				if (peer is ITextProvider2 textProvider2)
				{
					switch (eFunction)
					{
						case 0: // RangeFromAnnotation
							var annotationProvider = args[0] as IRawElementProviderSimple;
							return textProvider2.RangeFromAnnotation(annotationProvider);
						case 1: // GetCaretRange
							return textProvider2.GetCaretRange(out bool isActive); // C# signature might differ, usually returns range
						case 2: return true; // IsITextProvider2
					}
				}
				else if (eFunction == 2) return false;
				break;

			case PatternInterface.TextChild:
				if (peer is ITextChildProvider textChildProvider)
				{
					switch (eFunction)
					{
						case 0: return textChildProvider.TextContainer;
						case 1: return textChildProvider.TextRange;
					}
				}
				break;

			case PatternInterface.Annotation:
				if (peer is IAnnotationProvider annotationProvider)
				{
					switch (eFunction)
					{
						case 0: return annotationProvider.AnnotationTypeId;
						case 1: // AnnotationTypeName length
						case 2: return annotationProvider.AnnotationTypeName;
						case 3: // Author length
						case 4: return annotationProvider.Author;
						case 5: // DateTime length
						case 6: return annotationProvider.DateTime;
						case 7: return annotationProvider.Target;
					}
				}
				break;

			case PatternInterface.Drag:
				if (peer is IDragProvider dragProvider)
				{
					switch (eFunction)
					{
						case 0: return dragProvider.IsGrabbed;
						case 1: return dragProvider.DropEffect;
						case 2: return dragProvider.DropEffects;
						case 3: return dragProvider.GetGrabbedItems();
					}
				}
				break;

			case PatternInterface.DropTarget:
				if (peer is IDropTargetProvider dropTargetProvider)
				{
					switch (eFunction)
					{
						case 0: return dropTargetProvider.DropEffect;
						case 1: return dropTargetProvider.DropEffects;
					}
				}
				break;

			case PatternInterface.ObjectModel:
				if (peer is IObjectModelProvider objectModelProvider)
				{
					if (eFunction == 0) return objectModelProvider.GetUnderlyingObjectModel();
				}
				break;

			case PatternInterface.Spreadsheet:
				if (peer is ISpreadsheetProvider spreadsheetProvider)
				{
					if (eFunction == 0) return spreadsheetProvider.GetItemByName(Convert.ToString(args[0], System.Globalization.CultureInfo.InvariantCulture));
				}
				break;

			case PatternInterface.SpreadsheetItem:
				if (peer is ISpreadsheetItemProvider spreadsheetItemProvider)
				{
					switch (eFunction)
					{
						case 0: return spreadsheetItemProvider.Formula;
						case 1: // GetAnnotationObjects length
						case 2: return spreadsheetItemProvider.GetAnnotationObjects();
						case 3: // GetAnnotationTypes length
						case 4: return spreadsheetItemProvider.GetAnnotationTypes();
					}
				}
				break;

			case PatternInterface.Styles:
				if (peer is IStylesProvider stylesProvider)
				{
					switch (eFunction)
					{
						case 0: return stylesProvider.ExtendedProperties;
						case 1: return stylesProvider.FillColor;
						case 2: return stylesProvider.FillPatternColor;
						case 3: return stylesProvider.FillPatternStyle;
						case 4: return stylesProvider.Shape;
						case 5: return stylesProvider.StyleId;
						case 6: return stylesProvider.StyleName;
					}
				}
				break;

			case PatternInterface.SynchronizedInput:
				if (peer is ISynchronizedInputProvider syncInputProvider)
				{
					switch (eFunction)
					{
						case 0: syncInputProvider.Cancel(); break;
						case 1: syncInputProvider.StartListening((SynchronizedInputType)Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture)); break;
					}
				}
				break;

			case PatternInterface.TextEdit:
				if (peer is ITextEditProvider textEditProvider)
				{
					switch (eFunction)
					{
						case 0: return textEditProvider.GetActiveComposition();
						case 1: return textEditProvider.GetConversionTarget();
					}
				}
				break;

			case PatternInterface.CustomNavigation:
				if (peer is ICustomNavigationProvider customNavProvider)
				{
					if (eFunction == 0)
					{
						return customNavProvider.NavigateCustom((AutomationNavigationDirection)Convert.ToInt32(args[0], System.Globalization.CultureInfo.InvariantCulture));
					}
				}
				break;

			default:
				// MUX_ASSERT(!"Unknown pattern interface.");
				break;
		}

		return null;
	}

	// Helper: GenerateRawElementProviderRuntimeId
	public static InternalRuntimeId GenerateRawElementProviderRuntimeId()
	{
		// In C++, IDXamlCore generated the ID. In Uno/C#, we usually just use GetHashCode or a static counter.
		// Returning a struct similar to UiaRuntimeId
		return new InternalRuntimeId { Part1 = 42, Part2 = _runtimeIdGenerator++ };
	}
	private static int _runtimeIdGenerator = 1;
	public struct InternalRuntimeId { public int Part1; public int Part2; }

	// Helper: ListenerExists
	internal static bool ListenerExistsHelper(AutomationEvents eventId)
	{
		return AutomationPeer.ListenerExists(eventId);
	}

	// Helper: RaiseEventIfListener
	internal static void RaiseEventIfListener(UIElement element, AutomationEvents eventId)
	{
		if (element != null && AutomationPeer.ListenerExists(eventId))
		{
			var peer = FrameworkElementAutomationPeer.FromElement(element);
			peer?.RaiseAutomationEvent(eventId);
		}
	}

	// Helper: GetAutomationPeerDOValueFromIterable (DescribedBy, FlowsTo, etc.)
	internal static void GetAutomationPeerDOValueFromIterable(
		DependencyObject nativeTarget,
		AutomationProperty eProperty,
		out IList<AutomationPeer> result)
	{
		result = null;
		if (nativeTarget == null) return;

		var peer = nativeTarget as AutomationPeer ?? FrameworkElementAutomationPeer.FromElement(nativeTarget as UIElement);
		if (peer == null) return;

		IEnumerable<AutomationPeer> iterable = null;

		if (eProperty == AutomationElementIdentifiers.DescribedByProperty)
		{
			iterable = peer.GetDescribedBy();
		}
		else if (eProperty == AutomationElementIdentifiers.FlowsToProperty)
		{
			iterable = peer.GetFlowsTo();
		}
		else if (eProperty == AutomationElementIdentifiers.FlowsFromProperty)
		{
			iterable = peer.GetFlowsFrom();
		}

		if (iterable != null)
		{
			result = iterable.ToList();
		}
	}

	// Helper: Map Internal Integers to AutomationProperty
	private static AutomationProperty GetAutomationPropertyFromId(int id)
	{
		// This maps C++ enum values to C# AutomationElementIdentifiers
		// Assuming mapping logic exists or IDs match known constants.
		// For basic SelectionItem.IsSelected:
		if (id == 30079) // UIA_SelectionItemIsSelectedPropertyId
		{
			return SelectionItemPatternIdentifiers.IsSelectedProperty;
		}

		// In a full implementation, a dictionary lookup of ID -> AutomationProperty is required here.
		return null;
	}

	// Helper: GenerateAutomationPeerEventsSource
	private static void GenerateAutomationPeerEventsSource(DependencyObject nativeTarget, DependencyObject nativeTargetParent)
	{
		var peer = nativeTarget as AutomationPeer ?? FrameworkElementAutomationPeer.FromElement(nativeTarget as UIElement);
		var parentPeer = nativeTargetParent as AutomationPeer ?? FrameworkElementAutomationPeer.FromElement(nativeTargetParent as UIElement);

		if (peer != null && parentPeer != null)
		{
			// In C++: peer.GenerateAutomationPeerEventsSource(parentPeer);
			// In C#, we usually just set the property directly.
			peer.EventsSource = parentPeer;
		}
	}
}

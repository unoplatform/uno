using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Provider;
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
	protected virtual object GetPatternCore(Microsoft.UI.Xaml.Automation.Peers.PatternInterface patternInterface) => null;

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

		switch (apType)
		{
			case AutomationControlType.Button:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_BUTTON");
				break;
			case AutomationControlType.Calendar:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_CALENDAR");
				break;
			case AutomationControlType.CheckBox:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_CHECKBOX");
				break;
			case AutomationControlType.ComboBox:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_COMBOBOX");
				break;
			case AutomationControlType.Edit:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_EDIT");
				break;
			case AutomationControlType.Hyperlink:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_HYPERLINK");
				break;
			case AutomationControlType.Image:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_IMAGE");
				break;
			case AutomationControlType.ListItem:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_LISTITEM");
				break;
			case AutomationControlType.List:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_LIST");
				break;
			case AutomationControlType.Menu:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_MENU");
				break;
			case AutomationControlType.MenuBar:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_MENUBAR");
				break;
			case AutomationControlType.MenuItem:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_MENUITEM");
				break;
			case AutomationControlType.ProgressBar:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_PROGRESSBAR");
				break;
			case AutomationControlType.RadioButton:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_RADIOBUTTON");
				break;
			case AutomationControlType.ScrollBar:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_SCROLLBAR");
				break;
			case AutomationControlType.Slider:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_SLIDER");
				break;
			case AutomationControlType.Spinner:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_SPINNER");
				break;
			case AutomationControlType.StatusBar:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_STATUSBAR");
				break;
			case AutomationControlType.Tab:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_TAB");
				break;
			case AutomationControlType.TabItem:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_TABITEM");
				break;
			case AutomationControlType.Text:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_TEXT");
				break;
			case AutomationControlType.ToolBar:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_TOOLBAR");
				break;
			case AutomationControlType.ToolTip:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_TOOLTIP");
				break;
			case AutomationControlType.Tree:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_TREE");
				break;
			case AutomationControlType.TreeItem:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_TREEITEM");
				break;
			case AutomationControlType.Custom:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_CUSTOM");
				break;
			case AutomationControlType.Group:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_GROUP");
				break;
			case AutomationControlType.Thumb:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_THUMB");
				break;
			case AutomationControlType.DataGrid:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_DATAGRID");
				break;
			case AutomationControlType.DataItem:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_DATAITEM");
				break;
			case AutomationControlType.Document:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_DOCUMENT");
				break;
			case AutomationControlType.SplitButton:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_SPLITBUTTON");
				break;
			case AutomationControlType.Window:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_WINDOW");
				break;
			case AutomationControlType.Pane:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_PANE");
				break;
			case AutomationControlType.Header:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_HEADER");
				break;
			case AutomationControlType.HeaderItem:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_HEADERITEM");
				break;
			case AutomationControlType.Table:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_TABLE");
				break;
			case AutomationControlType.TitleBar:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_TITLEBAR");
				break;
			case AutomationControlType.Separator:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_SEPARATOR");
				break;
			case AutomationControlType.SemanticZoom:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_SEMANTICZOOM");
				break;
			case AutomationControlType.AppBar:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_APPBAR");
				break;
			case AutomationControlType.FlipView:
				DXamlCore.GetCurrentNoCreate().GetLocalizedResourceString("UIA_AP_FLIPVIEW");
				break;
			default:
				throw new NotSupportedException("Unsupported AutomationControlType");
		}
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

	//DirectUI.AutomationPeer.get_EventsSource(out  IAutomationPeer** pValue)
	//{

	//	* pValue = m_tpEventsSource;
	//    if(* pValue)
	//    {
	//        AddRefInterface(*pValue);
	//    }

	//    RRETURN(S_OK);
	//}

	//DirectUI.AutomationPeer.EventsSource = IAutomationPeer * value
	//{
	//	DependencyObject* pCurrentAutomationPeer = null;
	//	DependencyObject* pEventsSourceAutomationPeer = null;

	//	m_tpEventsSource = value;
	//	pCurrentAutomationPeer = this.GetHandle();
	//	if (m_tpEventsSource)
	//	{
	//		pEventsSourceAutomationPeer = (AutomationPeer*)(value).GetHandle();
	//	}
	//	// Making sure that setting EventsSource null gets transferred to Core.
	//	(CAutomationPeer*)(pCurrentAutomationPeer).SetAPEventsSource((CAutomationPeer*)(pEventsSourceAutomationPeer));
	//	RRETURN(S_OK);
	//}

	//private void GetParentImpl(out IAutomationPeer** returnValue)
	//{
	//	DependencyObject spTarget;
	//	IAutomationPeer spTargetAsAutomationPeer;

	//	CAutomationPeer* pAutomationPeerCore = (CAutomationPeer*)(GetHandle()).GetAPParent();

	//	if (pAutomationPeerCore)
	//	{
	//		DXamlCore.GetCurrent().TryGetPeer(pAutomationPeerCore, &spTarget);
	//		if (spTarget)
	//		{
	//			spTarget.As(&spTargetAsAutomationPeer);
	//		}
	//	}

	//	*returnValue = spTargetAsAutomationPeer.Detach();

	//	return S_OK;
	//}

	//private void InvalidatePeerImpl()
	//{
	//	(CAutomationPeer*)(GetHandle()).InvalidatePeer();

	//	return S_OK;
	//}

	//private void RaiseAutomationEventImpl(
	//	 AutomationEvents eventId)
	//{
	//	IAutomationPeer spEventsSource;

	//	get_EventsSource(&spEventsSource);

	//	if (spEventsSource)
	//	{
	//		(CAutomationPeer*)(spEventsSource.Cast<AutomationPeer>().GetHandle()).RaiseAutomationEvent((UIAXcp.APAutomationEvents)eventId);
	//	}
	//	else
	//	{
	//		(CAutomationPeer*)(GetHandle()).RaiseAutomationEvent((UIAXcp.APAutomationEvents)eventId);
	//	}

	//	return S_OK;
	//}

	public void RaiseStructureChangedEvent(AutomationStructureChangeType structureChangeType, AutomationPeer child)
	{
		CValue newValue;
		CValue oldValue;
		UIAXcp.APAutomationProperties ePropertiesEnum;
		oldValue.SetNull();

		switch ((AutomationStructureChangeType)(structureChangeType))
		{
			case AutomationStructureChangeType.ChildAdded:
				{
					ePropertiesEnum = UIAXcp.APStructureChangeType_ChildAddedProperty;
					newValue.SetNull();
					break;
				}

			case AutomationStructureChangeType.ChildRemoved:
				{
					// UIAutomationCore expects runtime id of the removed child to be returned, henceforth it's required to be non-null.
					IFCPTR_RETURN(pChild);

					Xint runtimeId = (CAutomationPeer*)((AutomationPeer*)(pChild).GetHandle()).GetRuntimeId();
					newValue.WrapSignedArray(1, &runtimeId);
					ePropertiesEnum = UIAXcp.APStructureChangeType_ChildRemovedProperty;
					break;
				}

			case AutomationStructureChangeType.ChildrenBulkAdded:
				{
					ePropertiesEnum = UIAXcp.APStructureChangeType_ChildrenBulkAddedProperty;
					newValue.SetNull();
					break;
				}

			case AutomationStructureChangeType.ChildrenBulkRemoved:
				{
					ePropertiesEnum = UIAXcp.APStructureChangeType_ChildrenBulkRemovedProperty;
					newValue.SetNull();
					break;
				}

			case AutomationStructureChangeType.ChildrenInvalidated:
				{
					ePropertiesEnum = UIAXcp.APStructureChangeType_ChildrenInvalidatedProperty;
					newValue.SetNull();
					break;
				}

			case AutomationStructureChangeType.ChildrenReordered:
				{
					ePropertiesEnum = UIAXcp.APStructureChangeType_ChildernReorderedProperty;
					newValue.SetNull();
					break;
				}

			default:
				{
					E_UNEXPECTED;
				}
		}

		CoreImports.AutomationRaiseAutomationPropertyChanged((CAutomationPeer*)(GetHandle()), ePropertiesEnum, oldValue, newValue);
	}

	private void RaisePropertyChangedEventImpl(
		 xaml_automation.IAutomationProperty* pAutomationProperty,
		 object pPropertyValueOld,
		 object pPropertyValueNew)
	{


		CValue valueOld;
		CValue valueNew;
		BoxerBuffer oldBuffer;
		BoxerBuffer newBuffer;
		DependencyObject* pObjectForCoreOld = null;
		DependencyObject* pObjectForCoreNew = null;

		AutomationPropertiesEnum ePropertiesEnum;
		(AutomationProperty*)(pAutomationProperty).GetAutomationPropertiesEnum(&ePropertiesEnum);

		if (ePropertiesEnum != AutomationPropertiesEnum.ControlledPeersProperty)
		{
			CValueBoxer.BoxObjectValue(&valueOld, /* pSourceType */ null, pPropertyValueOld, &oldBuffer, &pObjectForCoreOld);
			CValueBoxer.BoxObjectValue(&valueNew, /* pSourceType */ null, pPropertyValueNew, &newBuffer, &pObjectForCoreNew);
		}
		else
		{
			// Ideally, we shall verify and marshall the oldValue(vectorView) and newValue(vectorView). But, in this case
			// considering Narrator doesn't care for old and new values for ControllerFor its okay to ignore those values.
			valueOld = CValue();
			valueNew = CValue();
		}

		RaisePropertyChangedEvent(pAutomationProperty, valueOld, valueNew);

	Cleanup:
		ctl.release_interface(pObjectForCoreOld);
		ctl.release_interface(pObjectForCoreNew);

		RRETURN(hr);
	}


	private void RaisePropertyChangedEvent(
		 xaml_automation.IAutomationProperty* pAutomationProperty,
		  CValue& oldValue,
		  CValue& newValue)
	{


		AutomationPropertiesEnum ePropertiesEnum;

		(AutomationProperty*)(pAutomationProperty).GetAutomationPropertiesEnum(&ePropertiesEnum);
		CoreImports.AutomationRaiseAutomationPropertyChanged((CAutomationPeer*)(GetHandle()), (UIAXcp.APAutomationProperties)ePropertiesEnum, oldValue, newValue);

	Cleanup:
		RRETURN(hr);
	}

	public void RaiseTextEditTextChangedEvent(AutomationTextEditChangeType automationTextEditChangeType, IReadOnlyList<string> changedData)
	{
		CValue cValue;
		object spChangedDataAsInspectable;
		IVectorView<string> spChangedData(pChangedData);
		spChangedData.As(&spChangedDataAsInspectable);

		IFCPTR_RETURN(spChangedDataAsInspectable);
		cValue.SetobjectAddRef(spChangedDataAsInspectable);

		switch (pAutomationProperty)
		{
			case AutomationTextEditChangeType.None:
				(CAutomationPeer*)(GetHandle()).RaiseTextEditTextChangedEvent(UIAXcp.AutomationTextEditChangeType.AutomationTextEditChangeType_None, &cValue);
				break;
			case AutomationTextEditChangeType.AutoCorrect:
				(CAutomationPeer*)(GetHandle()).RaiseTextEditTextChangedEvent(UIAXcp.AutomationTextEditChangeType.AutomationTextEditChangeType_AutoCorrect, &cValue);
				break;
			case AutomationTextEditChangeType.Composition:
				(CAutomationPeer*)(GetHandle()).RaiseTextEditTextChangedEvent(UIAXcp.AutomationTextEditChangeType.AutomationTextEditChangeType_Composition, &cValue);
				break;
			case xaml_automation.AutomationTextEditChangeType.AutomationTextEditChangeType_CompositionFinalized:
				(CAutomationPeer*)(GetHandle()).RaiseTextEditTextChangedEvent(UIAXcp.AutomationTextEditChangeType.AutomationTextEditChangeType_CompositionFinalized, &cValue);
				break;
		}
	}

	//private void RaiseNotificationEventImpl(
	//	AutomationNotificationKind notificationKind,
	//	AutomationNotificationProcessing notificationProcessing,
	//	 string displayString,
	//	 string activityId)
	//{
	//	xstring_ptr xDisplayString;
	//	xstring_ptr xActivityId;
	//	xstring_ptr.CloneRuntimeStringHandle(displayString, &xDisplayString);
	//	xstring_ptr.CloneRuntimeStringHandle(activityId, &xActivityId);

	//	(CAutomationPeer*)(GetHandle()).RaiseNotificationEvent(
	//		(UIAXcp.AutomationNotificationKind)(notificationKind),
	//		(UIAXcp.AutomationNotificationProcessing)(notificationProcessing),
	//		xDisplayString,
	//		xActivityId);

	//	return S_OK;
	//}

	protected virtual AutomationPeer GetPeerFromPointCore(Point point) => this;

	//// Custom APs can override GetElementFromPointCoreImpl to manage the hit-testing of APs completley
	//// by themselves. In addition to that they can also use it to return native UIA nodes as applicable
	//// which is why the return type is object instead of an IAutomationPeer*. The default
	//// implementation still uses GetPeerFromPoint for backward compatibility.
	//// This method deprecates GetPeerFromPoint/Core.
	//private void GetElementFromPointCoreImpl(
	//	 wf.Point point,
	//	out object* ppReturnValue)
	//{

	//	IAutomationPeer spAutomationPeerFromPoint;
	//	object spAPAsInspectable;

	//	*ppReturnValue = null;

	//	GetPeerFromPoint(point, &spAutomationPeerFromPoint);

	//	spAutomationPeerFromPoint.As(&spAPAsInspectable);
	//	*ppReturnValue = spAPAsInspectable.Detach();

	//Cleanup:
	//	RRETURN(hr);
	//}

	protected virtual object GetFocusedElementCore() => this;

	protected AutomationPeer PeerFromProvider(IRawElementProviderSimple provider) =>
		PeerFromProviderStatic(provider);

	private static void PeerFromProviderStatic(IRawElementProviderSimple provider) =>
		provider.AutomationPeer;

	//private void ProviderFromPeerImpl(
	//	 IAutomationPeer* pAutomationPeer,
	//	out xaml_automation.Provider.IIRawElementProviderSimple** ppReturnValue)
	//{


	//	ProviderFromPeerStatic(pAutomationPeer, ppReturnValue);

	//Cleanup:
	//	RRETURN(hr);
	//}

	//private void ProviderFromPeerStatic(
	//	  IAutomationPeer* pAutomationPeer,
	//	out xaml_automation.Provider.IIRawElementProviderSimple** ppReturnValue)
	//{


	//	IFCPTR(ppReturnValue);
	//	*ppReturnValue = null;

	//	if (pAutomationPeer)
	//	{
	//		ctl.ComObject<DirectUI.IRawElementProviderSimple>.CreateInstance(ppReturnValue);
	//		(DirectUI.IRawElementProviderSimple*)(*ppReturnValue).SetAutomationPeer(pAutomationPeer);
	//	}

	//Cleanup:
	//	RRETURN(hr);
	//}

	//// Removes the leading and trailing spaces in the provided string and returns the trimmed version
	//// or an empty string when no characters are left.
	//// Because it is recommended to set an AppBarButton, AppBarToggleButton, MenuFlyoutItem or ToggleMenuFlyoutItem's
	//// KeyboardAcceleratorTextOverride to a single space to hide their keyboard accelerator UI, this trimming method
	//// prevents automation tools like Narrator from emitting a space when navigating to such an element.
	//private void GetTrimmedKeyboardAcceleratorTextOverrideStatic(
	//	 string& keyboardAcceleratorTextOverride,
	//	out string* returnValue)
	//{
	//	// Return an empty string when the provided keyboardAcceleratorTextOverride is already empty.
	//	if (!WindowsIsStringEmpty(keyboardAcceleratorTextOverride))
	//	{
	//		stringReference strSpace(" ");
	//		string trimmedKeyboardAcceleratorTextOverride;

	//		IFCFAILFAST(WindowsTrimStringStart(keyboardAcceleratorTextOverride, strSpace, trimmedKeyboardAcceleratorTextOverride.GetAddressOf()));

	//		// Return an empty string when the remaining string is empty.
	//		if (!WindowsIsStringEmpty(trimmedKeyboardAcceleratorTextOverride))
	//		{
	//			// Trim the trailing spaces as well.
	//			IFCFAILFAST(WindowsTrimStringEnd(keyboardAcceleratorTextOverride, strSpace, trimmedKeyboardAcceleratorTextOverride.GetAddressOf()));
	//			IFCFAILFAST(trimmedKeyboardAcceleratorTextOverride.CopyTo(returnValue));
	//			return S_OK;
	//		}
	//	}

	//	// Return an empty string
	//	stringReference("").CopyTo(returnValue);
	//	return S_OK;
	//}

	//// Notify Owner to release AP as no UIA client is holding on to it.
	//private void NotifyNoUIAClientObjectToOwner()
	//{
	//	return S_OK;
	//}

	//// Generate EventsSource for this AP, we only want to generate EventsSource for FrameworkElementAPs,
	//// for others it's responsibility of APP author to set one during creation of object.
	//private void GenerateAutomationPeerEventsSource(IAutomationPeer* pAPParent)
	//{

	//	FrameworkElementAutomationPeer* pContainerItemAP = null;

	//	pContainerItemAP = ctl.query_interface<FrameworkElementAutomationPeer>(this);
	//	if (pContainerItemAP)
	//	{
	//		ItemsControlAutomationPeer.GenerateAutomationPeerEventsSourceStatic(pContainerItemAP, pAPParent);
	//	}

	//Cleanup:
	//	ReleaseInterface(pContainerItemAP);
	//	RRETURN(hr);
	//}

	//// Notify Corresponding core object about managed owner(UI) being dead.
	//void NotifyManagedUIElementIsDead()
	//{
	//	DependencyObject* pAutomationPeer = GetHandle();
	//	if (pAutomationPeer)
	//	{
	//		(CAutomationPeer*)(pAutomationPeer).NotifyManagedUIElementIsDead();
	//	}
	//}

	//private void RaisePropertyChangedEventById(UIAXcp.APAutomationProperties propertyId, string oldValue, string newValue)
	//{

	//	CValue cvalueOld;
	//	CValue cvalueNew;

	//	CValueBoxer.BoxValue(&cvalueOld, oldValue);
	//	CValueBoxer.BoxValue(&cvalueNew, newValue);
	//	CoreImports.AutomationRaiseAutomationPropertyChanged((CAutomationPeer*)(GetHandle()), propertyId, cvalueOld, cvalueNew);

	//Cleanup:
	//	RRETURN(hr);
	//}

	//private void RaisePropertyChangedEventById(UIAXcp.APAutomationProperties propertyId, bool oldValue, bool newValue)
	//{

	//	CValue cvalueOld;
	//	CValue cvalueNew;

	//	cvalueOld.SetBool(!!oldValue);
	//	cvalueNew.SetBool(!!newValue);
	//	CoreImports.AutomationRaiseAutomationPropertyChanged((CAutomationPeer*)(GetHandle()), propertyId, cvalueOld, cvalueNew);

	//Cleanup:
	//	RRETURN(hr);
	//}

	//// Get the string value from the specified target AutomationPeer
	//private void GetAutomationPeerStringValue(
	//	 DependencyObject* nativeTarget,
	//	 UIAXcp.APAutomationProperties eProperty,
	//	_Out_writes_z_(* pcText) char* psText,
	//	 Xint* pcText)
	//{

	//	DependencyObject* pTarget = null;
	//	IAutomationPeer* pTargetAsAutomationPeer = null;
	//	string strValue;
	//	string psTextTemp = null;
	//	uint pcTextTemp = 0;

	//	IFCPTR(nativeTarget);
	//	IFCPTR(psText);
	//	*psText = 0;

	//	DXamlCore.GetCurrent().GetPeer(nativeTarget, &pTarget);
	//	ctl.do_query_interface(pTargetAsAutomationPeer, pTarget);

	//	switch ((AutomationPropertiesEnum)(eProperty))
	//	{
	//		case AutomationPropertiesEnum.AcceleratorKeyProperty:
	//			pTargetAsAutomationPeer.GetAcceleratorKey(strValue.GetAddressOf());
	//			break;
	//		case AutomationPropertiesEnum.AccessKeyProperty:
	//			pTargetAsAutomationPeer.GetAccessKey(strValue.GetAddressOf());
	//			break;
	//		case AutomationPropertiesEnum.AutomationIdProperty:
	//			pTargetAsAutomationPeer.GetAutomationId(strValue.GetAddressOf());
	//			break;
	//		case AutomationPropertiesEnum.ClassNameProperty:
	//			pTargetAsAutomationPeer.GetClassName(strValue.GetAddressOf());
	//			break;
	//		case AutomationPropertiesEnum.HelpTextProperty:
	//			pTargetAsAutomationPeer.GetHelpText(strValue.GetAddressOf());
	//			break;
	//		case AutomationPropertiesEnum.ItemStatusProperty:
	//			pTargetAsAutomationPeer.GetItemStatus(strValue.GetAddressOf());
	//			break;
	//		case AutomationPropertiesEnum.ItemTypeProperty:
	//			pTargetAsAutomationPeer.GetItemType(strValue.GetAddressOf());
	//			break;
	//		case AutomationPropertiesEnum.LocalizedControlTypeProperty:
	//			pTargetAsAutomationPeer.GetLocalizedControlType(strValue.GetAddressOf());
	//			break;
	//		case AutomationPropertiesEnum.NameProperty:
	//			pTargetAsAutomationPeer.GetName(strValue.GetAddressOf());
	//			break;
	//		case AutomationPropertiesEnum.LocalizedLandmarkTypeProperty:
	//			{
	//				pTargetAsAutomationPeer.GetLocalizedLandmarkType(strValue.GetAddressOf());
	//			}
	//			break;
	//		case AutomationPropertiesEnum.FullDescriptionProperty:
	//			{
	//				pTargetAsAutomationPeer.GetFullDescription(strValue.GetAddressOf());
	//				break;
	//			}
	//		default:
	//			MUX_ASSERT(false);
	//			break;
	//	}

	//	psTextTemp = strValue.GetRawBuffer(&pcTextTemp);
	//	psTextTemp? hr : E_FAIL;
	//	if (pcTextTemp > (uint)(*pcText))
	//	{
	//		*pcText = (INT)(pcTextTemp);

	//		// NOTRACE because this API uses the common pattern where the caller first invokes
	//		// it with a default small buffer. If that's not large enough, this API fails and
	//		// the caller retries with a dynamically allocated large buffer. NOTRACE avoids
	//		// the error noise from the first try failing.
	//		// See CAutomationPeer.GetAutomationPeerStringValueFromManaged.
	//		IFC_NOTRACE(E_FAIL);
	//	}
	//	xstrncpy(psText, psTextTemp, pcTextTemp);
	//	*pcText = (INT)(pcTextTemp);

	//Cleanup:
	//	ctl.release_interface(pTarget);
	//	ReleaseInterface(pTargetAsAutomationPeer);
	//	RRETURN(hr);
	//}

	//// Get the int value from the specified target AutomationPeer
	//private void GetAutomationPeerIntValue(
	//	 DependencyObject* nativeTarget,
	//	 UIAXcp.APAutomationProperties eProperty,
	//	 Xint* pcReturnValue)
	//{

	//	DependencyObject* pTarget = null;
	//	IAutomationPeer* pTargetAsAutomationPeer = null;
	//	bool bReturnValue = false;

	//	IFCPTR(nativeTarget);

	//	DXamlCore.GetCurrent().GetPeer(nativeTarget, &pTarget);
	//	ctl.do_query_interface(pTargetAsAutomationPeer, pTarget);

	//	switch ((AutomationPropertiesEnum)(eProperty))
	//	{
	//		case AutomationPropertiesEnum.ControlTypeProperty:
	//			pTargetAsAutomationPeer.GetAutomationControlType((AutomationControlType*)pcReturnValue);
	//			break;
	//		case AutomationPropertiesEnum.OrientationProperty:
	//			pTargetAsAutomationPeer.GetOrientation((AutomationOrientation*)pcReturnValue);
	//			break;
	//		case AutomationPropertiesEnum.LiveSettingProperty:
	//			pTargetAsAutomationPeer.GetLiveSetting((AutomationLiveSetting*)pcReturnValue);
	//			break;
	//		case AutomationPropertiesEnum.HasKeyboardFocusProperty:
	//			pTargetAsAutomationPeer.HasKeyboardFocus(&bReturnValue);
	//			*pcReturnValue = bReturnValue ? -1 : 0;
	//			break;
	//		case AutomationPropertiesEnum.IsContentElementProperty:
	//			pTargetAsAutomationPeer.IsContentElement(&bReturnValue);
	//			*pcReturnValue = bReturnValue ? -1 : 0;
	//			break;
	//		case AutomationPropertiesEnum.IsControlElementProperty:
	//			pTargetAsAutomationPeer.IsControlElement(&bReturnValue);
	//			*pcReturnValue = bReturnValue ? -1 : 0;
	//			break;
	//		case AutomationPropertiesEnum.IsEnabledProperty:
	//			pTargetAsAutomationPeer.IsEnabled = &bReturnValue;
	//			*pcReturnValue = bReturnValue ? -1 : 0;
	//			break;
	//		case AutomationPropertiesEnum.IsKeyboardFocusableProperty:
	//			pTargetAsAutomationPeer.IsKeyboardFocusable(&bReturnValue);
	//			*pcReturnValue = bReturnValue ? -1 : 0;
	//			break;
	//		case AutomationPropertiesEnum.IsOffscreenProperty:
	//			pTargetAsAutomationPeer.IsOffscreen(&bReturnValue);
	//			*pcReturnValue = bReturnValue ? -1 : 0;
	//			break;
	//		case AutomationPropertiesEnum.IsPasswordProperty:
	//			pTargetAsAutomationPeer.IsPassword(&bReturnValue);
	//			*pcReturnValue = bReturnValue ? -1 : 0;
	//			break;
	//		case AutomationPropertiesEnum.IsRequiredForFormProperty:
	//			pTargetAsAutomationPeer.IsRequiredForForm(&bReturnValue);
	//			*pcReturnValue = bReturnValue ? -1 : 0;
	//			break;
	//		case AutomationPropertiesEnum.PositionInSetProperty:
	//			(AutomationPeer*)(pTargetAsAutomationPeer).GetPositionInSet(pcReturnValue);
	//			break;
	//		case AutomationPropertiesEnum.CultureProperty:
	//			(AutomationPeer*)(pTargetAsAutomationPeer).GetCulture(pcReturnValue);
	//			break;
	//		case AutomationPropertiesEnum.SizeOfSetProperty:
	//			(AutomationPeer*)(pTargetAsAutomationPeer).GetSizeOfSet(pcReturnValue);
	//			break;
	//		case AutomationPropertiesEnum.LevelProperty:
	//			(AutomationPeer*)(pTargetAsAutomationPeer).GetLevel(pcReturnValue);
	//			break;
	//		case AutomationPropertiesEnum.LandmarkTypeProperty:
	//			(AutomationPeer*)(pTargetAsAutomationPeer).GetLandmarkType((AutomationLandmarkType*)pcReturnValue);
	//			break;
	//		case AutomationPropertiesEnum.IsPeripheralProperty:
	//			{
	//				pTargetAsAutomationPeer.IsPeripheral(&bReturnValue);
	//				*pcReturnValue = bReturnValue ? -1 : 0;
	//				break;
	//			}
	//		case AutomationPropertiesEnum.IsDataValidForFormProperty:
	//			{
	//				pTargetAsAutomationPeer.IsDataValidForForm(&bReturnValue);
	//				*pcReturnValue = bReturnValue ? -1 : 0;
	//				break;
	//			}
	//		case AutomationPropertiesEnum.HeadingLevelProperty:
	//			(AutomationPeer*)(pTargetAsAutomationPeer).GetHeadingLevel((AutomationHeadingLevel*)pcReturnValue);
	//			break;
	//		case AutomationPropertiesEnum.IsDialogProperty:
	//			{
	//				pTargetAsAutomationPeer.IsDialog(&bReturnValue);
	//				*pcReturnValue = bReturnValue ? -1 : 0;
	//				break;
	//			}
	//		default:
	//			MUX_ASSERT(false);
	//			break;
	//	}

	//Cleanup:
	//	ctl.release_interface(pTarget);
	//	ReleaseInterface(pTargetAsAutomationPeer);

	//	RRETURN(hr);
	//}

	//// Get the point position of the specified AutomationPeer
	//private void GetAutomationPeerPointValue(
	//	 DependencyObject* nativeTarget,
	//	 UIAXcp.APAutomationProperties eProperty,
	//	out XPOINTF* pReturnPoint)
	//{

	//	DependencyObject* pTarget = null;
	//	IAutomationPeer* pTargetAsAutomationPeer = null;
	//	wf.Point pointValue = default;

	//	IFCPTR(nativeTarget);

	//	DXamlCore.GetCurrent().GetPeer(nativeTarget, &pTarget);
	//	ctl.do_query_interface(pTargetAsAutomationPeer, pTarget);

	//	pTargetAsAutomationPeer.GetClickablePoint(&pointValue);

	//	pReturnPoint.x = pointValue.X;
	//	pReturnPoint.y = pointValue.Y;

	//Cleanup:
	//	ctl.release_interface(pTarget);
	//	ReleaseInterface(pTargetAsAutomationPeer);

	//	RRETURN(hr);
	//}

	//private void GetAutomationPeerRectValue(
	//	 DependencyObject* nativeTarget,
	//	 UIAXcp.APAutomationProperties eProperty,
	//	out Rect* pReturnRect)
	//{

	//	DependencyObject* pTarget = null;
	//	IAutomationPeer* pTargetAsAutomationPeer = null;
	//	wf.Rect rectValue;

	//	IFCPTR(nativeTarget);

	//	DXamlCore.GetCurrent().GetPeer(nativeTarget, &pTarget);
	//	ctl.do_query_interface(pTargetAsAutomationPeer, pTarget);

	//	pTargetAsAutomationPeer.GetBoundingRectangle(&rectValue);

	//	pReturnRect.X = rectValue.X;
	//	pReturnRect.Y = rectValue.Y;
	//	pReturnRect.Width = rectValue.Width;
	//	pReturnRect.Height = rectValue.Height;

	//Cleanup:
	//	ctl.release_interface(pTarget);
	//	ReleaseInterface(pTargetAsAutomationPeer);

	//	RRETURN(hr);
	//}

	//private void GetAutomationPeerAPValue(
	//	 DependencyObject* nativeTarget,
	//	 UIAXcp.APAutomationProperties eProperty,
	//     .DependencyObject** ppReturnAP)
	//{

	//	DependencyObject spTarget;
	//	IAutomationPeer spTargetAsAutomationPeer;
	//	IAutomationPeer spReturnedAP;

	//	IFCPTR(nativeTarget);

	//	DXamlCore.GetCurrent().GetPeer(nativeTarget, &spTarget);
	//	spTarget.As(&spTargetAsAutomationPeer);

	//	switch (eProperty)
	//	{
	//		case UIAXcp.APLabeledByProperty:
	//			spTargetAsAutomationPeer.GetLabeledBy(&spReturnedAP);
	//			break;
	//		default:
	//			E_NOTIMPL;
	//	}

	//	if (spReturnedAP)
	//	{
	//		DependencyObject* pAutomationPeer = spReturnedAP.Cast<AutomationPeer>().GetHandle();
	//		*ppReturnAP = (CAutomationPeer*)(pAutomationPeer);
	//		CoreImports.DependencyObject_AddRef(pAutomationPeer);
	//	}

	//Cleanup:

	//	RRETURN(hr);
	//}

	//private void GetAutomationPeerDOValue(
	//	 DependencyObject* nativeTarget,
	//	 UIAXcp.APAutomationProperties eProperty,
	//	out  .DependencyObject** ppReturnDO)
	//{
	//	DependencyObject spTarget;
	//	IAutomationPeer spTargetAsAutomationPeer;

	//	unsigned size = 0;

	//	IFCPTR_RETURN(nativeTarget);
	//	IFCPTR_RETURN(ppReturnDO);
	//	*ppReturnDO = null;

	//	DXamlCore.GetCurrent().GetPeer(nativeTarget, &spTarget);
	//	spTarget.As(&spTargetAsAutomationPeer);

	//	switch (eProperty)
	//	{
	//		case UIAXcp.APControlledPeersProperty:
	//			{
	//				IVectorView<AutomationPeer*> spAutomationPeerVector;
	//				AutomationPeerCollection spColleciton;

	//				spTargetAsAutomationPeer.GetControlledPeers(&spAutomationPeerVector);

	//				if (spAutomationPeerVector)
	//				{
	//					size = spAutomationPeerVector.Size;
	//				}
	//				if (size == 0)
	//				{
	//					return S_OK;
	//				}

	//				// AGCore doesn't know about DXAML VectorViews. So, we have to marshal to a CAutomationPeerCollection.
	//				spColleciton = new();
	//				for (unsigned i = 0; i < size; ++i)
	//				{
	//					IAutomationPeer spPeer;
	//					spAutomationPeerVector.GetAt(i, &spPeer);
	//					spColleciton.Add(spPeer);
	//				}
	//				*ppReturnDO = spColleciton.GetHandle();
	//				CoreImports.DependencyObject_AddRef(*ppReturnDO);
	//				break;
	//			}

	//		case UIAXcp.APAnnotationsProperty:
	//			{
	//				IVector<AutomationPeerAnnotation*> spAnnotationVector;
	//				AutomationPeerAnnotationCollection spColleciton;

	//				spTargetAsAutomationPeer.GetAnnotations(&spAnnotationVector);

	//				if (spAnnotationVector)
	//				{
	//					size = spAnnotationVector.Size;
	//				}
	//				if (size == 0)
	//				{
	//					return S_OK;
	//				}

	//				// AGCore doesn't know about DXAML Vectors. So, we have to marshal to a CAutomationPeerAnnotationCollection.
	//				spColleciton = new();
	//				for (unsigned i = 0; i < size; ++i)
	//				{
	//					IAutomationPeerAnnotation spAnnotation;
	//					spAnnotationVector.GetAt(i, &spAnnotation);
	//					spColleciton.Add(spAnnotation);
	//				}
	//				*ppReturnDO = spColleciton.GetHandle();
	//				CoreImports.DependencyObject_AddRef(*ppReturnDO);
	//				break;
	//			}

	//		case UIAXcp.APDescribedByProperty:
	//		case UIAXcp.APFlowsToProperty:
	//		case UIAXcp.APFlowsFromProperty:
	//			GetAutomationPeerDOValueFromIterable(nativeTarget, eProperty, ppReturnDO);
	//			break;

	//		default:
	//			MUX_ASSERT(!"Incorrect APAutomationProperties in GetAutomationPeerDOValue");
	//			break;
	//	}

	//	return S_OK;
	//}

	//private void CallAutomationPeerMethod(
	//	 DependencyObject* nativeTarget,
	//	 Xint CallAutomationPeerMethod)
	//{

	//	DependencyObject spTarget;

	//	IFCPTR(nativeTarget);

	//	DXamlCore.GetCurrent().GetPeer(nativeTarget, &spTarget);

	//	if (CallAutomationPeerMethod == 0 /* SetFocus */)
	//	{
	//		IAutomationPeer spTargetAsAutomationPeer;
	//		spTarget.As(&spTargetAsAutomationPeer);
	//		spTargetAsAutomationPeer.SetFocus();
	//	}
	//	else if (CallAutomationPeerMethod == 1 /* ShowContextMenu */)
	//	{
	//		IAutomationPeer spTargetAsAutomationPeer;
	//		spTarget.As(&spTargetAsAutomationPeer);
	//		spTargetAsAutomationPeer.ShowContextMenu();
	//	}

	//Cleanup:
	//	RRETURN(hr);
	//}

	//private void Navigate(
	//	 DependencyObject* nativeTarget,
	//	 UIAXcp.AutomationNavigationDirection direction,
	//	out result_maybenull_.DependencyObject** ppReturnAPAsDO,
	//	out result_maybenull_ IUnknown** ppReturnIREPFAsUnk)
	//{


	//	DependencyObject spTarget;
	//	IAutomationPeer spTargetAsAutomationPeer;
	//	object spResult;

	//	*ppReturnAPAsDO = null;
	//	*ppReturnIREPFAsUnk = null;

	//	DXamlCore.GetCurrent().GetPeer(nativeTarget, &spTarget);
	//	spTarget.As(&spTargetAsAutomationPeer);

	//	spTargetAsAutomationPeer.Navigate((AutomationNavigationDirection)(direction), &spResult);

	//	// handle and verify if returned object is an AP or a native UIA node
	//	if (spResult)
	//	{
	//		RetrieveNativeNodeOrAPFromobject(spResult, ppReturnAPAsDO, ppReturnIREPFAsUnk);
	//	}

	//Cleanup:
	//	RRETURN(hr);
	//}

	//private void GetAutomationPeerChildren(
	//	 DependencyObject* nativeTarget,
	//	 uint CallAutomationPeerMethod,
	//	 Xint* pcReturnAPChildren,
	//	__deref_inout_ecount(* pcReturnAPChildren) .DependencyObject*** pppReturnAPChildren)
	//{


	//	DependencyObject* pTarget = null;
	//	IAutomationPeer* pTargetAsAutomationPeer = null;
	//	IVector<AutomationPeer*>* pChildren = null;

	//	IFCPTR(nativeTarget);
	//	IFCPTR(pcReturnAPChildren);
	//	IFCPTR(pppReturnAPChildren);

	//	DXamlCore.GetCurrent().GetPeer(nativeTarget, &pTarget);
	//	ctl.do_query_interface(pTargetAsAutomationPeer, pTarget);

	//	if (CallAutomationPeerMethod == 0 /* Count */ || CallAutomationPeerMethod == 1 /* Children */)
	//	{
	//		uint nChildrenCount = 0;

	//		pTargetAsAutomationPeer.GetChildren(&pChildren);
	//		if (pChildren != null)
	//		{
	//			nChildrenCount = pChildren.Size;
	//			if (CallAutomationPeerMethod == 1)
	//			{
	//				if ((uint)(*pcReturnAPChildren) < nChildrenCount)
	//				{
	//					E_FAIL;
	//				}

	//				if (CallAutomationPeerMethod == 1 /* Children */)
	//				{
	//					IAutomationPeer* pAP = null;

	//					for (uint nIndex = 0; nIndex < nChildrenCount; ++nIndex)
	//					{
	//						pChildren.GetAt(nIndex, &pAP);
	//						IFCPTR(pAP);

	//						*pppReturnAPChildren)[nIndex] = (AutomationPeer*)(pAP).GetHandle(;

	//					ReleaseInterface(pAP);
	//				}
	//			}
	//		}

	//		// In the case where CallAutomationPeerMethod==0 we need to return the required size.
	//		// OACR thinks we are blowing up the array, but we are not.
	//		_Analysis_assume_(nChildrenCount <= (uint)(*pcReturnAPChildren));
	//		*pcReturnAPChildren = (INT)(nChildrenCount);
	//	}
	//}

	//	else
	//{
	//	MUX_ASSERT(false);
	//}

	//Cleanup:
	//ctl.release_interface(pTarget);
	//ReleaseInterface(pTargetAsAutomationPeer);
	//ReleaseInterface(pChildren);
	//RRETURN(hr);
	//}

	//// Get the specified pattern on the target AutomationPeer
	//private void GetPattern(
	//	 DependencyObject* nativeTarget,
	//	out DependencyObject** nativeInterface,
	//	 UIAXcp.APPatternInterface eInterface)
	//{

	//	DependencyObject spTarget;
	//	IAutomationPeer spTargetAsAutomationPeer;
	//	FrameworkElementAutomationPeer spTargetAsFEAP;
	//	object spObject;
	//	DependencyObject spPatternObject;

	//	IFCPTR_RETURN(nativeInterface);
	//	*nativeInterface = null;

	//	DXamlCore.GetCurrent().GetPeer(nativeTarget, &spTarget);
	//	spTarget.As(&spTargetAsAutomationPeer);

	//	spTargetAsAutomationPeer.GetPattern((PatternInterface)eInterface, &spObject);

	//	if (spObject == null)
	//	{
	//		spTargetAsFEAP = spTarget.AsOrNull<FrameworkElementAutomationPeer>();
	//		if (spTargetAsFEAP != null)
	//		{
	//			spTargetAsFEAP.Cast<FrameworkElementAutomationPeer>().GetDefaultPattern((PatternInterface)eInterface, &spObject);
	//		}
	//	}

	//	if (spObject)
	//	{
	//		ExternalObjectReference.ConditionalWrap(spObject, &spPatternObject);
	//		*nativeInterface = spPatternObject.GetHandle();

	//		// Peg the object; the core is responsible for un-pegging.
	//		spPatternObject.PegNoRef();
	//	}

	//	return S_OK;
	//}

	//private void GetElementFromPoint(
	//	 DependencyObject* nativeTarget,
	//	  CValue& param,
	//	out result_maybenull_.DependencyObject** ppReturnAPAsDO,
	//	out result_maybenull_ IUnknown** ppReturnIREPFAsUnk)
	//{


	//	DependencyObject spTarget;
	//	IAutomationPeer spTargetAsAutomationPeer;
	//	object spResult;
	//	wf.Point screenPoint;

	//	*ppReturnAPAsDO = null;
	//	*ppReturnIREPFAsUnk = null;

	//	DXamlCore.GetCurrent().GetPeer(nativeTarget, &spTarget);
	//	spTarget.As(&spTargetAsAutomationPeer);

	//	// No need to type-cast, float is expected.
	//	screenPoint.X = param.AsPoint().x;
	//	screenPoint.Y = param.AsPoint().y;

	//	spTargetAsAutomationPeer.GetElementFromPoint(screenPoint, &spResult);

	//	// handle and verify if returned object is an AP or a native UIA node
	//	if (spResult)
	//	{
	//		RetrieveNativeNodeOrAPFromobject(spResult, ppReturnAPAsDO, ppReturnIREPFAsUnk);
	//	}

	//Cleanup:
	//	RRETURN(hr);
	//}

	//private void GetFocusedElement(
	//	 DependencyObject* nativeTarget,
	//	out result_maybenull_.DependencyObject** ppReturnAPAsDO,
	//	out result_maybenull_ IUnknown** ppReturnIREPFAsUnk)
	//{


	//	DependencyObject spTarget;
	//	IAutomationPeer spTargetAsAutomationPeer;
	//	object spResult;

	//	*ppReturnAPAsDO = null;
	//	*ppReturnIREPFAsUnk = null;

	//	DXamlCore.GetCurrent().GetPeer(nativeTarget, &spTarget);
	//	spTarget.As(&spTargetAsAutomationPeer);

	//	spTargetAsAutomationPeer.GetFocusedElement(&spResult);

	//	// handle and verify if returned object is an AP or a native UIA node
	//	if (spResult)
	//	{
	//		RetrieveNativeNodeOrAPFromobject(spResult, ppReturnAPAsDO, ppReturnIREPFAsUnk);
	//	}

	//Cleanup:
	//	RRETURN(hr);
	//}

	//// Invoke the specified pattern methods
	//private void UIATextRangeInvoke(
	//	 DependencyObject* nativeTarget,
	//	 Xint eFunction,
	//	 Xint cParams,
	//	 void* pvParams,
	//	out Automation.object pReturnVal)
	//{

	//	DependencyObject* pMOROrTarget = null;
	//	object pMOROrTargetAsobject = null;
	//	object pTargetAsobject = null;
	//	IAutomationPeer* pAP = null;
	//	object pAttributeValue = null;
	//	BoxerBuffer buffer;
	//	DependencyObject* pDO = null;
	//	Automation.object pValueParams = null;
	//	xaml_automation.Provider.ITextRangeProvider* pTextRangeProvider = null;
	//	xaml_automation.Provider.ITextRangeProvider2* pTextRangeProvider2 = null;
	//	xaml_automation.Provider.ITextRangeProvider* pTextRangeProviderOther = null;
	//	xaml_automation.Provider.IIRawElementProviderSimple** pChildren = null;
	//	xaml_automation.Provider.IIRawElementProviderSimple* pRawElementProvider = null;
	//	DependencyObject* pPatternObject = null;
	//	string strValue;
	//	string psTextTemp = null;
	//	uint pcTextTemp = 0;
	//	Xint iAttributeValue = -1;
	//	double* pDoubleArray = null;
	//	double* pDoubleCurrent = null;
	//	void** ppCurrentItem = null;
	//	uint nLength = 0;
	//	bool bRetVal = false;
	//	Xint iRetVal = -1;
	//	xaml_automation.Text.TextPatternRangeEndpoint rangeEndpoint;
	//	xaml_automation.Text.TextPatternRangeEndpoint targetRangeEndpoint;
	//	xaml_automation.Text.TextUnit textUnit;
	//	bool isBackward = false;
	//	bool isIgnoreCase = false;
	//	bool alignToTop = false;
	//	bool bIsUnsetValue = false;
	//	DXamlCore* pCore = DXamlCore.GetCurrent();
	//	INT enumValue = 0;
	//	CValue tempValue;
	//	CSolidColorBrush* pBrush = null;

	//	IFCPTR(nativeTarget);

	//	// Get the target AutomationPeer
	//	pCore.GetPeer(nativeTarget, &pMOROrTarget);
	//	ctl.do_query_interface(pMOROrTargetAsobject, pMOROrTarget);
	//	CValueBoxer.UnwrapExternalObjectReferenceIfPresent(pMOROrTargetAsobject, &pTargetAsobject);
	//	IFCPTR(pTargetAsobject);

	//	// Assign CValue parameters
	//	pValueParams = (Automation.object)pvParams;
	//	ctl.do_query_interface(pTextRangeProvider, pTargetAsobject);
	//	ctl.release_interface(pMOROrTarget);
	//	ReleaseInterface(pMOROrTargetAsobject);
	//	ReleaseInterface(pTargetAsobject);

	//	if (pTextRangeProvider)
	//	{
	//		// ITextRangeProvider inferface name is hard coded like as below and it's tracking with bug#32555
	//		switch (eFunction)
	//		{
	//			case 0: // AddToSelection
	//				pTextRangeProvider.AddToSelection();
	//				break;
	//			case 1: // Clone
	//				IFCPTR(pReturnVal);
	//				pTextRangeProvider.Clone(&pTextRangeProviderOther);
	//				if (pTextRangeProviderOther)
	//				{
	//					ExternalObjectReference.ConditionalWrap(pTextRangeProviderOther, &pPatternObject);
	//					pReturnVal.m_pdoValue = pPatternObject.GetHandle();

	//					// Peg the object; the core is responsible for un-pegging.
	//					pPatternObject.PegNoRef();
	//				}
	//				else
	//				{
	//					pReturnVal.m_pdoValue = null;
	//				}
	//				break;
	//			case 2: // Compare
	//				IFCPTR(pReturnVal);
	//				IFCPTR(pValueParams);
	//				pCore.GetPeer((&pValueParams[0]).m_pdoValue, &pMOROrTarget);
	//				ctl.do_query_interface(pMOROrTargetAsobject, pMOROrTarget);
	//				CValueBoxer.UnwrapExternalObjectReferenceIfPresent(pMOROrTargetAsobject, &pTargetAsobject);
	//				IFCPTR(pTargetAsobject);
	//				ctl.do_query_interface(pTextRangeProviderOther, pTargetAsobject);
	//				pTextRangeProvider.Compare(pTextRangeProviderOther, &bRetVal);
	//				pReturnVal.SetBool(bRetVal);
	//				break;
	//			case 3: // CompareEndpoints
	//				IFCPTR(pReturnVal);
	//				IFCPTR(pValueParams);
	//				rangeEndpoint = (xaml_automation.Text.TextPatternRangeEndpoint)(&pValueParams[0]).m_nValue;
	//				pCore.GetPeer((&pValueParams[1]).m_pdoValue, &pMOROrTarget);
	//				ctl.do_query_interface(pMOROrTargetAsobject, pMOROrTarget);
	//				CValueBoxer.UnwrapExternalObjectReferenceIfPresent(pMOROrTargetAsobject, &pTargetAsobject);
	//				IFCPTR(pTargetAsobject);
	//				ctl.do_query_interface(pTextRangeProviderOther, pTargetAsobject);
	//				targetRangeEndpoint = (xaml_automation.Text.TextPatternRangeEndpoint)(&pValueParams[2]).m_nValue;
	//				pTextRangeProvider.CompareEndpoints(rangeEndpoint, pTextRangeProviderOther, targetRangeEndpoint, &iRetVal);
	//				pReturnVal.SetSigned(iRetVal);
	//				break;
	//			case 4: // ExpandToEnclosingUnit
	//				IFCPTR(pValueParams);
	//				textUnit = (xaml_automation.Text.TextUnit)(&pValueParams[0]).m_nValue;
	//				pTextRangeProvider.ExpandToEnclosingUnit(textUnit);
	//				break;
	//			case 5: // FindAttribute
	//				IFCPTR(pReturnVal);
	//				IFCPTR(pValueParams);
	//				iAttributeValue = pValueParams[0].m_iValue;

	//				UnboxObjectValueHelper(&pValueParams[1], null, &pAttributeValue);
	//				isBackward = pValueParams[2].m_nValue != 0;

	//				pTextRangeProvider.FindAttribute(iAttributeValue, pAttributeValue, isBackward, &pTextRangeProviderOther);
	//				if (pTextRangeProviderOther)
	//				{
	//					ExternalObjectReference.ConditionalWrap(pTextRangeProviderOther, &pPatternObject);
	//					pReturnVal.SetObjectAddRef(pPatternObject.GetHandle());

	//					// Peg the object; the core is responsible for un-pegging.
	//					pPatternObject.PegNoRef();
	//				}
	//				else
	//				{
	//					pReturnVal.SetNull();
	//				}
	//				break;
	//			case 6: // FindText
	//				IFCPTR(pReturnVal);
	//				IFCPTR(pValueParams);
	//				GetStringFromCValue(&pValueParams[0], strValue.GetAddressOf());
	//				isBackward = pValueParams[1].m_nValue != 0;
	//				isIgnoreCase = pValueParams[2].m_nValue != 0;

	//				pTextRangeProvider.FindText(strValue, isBackward, isIgnoreCase, &pTextRangeProviderOther);
	//				if (pTextRangeProviderOther)
	//				{
	//					ExternalObjectReference.ConditionalWrap(pTextRangeProviderOther, &pPatternObject);
	//					pReturnVal.m_pdoValue = pPatternObject.GetHandle();

	//					// Peg the object; the core is responsible for un-pegging.
	//					pPatternObject.PegNoRef();
	//				}
	//				else
	//				{
	//					pReturnVal.m_pdoValue = null;
	//				}
	//				break;
	//			case 7: // GetAttributeValue
	//				IFCPTR(pReturnVal);
	//				IFCPTR(pValueParams);
	//				iAttributeValue = pValueParams[0].m_iValue;
	//				// Different attribute values needs to be boxed differently, also there is no way to differentiate Enum
	//				// via Inspectable as Inspectable loses Enum info so that needs to be handled here.
	//				pTextRangeProvider.GetAttributeValue(iAttributeValue, &pAttributeValue);
	//				if (pAttributeValue)
	//				{
	//					DependencyPropertyFactory.IsUnsetValue(pAttributeValue, bIsUnsetValue);
	//					// Mixed Attribute is returned as UnsetValue in TextRangeAdapter.GetAttributeValueImpl(),
	//					// Set it to type IUnknown with value null here, so it can be detected in CUIATextRangeProviderWrapper.GetAttributeValueImpl
	//					// and the real global mixed attribute can be returned to UIACore.
	//					if (bIsUnsetValue)
	//					{
	//						pReturnVal.SetIUnknownNoRef(null);
	//						break;
	//					}
	//					switch ((AutomationTextAttributesEnum)(iAttributeValue))
	//					{
	//						case AutomationTextAttributesEnum.AnimationStyleAttribute:
	//						case AutomationTextAttributesEnum.BulletStyleAttribute:
	//						case AutomationTextAttributesEnum.CapStyleAttribute:
	//						case AutomationTextAttributesEnum.CultureAttribute:
	//						case AutomationTextAttributesEnum.FontWeightAttribute:
	//						case AutomationTextAttributesEnum.HorizontalTextAlignmentAttribute:
	//						case AutomationTextAttributesEnum.OutlineStylesAttribute:
	//						case AutomationTextAttributesEnum.OverlineStyleAttribute:
	//						case AutomationTextAttributesEnum.StrikethroughStyleAttribute:
	//						case AutomationTextAttributesEnum.TextFlowDirectionsAttribute:
	//						case AutomationTextAttributesEnum.UnderlineStyleAttribute:
	//						case AutomationTextAttributesEnum.StyleIdAttribute:
	//						case AutomationTextAttributesEnum.SelectionActiveEndAttribute:
	//						case AutomationTextAttributesEnum.CaretPositionAttribute:
	//						case AutomationTextAttributesEnum.CaretBidiModeAttribute:
	//							ctl.do_get_value(enumValue, pAttributeValue);
	//							BoxEnumValueHelper(pReturnVal, (uint)(enumValue));
	//							break;
	//						case AutomationTextAttributesEnum.BackgroundColorAttribute:
	//						case AutomationTextAttributesEnum.ForegroundColorAttribute:
	//						case AutomationTextAttributesEnum.OverlineColorAttribute:
	//						case AutomationTextAttributesEnum.StrikethroughColorAttribute:
	//						case AutomationTextAttributesEnum.UnderlineColorAttribute:
	//							CValueBoxer.BoxObjectValue(&tempValue, null, pAttributeValue, &buffer, &pDO, true);
	//							if (tempValue.GetType() == valueSigned)
	//							{
	//								pReturnVal.SetEnum(tempValue.AsSigned());
	//							}
	//							else if (tempValue.GetType() == valueObject)
	//							{
	//								pBrush = do_pointer_cast<CSolidColorBrush>(tempValue.AsObject());
	//								if (pBrush != null)
	//								{
	//									pReturnVal.SetEnum(((pBrush.m_rgb & 0X00ff0000) >> 16) | (pBrush.m_rgb & 0X0000ff00) | ((pBrush.m_rgb & 0X000000ff) << 16));
	//								}
	//								else
	//								{
	//									pReturnVal.SetEnum(0);
	//								}
	//							}
	//							else if (tempValue.IsEnum())
	//							{
	//								pReturnVal.ConvertFrom(tempValue);
	//							}
	//							break;
	//						case AutomationTextAttributesEnum.LinkAttribute:
	//							{
	//								ctl.do_get_value(pTextRangeProviderOther, pAttributeValue);
	//								if (pTextRangeProviderOther)
	//								{
	//									ExternalObjectReference.ConditionalWrap(pTextRangeProviderOther, &pPatternObject);
	//									pReturnVal.SetObjectAddRef(pPatternObject.GetHandle());

	//									// Peg the object; the core is responsible for un-pegging.
	//									pPatternObject.PegNoRef();
	//								}
	//								else
	//								{
	//									pReturnVal.SetNull();
	//								}
	//							}
	//							break;
	//						case AutomationTextAttributesEnum.TabsAttribute:
	//							{
	//								unsigned size = 0;
	//								IVectorView<double> spDoubleVector;

	//								object spAttributeValue = pAttributeValue;
	//								spAttributeValue.As(&spDoubleVector);
	//								if (spDoubleVector)
	//								{
	//									size = spDoubleVector.Size;
	//								}

	//								if (size == 0)
	//								{
	//									pReturnVal.SetNull();
	//								}
	//								else
	//								{
	//									double* prgTabAttributes = new double[size];
	//									for (unsigned i = 0; i < size; i++)
	//									{
	//										double value = 0;
	//										spDoubleVector.GetAt(i, &value);
	//										prgTabAttributes[i] = value;
	//									}
	//									pReturnVal.SetDoubleArray(size, prgTabAttributes);
	//								}
	//							}
	//							break;
	//						case AutomationTextAttributesEnum.AnnotationTypesAttribute:
	//							{
	//								unsigned size = 0;
	//								IVectorView<Xint> spIntVector;

	//								object spAttributeValue = pAttributeValue;
	//								spAttributeValue.As(&spIntVector);
	//								if (spIntVector)
	//								{
	//									size = spIntVector.Size;
	//								}

	//								if (size == 0)
	//								{
	//									pReturnVal.SetNull();
	//								}
	//								else
	//								{
	//									Xint* prgTabs = new Xint[size];
	//									for (unsigned i = 0; i < size; i++)
	//									{
	//										Xint value = 0;
	//										spIntVector.GetAt(i, &value);
	//										prgTabs[i] = value;
	//									}
	//									pReturnVal.SetSignedArray(size, prgTabs);
	//								}
	//							}
	//							break;
	//						case AutomationTextAttributesEnum.AnnotationObjectsAttribute:
	//							{
	//								IVectorView<AutomationPeer*> spAutomationPeerVector;
	//								AutomationPeerCollection spCollection;
	//								unsigned size = 0;
	//								DependencyObject* pCDO = null;

	//								object spAttributeValue = pAttributeValue;
	//								spAttributeValue.As(&spAutomationPeerVector);

	//								if (spAutomationPeerVector)
	//								{
	//									size = spAutomationPeerVector.Size;
	//								}

	//								if (size == 0)
	//								{
	//									pReturnVal.SetIUnknownNoRef(null);
	//								}
	//								else
	//								{
	//									// AGCore doesn't know about DXAML VectorViews. So, we have to marshal to a CAutomationPeerCollection.
	//									spCollection = new();
	//									for (unsigned i = 0; i < size; ++i)
	//									{
	//										IAutomationPeer spPeer;
	//										spAutomationPeerVector.GetAt(i, &spPeer);
	//										spCollection.Add(spPeer);
	//									}
	//									pCDO = spCollection.GetHandle();
	//									AddRefInterface(pCDO);
	//									pReturnVal.SetPointer(pCDO);
	//								}
	//							}
	//							break;

	//						case AutomationTextAttributesEnum.FontNameAttribute:
	//						case AutomationTextAttributesEnum.StyleNameAttribute:
	//						case AutomationTextAttributesEnum.FontSizeAttribute:
	//						case AutomationTextAttributesEnum.IndentationFirstLineAttribute:
	//						case AutomationTextAttributesEnum.IndentationLeadingAttribute:
	//						case AutomationTextAttributesEnum.IndentationTrailingAttribute:
	//						case AutomationTextAttributesEnum.IsHiddenAttribute:
	//						case AutomationTextAttributesEnum.IsItalicAttribute:
	//						case AutomationTextAttributesEnum.IsReadOnlyAttribute:
	//						case AutomationTextAttributesEnum.MarginBottomAttribute:
	//						case AutomationTextAttributesEnum.MarginLeadingAttribute:
	//						case AutomationTextAttributesEnum.MarginTopAttribute:
	//						case AutomationTextAttributesEnum.MarginTrailingAttribute:
	//						case AutomationTextAttributesEnum.IsSubscriptAttribute:
	//						case AutomationTextAttributesEnum.IsSuperscriptAttribute:
	//						case AutomationTextAttributesEnum.IsActiveAttribute:
	//						default:
	//							BoxObjectValueHelper(pReturnVal, null, pAttributeValue, &buffer, &pDO, true);
	//							break;
	//					}
	//				}
	//				break;
	//			case 8: // GetBoundingRectangles
	//				IFCPTR(pReturnVal);
	//				pTextRangeProvider.GetBoundingRectangles(&nLength, &pDoubleArray);
	//				pReturnVal.SetSigned(nLength);
	//				break;
	//			case 9: // GetBoundingRectangles
	//				IFCPTR(pReturnVal);
	//				pTextRangeProvider.GetBoundingRectangles(&nLength, &pDoubleArray);
	//				nLength = MIN(pReturnVal.GetArrayElementCount(), nLength);
	//				pReturnVal.SetArrayElementCount(nLength);
	//				pDoubleCurrent = (double*)pReturnVal.m_pvValue;
	//				for (uint i = 0; i < nLength; i++)
	//				{
	//					// We use the Min above to ensure that the number of elements we process is within the range of both
	//					// arrays.  The logic is a little too tricky for PREfast.  Here we let PREfast know 'i' is in range.
	//					_Analysis_assume_(i == 0);
	//					pDoubleCurrent[i] = pDoubleArray[i];
	//				}
	//				break;
	//			case 10: // GetChildren
	//				IFCPTR(pReturnVal);
	//				pTextRangeProvider.GetChildren(&nLength, &pChildren);
	//				pReturnVal.SetSigned(nLength);
	//				break;
	//			case 11: // GetChildren
	//				IFCPTR(pReturnVal);
	//				pTextRangeProvider.GetChildren(&nLength, &pChildren);
	//				pReturnVal.SetArrayElementCount(MIN(pReturnVal.GetArrayElementCount(), nLength));
	//				ppCurrentItem = (void**)pReturnVal.m_pvValue;
	//				for (uint i = 0; i < pReturnVal.GetArrayElementCount(); i++)
	//				{
	//					_Analysis_assume_(i == 0);
	//					(IRawElementProviderSimple*)(pChildren[i]).GetAutomationPeer(&pAP);
	//					if (pAP)
	//					{
	//						// We use the Min above to ensure that the number of elements we process is within the range of both
	//						// arrays.  The logic is a little too tricky for PREfast.  Here we let PREfast know 'i' is in range.
	//						ppCurrentItem[i] = (AutomationPeer*)(pAP).GetHandle();
	//					}
	//					ReleaseInterface(pAP);
	//				}
	//				break;
	//			case 12: // GetEnclosingElement
	//				IFCPTR(pReturnVal);
	//				pTextRangeProvider.GetEnclosingElement(&pRawElementProvider);
	//				(IRawElementProviderSimple*)(pRawElementProvider).GetAutomationPeer(&pAP);
	//				if (pAP)
	//				{
	//					pReturnVal.m_pdoValue = (AutomationPeer*)(pAP).GetHandle();
	//				}
	//				else
	//				{
	//					pReturnVal.m_pdoValue = null;
	//				}
	//				break;
	//			case 13: // GetText
	//				IFCPTR(pReturnVal);
	//				IFCPTR(pValueParams);
	//				// -1 for the length of Text is a supported value, hence we have to include it.
	//				// When length asked for is -1, expected result is whole Text.
	//				if (pValueParams[0].m_iValue < -1)
	//				{
	//					ErrorHelper.OriginateError(AgError(UIA_GETTEXT_OUTOFRANGE_LENGTH));
	//				}
	//				pTextRangeProvider.GetText(pValueParams[0].m_iValue, strValue.GetAddressOf());
	//				psTextTemp = strValue.GetRawBuffer(&pcTextTemp);
	//				if (pValueParams[0].m_iValue != -1)
	//				{
	//					pcTextTemp = MIN((uint)(pValueParams[0].m_iValue), pcTextTemp);
	//				}
	//				pReturnVal.SetSigned(pcTextTemp);
	//				break;
	//			case 14: // GetText
	//				IFCPTR(pReturnVal);
	//				IFCPTR(pValueParams);
	//				pTextRangeProvider.GetText(pValueParams[0].m_iValue, strValue.GetAddressOf());
	//				(SetCValueFromString(pReturnVal, strValue))

	//				break;
	//			case 15: // Move
	//				IFCPTR(pReturnVal);
	//				IFCPTR(pValueParams);
	//				textUnit = (xaml_automation.Text.TextUnit)(&pValueParams[0]).m_nValue;
	//				pTextRangeProvider.Move(textUnit, pValueParams[1].m_iValue, &iRetVal);
	//				pReturnVal.SetSigned(iRetVal);
	//				break;
	//			case 16: // MoveEndpointByRange
	//				IFCPTR(pValueParams);
	//				rangeEndpoint = (xaml_automation.Text.TextPatternRangeEndpoint)(&pValueParams[0]).m_nValue;
	//				pCore.GetPeer((&pValueParams[1]).m_pdoValue, &pMOROrTarget);
	//				ctl.do_query_interface(pMOROrTargetAsobject, pMOROrTarget);
	//				CValueBoxer.UnwrapExternalObjectReferenceIfPresent(pMOROrTargetAsobject, &pTargetAsobject);
	//				IFCPTR(pTargetAsobject);
	//				ctl.do_query_interface(pTextRangeProviderOther, pTargetAsobject);
	//				targetRangeEndpoint = (xaml_automation.Text.TextPatternRangeEndpoint)(&pValueParams[2]).m_nValue;
	//				pTextRangeProvider.MoveEndpointByRange(rangeEndpoint, pTextRangeProviderOther, targetRangeEndpoint);
	//				break;
	//			case 17: // MoveEndpointByUnit
	//				IFCPTR(pReturnVal);
	//				IFCPTR(pValueParams);
	//				rangeEndpoint = (xaml_automation.Text.TextPatternRangeEndpoint)(&pValueParams[0]).m_nValue;
	//				textUnit = (xaml_automation.Text.TextUnit)(&pValueParams[1]).m_nValue;
	//				pTextRangeProvider.MoveEndpointByUnit(rangeEndpoint, textUnit, (&pValueParams[2]).m_iValue /*count*/, &iRetVal);
	//				pReturnVal.SetSigned(iRetVal);
	//				break;
	//			case 18: // RemoveFromSelection
	//				pTextRangeProvider.RemoveFromSelection();
	//				break;
	//			case 19: // ScrollIntoView
	//				IFCPTR(pValueParams);
	//				alignToTop = (&pValueParams[0]).m_nValue != 0;
	//				pTextRangeProvider.ScrollIntoView(alignToTop);
	//				break;
	//			case 20: // Select
	//				pTextRangeProvider.Select();
	//				break;
	//			case 21: // ShowContextMenu
	//				pTextRangeProvider2 = ctl.query_interface<xaml_automation.Provider.ITextRangeProvider2>(pTextRangeProvider);
	//				if (pTextRangeProvider2)
	//				{
	//					pTextRangeProvider2.ShowContextMenu();
	//				}
	//				else
	//				{
	//					E_FAIL;
	//				}
	//				break;
	//			case 22: // IsITextRangeProvider2
	//				pTextRangeProvider2 = ctl.query_interface<xaml_automation.Provider.ITextRangeProvider2>(pTextRangeProvider);
	//				if (pTextRangeProvider2)
	//				{
	//					BoxValueHelper(pReturnVal, true);
	//				}
	//				else
	//				{
	//					BoxValueHelper(pReturnVal, false);
	//				}
	//				break;
	//			default:
	//				E_FAIL;
	//		}
	//	}

	//Cleanup:
	//	ctl.release_interface(pMOROrTarget);
	//	ctl.release_interface(pPatternObject);
	//	ReleaseInterface(pMOROrTargetAsobject);
	//	ReleaseInterface(pTargetAsobject);
	//	ReleaseInterface(pAP);
	//	ReleaseInterface(pTextRangeProvider);
	//	ReleaseInterface(pTextRangeProvider2);
	//	ReleaseInterface(pTextRangeProviderOther);
	//	ReleaseInterface(pRawElementProvider);
	//	ReleaseInterface(pAttributeValue);
	//	if (pChildren)
	//	{
	//		for (uint i = 0; i < nLength; i++)
	//		{
	//			ReleaseInterface(pChildren[i]);
	//		}
	//	}
	//	DELETE_ARRAY(pChildren);
	//	DELETE_ARRAY(pDoubleArray);
	//	ctl.release_interface(pDO);
	//	RRETURN(hr);
	//}

	//// Invoke the specified pattern methods
	//private void UIAPatternInvoke(
	//	 DependencyObject* nativeTarget,
	//	 UIAXcp.APPatternInterface ePatternInterface,
	//	 Xint eFunction,
	//	 Xint cParams,
	//	 void* pvParams,
	//	out Automation.object pReturnVal)
	//{

	//	DependencyObject* pMOROrTarget = null;
	//	object pMOROrTargetAsobject = null;
	//	object pTargetAsobject = null;
	//	Automation.object pValueParams = null;
	//	string strValue;
	//	string* phsArray = null;
	//	INT iValue = 0;
	//	double dValue = 0.0;
	//	bool bValue = false;
	//	string psTextTemp = null;
	//	uint pcTextTemp = 0;
	//	uint nLength = 0;
	//	INT* pIntArray = null;
	//	wu.Color color;

	//	void** ppCurrentItem = null;
	//	IAutomationPeer* pAP = null;
	//	xaml_automation.Provider.IIRawElementProviderSimple* pProvider = null;
	//	xaml_automation.Provider.IInvokeProvider* pInvokeProvider = null;
	//	xaml_automation.Provider.IDockProvider* pDockProvider = null;
	//	xaml_automation.Provider.IExpandCollapseProvider* pExpandCollapseProvider = null;
	//	xaml_automation.Provider.IValueProvider* pValueProvider = null;
	//	xaml_automation.Provider.IGridItemProvider* pGridItemProvider = null;
	//	xaml_automation.Provider.IGridProvider* pGridProvider = null;
	//	xaml_automation.Provider.IMultipleViewProvider* pMultipleViewProvider = null;
	//	xaml_automation.Provider.IRangeValueProvider* pRangeValueProvider = null;
	//	xaml_automation.Provider.IScrollItemProvider* pScrollItemProvider = null;
	//	xaml_automation.Provider.IScrollProvider* pScrollProvider = null;
	//	xaml_automation.Provider.ISelectionItemProvider* pSelectionItemProvider = null;
	//	xaml_automation.Provider.ISelectionProvider* pSelectionProvider = null;
	//	xaml_automation.Provider.ITableItemProvider* pTableItemProvider = null;
	//	xaml_automation.Provider.ITableProvider* pTableProvider = null;
	//	xaml_automation.Provider.IToggleProvider* pToggleProvider = null;
	//	xaml_automation.Provider.ITransformProvider* pTransformProvider = null;
	//	xaml_automation.Provider.ITransformProvider2* pTransformProvider2 = null;
	//	xaml_automation.Provider.IVirtualizedItemProvider* pVirtualizedItemProvider = null;
	//	xaml_automation.Provider.IItemContainerProvider* pItemContainerProvider = null;
	//	xaml_automation.Provider.IWindowProvider* pWindowProvider = null;
	//	xaml_automation.Provider.ITextProvider* pTextProvider = null;
	//	xaml_automation.Provider.ITextProvider2* pTextProvider2 = null;
	//	xaml_automation.Provider.ITextChildProvider* pTextChildProvider = null;
	//	xaml_automation.Provider.IAnnotationProvider* pAnnotationProvider = null;
	//	xaml_automation.Provider.ITextRangeProvider* pTextRangeProvider = null;
	//	xaml_automation.Provider.IIRawElementProviderSimple* pRawElementProvider = null;
	//	xaml_automation.Provider.IIRawElementProviderSimple** pSelections = null;
	//	xaml_automation.Provider.IIRawElementProviderSimple** pTableItems = null;
	//	xaml_automation.Provider.IIRawElementProviderSimple** pTableHeaders = null;
	//	xaml_automation.Provider.IIRawElementProviderSimple** pAnnotationObjects = null;
	//	xaml_automation.Provider.ITextRangeProvider** pTextRangeSelections = null;
	//	xaml_automation.Provider.IDragProvider* pDragProvider = null;
	//	xaml_automation.Provider.IDropTargetProvider* pDropTargetProvider = null;
	//	xaml_automation.Provider.IObjectModelProvider* pObjectModelProvider = null;
	//	xaml_automation.Provider.ISpreadsheetProvider* pSpreadsheetProvider = null;
	//	xaml_automation.Provider.ISpreadsheetItemProvider* pSpreadsheetItemProvider = null;
	//	xaml_automation.Provider.IStylesProvider* pStylesProvider = null;
	//	xaml_automation.Provider.ISynchronizedInputProvider* pSynchronizedInputProvider = null;
	//	xaml_automation.Provider.ITextEditProvider spTextEditProvider = null;
	//	xaml_automation.Provider.ICustomNavigationProvider spCustomNavigationProvider = null;

	//	// Some functions return a reference to an array that should not be deleted.
	//	xaml_automation.Provider.IIRawElementProviderSimple** pNoDeleteRepsArray = null;
	//	xaml_automation.AnnotationType* pAnnotationType = null;
	//	UIAXcp.AnnotationType* pCurrentAnnotationType = null;

	//	DependencyObject* pStartAfter = null;
	//	DependencyObject* pChildDO = null;
	//	DependencyObject* pAnnotationDO = null;
	//	DependencyObject* pPatternObject = null;
	//	IAutomationPeer* pStartAfterAsAutomationPeer = null;
	//	xaml_automation.Provider.IIRawElementProviderSimple* pStartAfterAsRaw = null;
	//	xaml_automation.IAutomationProperty* pAutomationProperty = null;
	//	object propertyValueAsInspectable = null;
	//	object objetModelInspectable = null;
	//	IUnknown* objetModelIUnknown = null;
	//	XPOINTF* pPoint = null;
	//	wf.Point screenPoint;
	//	xaml_automation.SupportedTextSelection supportedTextSelection;
	//	AutomationNavigationDirection navigationDirection;
	//	DXamlCore* pCore = DXamlCore.GetCurrent();

	//	// Get the target AutomationPeer
	//	pCore.GetPeer(nativeTarget, &pMOROrTarget);
	//	ctl.do_query_interface(pMOROrTargetAsobject, pMOROrTarget);
	//	CValueBoxer.UnwrapExternalObjectReferenceIfPresent(pMOROrTargetAsobject, &pTargetAsobject);
	//	IFCPTR(pTargetAsobject);

	//	// Assign CValue parameters
	//	pValueParams = (Automation.object)pvParams;

	//	switch (ePatternInterface)
	//	{
	//		case PatternInterface.Invoke:
	//			ctl.do_query_interface(pInvokeProvider, pTargetAsobject);
	//			if (pInvokeProvider)
	//			{
	//				if (eFunction == 0)
	//				{
	//					pInvokeProvider.Invoke();
	//				}
	//			}
	//			break;

	//		case PatternInterface.Dock:
	//			ctl.do_query_interface(pDockProvider, pTargetAsobject);
	//			if (pDockProvider)
	//			{
	//				xaml_automation.DockPosition eDockPosition;
	//				switch (eFunction)
	//				{
	//					case 0:
	//						eDockPosition = pDockProvider.DockPosition;
	//						BoxEnumValueHelper(pReturnVal, eDockPosition);
	//						break;
	//					case 1:
	//						UnboxEnumValueHelper(&pValueParams[0], null, (uint*)&eDockPosition);
	//						pDockProvider.SetDockPosition(eDockPosition);
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.ExpandCollapse:
	//			ctl.do_query_interface(pExpandCollapseProvider, pTargetAsobject);
	//			if (pExpandCollapseProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						pExpandCollapseProvider.Collapse();
	//						break;
	//					case 1:
	//						pExpandCollapseProvider.Expand();
	//						break;
	//					case 2:
	//						xaml_automation.ExpandCollapseState eExpandCollapseState;
	//						eExpandCollapseState = pExpandCollapseProvider.ExpandCollapseState;
	//						BoxEnumValueHelper(pReturnVal, eExpandCollapseState);
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.Value:
	//			ctl.do_query_interface(pValueProvider, pTargetAsobject);
	//			if (pValueProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						bValue = pValueProvider.IsReadOnly;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 1:
	//						GetStringFromCValue(&pValueParams[0], strValue.GetAddressOf());
	//						pValueProvider.SetValue(strValue);
	//						break;
	//					case 2:
	//						pValueProvider.get_Value(strValue.GetAddressOf());
	//						psTextTemp = strValue.GetRawBuffer(&pcTextTemp);
	//						BoxValueHelper(pReturnVal, (INT)pcTextTemp);
	//						break;
	//					case 3:
	//						pValueProvider.get_Value(strValue.GetAddressOf());
	//						SetCValueFromString(pReturnVal, strValue);
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.GridItem:
	//			ctl.do_query_interface(pGridItemProvider, pTargetAsobject);
	//			if (pGridItemProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						iValue = pGridItemProvider.Column;
	//						BoxValueHelper(pReturnVal, iValue);
	//						break;
	//					case 1:
	//						iValue = pGridItemProvider.ColumnSpan;
	//						BoxValueHelper(pReturnVal, iValue);
	//						break;
	//					case 2:
	//						pProvider = pGridItemProvider.ContainingGrid;
	//						if (pProvider)
	//						{
	//							((IRawElementProviderSimple*)(pProvider)).GetAutomationPeer(&pAP);
	//							if (pAP)
	//							{
	//								pReturnVal.m_pdoValue = (AutomationPeer*)(pAP).GetHandle();
	//							}
	//						}
	//						break;
	//					case 3:
	//						iValue = pGridItemProvider.Row;
	//						BoxValueHelper(pReturnVal, iValue);
	//						break;
	//					case 4:
	//						iValue = pGridItemProvider.RowSpan;
	//						BoxValueHelper(pReturnVal, iValue);
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.Grid:
	//			ctl.do_query_interface(pGridProvider, pTargetAsobject);
	//			if (pGridProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						iValue = pGridProvider.ColumnCount;
	//						BoxValueHelper(pReturnVal, iValue);
	//						break;
	//					case 1:
	//						pGridProvider.GetItem((&pValueParams[0]).m_iValue, (&pValueParams[1]).m_iValue, &pProvider);
	//						if (pProvider)
	//						{
	//							((IRawElementProviderSimple*)(pProvider)).GetAutomationPeer(&pAP);

	//							if (pAP)
	//							{
	//								pReturnVal.m_pdoValue = (AutomationPeer*)(pAP).GetHandle();
	//							}
	//						}
	//						break;
	//					case 2:
	//						iValue = pGridProvider.RowCount;
	//						BoxValueHelper(pReturnVal, iValue);
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.MultipleView:
	//			ctl.do_query_interface(pMultipleViewProvider, pTargetAsobject);
	//			if (pMultipleViewProvider)
	//			{
	//				INT* pIntCurrent = null;
	//				switch (eFunction)
	//				{
	//					case 0:
	//						iValue = pMultipleViewProvider.CurrentView;
	//						BoxValueHelper(pReturnVal, iValue);
	//						break;
	//					case 1:
	//						pMultipleViewProvider.GetSupportedViews(&nLength, &pIntArray);
	//						BoxValueHelper(pReturnVal, (INT)nLength);
	//						break;
	//					case 2:
	//						pMultipleViewProvider.GetSupportedViews(&nLength, &pIntArray);
	//						nLength = MIN(pReturnVal.GetArrayElementCount(), nLength);
	//						pReturnVal.SetArrayElementCount(nLength);
	//						pIntCurrent = (INT*)pReturnVal.m_pvValue;
	//						for (uint i = 0; i < nLength; i++)
	//						{
	//							// We use the Min above to ensure that the number of elements we process is within the range of both
	//							// arrays.  The logic is a little too tricky for PREfast.  Here we let PREfast know 'i' is in range.
	//							_Analysis_assume_(i == 0);
	//							pIntCurrent[i] = pIntArray[i];
	//						}
	//						break;
	//					case 3:
	//						pMultipleViewProvider.GetViewName((&pValueParams[0]).m_iValue, strValue.GetAddressOf());
	//						psTextTemp = strValue.GetRawBuffer(&pcTextTemp);
	//						BoxValueHelper(pReturnVal, (INT)pcTextTemp);
	//						break;
	//					case 4:
	//						pMultipleViewProvider.GetViewName((&pValueParams[0]).m_iValue, strValue.GetAddressOf());
	//						SetCValueFromString(pReturnVal, strValue);
	//						break;
	//					case 5:
	//						pMultipleViewProvider.SetCurrentView((&pValueParams[0]).m_iValue);
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.RangeValue:
	//			ctl.do_query_interface(pRangeValueProvider, pTargetAsobject);
	//			if (pRangeValueProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						bValue = pRangeValueProvider.IsReadOnly;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 1:
	//						dValue = pRangeValueProvider.LargeChange;
	//						BoxValueHelper(pReturnVal, dValue);
	//						break;
	//					case 2:
	//						dValue = pRangeValueProvider.Maximum;
	//						BoxValueHelper(pReturnVal, dValue);
	//						break;
	//					case 3:
	//						dValue = pRangeValueProvider.Minimum;
	//						BoxValueHelper(pReturnVal, dValue);
	//						break;
	//					case 4:
	//						pRangeValueProvider.SetValue((&pValueParams[0]).m_eValue);
	//						break;
	//					case 5:
	//						dValue = pRangeValueProvider.SmallChange;
	//						BoxValueHelper(pReturnVal, dValue);
	//						break;
	//					case 6:
	//						dValue = pRangeValueProvider.Value;
	//						BoxValueHelper(pReturnVal, dValue);
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.ScrollItem:
	//			ctl.do_query_interface(pScrollItemProvider, pTargetAsobject);
	//			if (pScrollItemProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						pScrollItemProvider.ScrollIntoView();
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.Scroll:
	//			ctl.do_query_interface(pScrollProvider, pTargetAsobject);
	//			if (pScrollProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						bValue = pScrollProvider.HorizontallyScrollable;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 1:
	//						dValue = pScrollProvider.HorizontalScrollPercent;
	//						BoxValueHelper(pReturnVal, dValue);
	//						break;
	//					case 2:
	//						dValue = pScrollProvider.HorizontalViewSize;
	//						BoxValueHelper(pReturnVal, dValue);
	//						break;
	//					case 3:
	//						pScrollProvider.Scroll((xaml_automation.ScrollAmount)(&pValueParams[0]).m_nValue, (xaml_automation.ScrollAmount)(&pValueParams[1]).m_nValue);
	//						break;
	//					case 4:
	//						pScrollProvider.SetScrollPercent((&pValueParams[0]).m_eValue, (&pValueParams[1]).m_eValue);
	//						break;
	//					case 5:
	//						bValue = pScrollProvider.VerticallyScrollable;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 6:
	//						dValue = pScrollProvider.VerticalScrollPercent;
	//						BoxValueHelper(pReturnVal, dValue);
	//						break;
	//					case 7:
	//						dValue = pScrollProvider.VerticalViewSize;
	//						BoxValueHelper(pReturnVal, dValue);
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.SelectionItem:
	//			ctl.do_query_interface(pSelectionItemProvider, pTargetAsobject);
	//			if (pSelectionItemProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						pSelectionItemProvider.AddToSelection();
	//						break;
	//					case 1:
	//						bValue = pSelectionItemProvider.IsSelected;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 2:
	//						pSelectionItemProvider.RemoveFromSelection();
	//						break;
	//					case 3:
	//						pSelectionItemProvider.Select();
	//						break;
	//					case 4:
	//						pProvider = pSelectionItemProvider.SelectionContainer;
	//						if (pProvider)
	//						{
	//							((IRawElementProviderSimple*)(pProvider)).GetAutomationPeer(&pAP);
	//							if (pAP)
	//							{
	//								pReturnVal.m_pdoValue = (AutomationPeer*)(pAP).GetHandle();
	//							}
	//						}
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.Selection:
	//			ctl.do_query_interface(pSelectionProvider, pTargetAsobject);
	//			if (pSelectionProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						bValue = pSelectionProvider.CanSelectMultiple;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 1: // The first method is to get the length of buffer then second call is for copying buffer
	//						pSelectionProvider.GetSelection(&nLength, &pSelections);
	//						pReturnVal.SetSigned(nLength);
	//						break;
	//					case 2:
	//						pSelectionProvider.GetSelection(&nLength, &pSelections);
	//						pReturnVal.SetArrayElementCount(MIN(pReturnVal.GetArrayElementCount(), nLength));
	//						ppCurrentItem = (void**)pReturnVal.m_pvValue;
	//						for (uint i = 0; i < pReturnVal.GetArrayElementCount(); i++)
	//						{
	//							_Analysis_assume_(i == 0);
	//							if (pSelections[i])
	//							{
	//								(IRawElementProviderSimple*)(pSelections[i]).GetAutomationPeer(&pAP);
	//								if (pAP)
	//								{
	//									ppCurrentItem[i] = (AutomationPeer*)(pAP).GetHandle();
	//								}
	//							}
	//							ReleaseInterface(pAP);
	//						}
	//						break;
	//					case 3:
	//						bValue = pSelectionProvider.IsSelectionRequired;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.TableItem:
	//			ctl.do_query_interface(pTableItemProvider, pTargetAsobject);
	//			if (pTableItemProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0: // The first method is to get the length of buffer then second call is for copying buffer
	//						pTableItemProvider.GetColumnHeaderItems(&nLength, &pTableItems);
	//						pReturnVal.SetSigned(nLength);
	//						break;
	//					case 1:
	//						pTableItemProvider.GetColumnHeaderItems(&nLength, &pTableItems);
	//						pReturnVal.SetArrayElementCount(MIN(pReturnVal.GetArrayElementCount(), nLength));
	//						ppCurrentItem = (void**)pReturnVal.m_pvValue;
	//						for (uint i = 0; i < pReturnVal.GetArrayElementCount(); i++)
	//						{
	//							_Analysis_assume_(i == 0);
	//							if (pTableItems[i])
	//							{
	//								(IRawElementProviderSimple*)(pTableItems[i]).GetAutomationPeer(&pAP);
	//								if (pAP)
	//								{
	//									ppCurrentItem[i] = (AutomationPeer*)(pAP).GetHandle();
	//								}
	//							}
	//							ReleaseInterface(pAP);
	//						}
	//						break;
	//					case 2: // The first method is to get the length of buffer then second call is for copying buffer
	//						pTableItemProvider.GetRowHeaderItems(&nLength, &pTableItems);
	//						pReturnVal.SetSigned(nLength);
	//						break;
	//					case 3:
	//						pTableItemProvider.GetRowHeaderItems(&nLength, &pTableItems);
	//						pReturnVal.SetArrayElementCount(MIN(pReturnVal.GetArrayElementCount(), nLength));
	//						ppCurrentItem = (void**)pReturnVal.m_pvValue;
	//						for (uint i = 0; i < pReturnVal.GetArrayElementCount(); i++)
	//						{
	//							_Analysis_assume_(i == 0);
	//							if (pTableItems[i])
	//							{
	//								(IRawElementProviderSimple*)(pTableItems[i]).GetAutomationPeer(&pAP);
	//								if (pAP)
	//								{
	//									ppCurrentItem[i] = (AutomationPeer*)(pAP).GetHandle();
	//								}
	//							}
	//							ReleaseInterface(pAP);
	//						}
	//						break;
	//				}
	//			}
	//			break;
	//		case PatternInterface.Table:
	//			ctl.do_query_interface(pTableProvider, pTargetAsobject);
	//			if (pTableProvider)
	//			{
	//				xaml_automation.RowOrColumnMajor eRowOrColumnMajor;
	//				switch (eFunction)
	//				{
	//					case 0: // The first method is to get the length of buffer then second call is for copying buffer
	//						pTableProvider.GetColumnHeaders(&nLength, &pTableHeaders);
	//						pReturnVal.SetSigned(nLength);
	//						break;
	//					case 1:
	//						pTableProvider.GetColumnHeaders(&nLength, &pTableHeaders);
	//						pReturnVal.SetArrayElementCount(MIN(pReturnVal.GetArrayElementCount(), nLength));
	//						ppCurrentItem = (void**)pReturnVal.m_pvValue;
	//						for (uint i = 0; i < pReturnVal.GetArrayElementCount(); i++)
	//						{
	//							_Analysis_assume_(i == 0);
	//							if (pTableHeaders[i])
	//							{
	//								(IRawElementProviderSimple*)(pTableHeaders[i]).GetAutomationPeer(&pAP);
	//								if (pAP)
	//								{
	//									ppCurrentItem[i] = (AutomationPeer*)(pAP).GetHandle();
	//								}
	//							}
	//							ReleaseInterface(pAP);
	//						}
	//						break;
	//					case 2: // The first method is to get the length of buffer then second call is for copying buffer
	//						pTableProvider.GetRowHeaders(&nLength, &pTableHeaders);
	//						pReturnVal.SetSigned(nLength);
	//						break;
	//					case 3:
	//						pTableProvider.GetRowHeaders(&nLength, &pTableHeaders);
	//						pReturnVal.SetArrayElementCount(MIN(pReturnVal.GetArrayElementCount(), nLength));
	//						ppCurrentItem = (void**)pReturnVal.m_pvValue;
	//						for (uint i = 0; i < pReturnVal.GetArrayElementCount(); i++)
	//						{
	//							_Analysis_assume_(i == 0);
	//							if (pTableHeaders[i])
	//							{
	//								(IRawElementProviderSimple*)(pTableHeaders[i]).GetAutomationPeer(&pAP);
	//								if (pAP)
	//								{
	//									ppCurrentItem[i] = (AutomationPeer*)(pAP).GetHandle();
	//								}
	//							}
	//							ReleaseInterface(pAP);
	//						}
	//						break;
	//					case 4:
	//						eRowOrColumnMajor = pTableProvider.RowOrColumnMajor;
	//						BoxEnumValueHelper(pReturnVal, eRowOrColumnMajor);
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.Toggle:
	//			ctl.do_query_interface(pToggleProvider, pTargetAsobject);
	//			if (pToggleProvider)
	//			{
	//				xaml_automation.ToggleState eToggleState;
	//				switch (eFunction)
	//				{
	//					case 0:
	//						pToggleProvider.Toggle();
	//						break;
	//					case 1:
	//						eToggleState = pToggleProvider.ToggleState;
	//						BoxEnumValueHelper(pReturnVal, eToggleState);
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.Transform:
	//			ctl.do_query_interface(pTransformProvider, pTargetAsobject);
	//			if (pTransformProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						bValue = pTransformProvider.CanMove;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 1:
	//						bValue = pTransformProvider.CanResize;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 2:
	//						bValue = pTransformProvider.CanRotate;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 3:
	//						pTransformProvider.Move((&pValueParams[0]).m_eValue, (&pValueParams[1]).m_eValue);
	//						break;
	//					case 4:
	//						pTransformProvider.Resize((&pValueParams[0]).m_eValue, (&pValueParams[1]).m_eValue);
	//						break;
	//					case 5:
	//						pTransformProvider.Rotate((&pValueParams[0]).m_eValue);
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.Transform2:
	//			pTransformProvider2 = ctl.query_interface<xaml_automation.Provider.ITransformProvider2>(pTargetAsobject);
	//			if (pTransformProvider2)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						bValue = pTransformProvider2.CanZoom;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 1:
	//						dValue = pTransformProvider2.ZoomLevel;
	//						BoxValueHelper(pReturnVal, dValue);
	//						break;
	//					case 2:
	//						dValue = pTransformProvider2.MaxZoom;
	//						BoxValueHelper(pReturnVal, dValue);
	//						break;
	//					case 3:
	//						dValue = pTransformProvider2.MinZoom;
	//						BoxValueHelper(pReturnVal, dValue);
	//						break;
	//					case 4:
	//						pTransformProvider2.Zoom((&pValueParams[0]).m_eValue);
	//						break;
	//					case 5:
	//						pTransformProvider2.ZoomByUnit((xaml_automation.ZoomUnit)(&pValueParams[0]).m_nValue);
	//						break;
	//					// For IsTransformProvider2, If the object is of type ITransformProvider2 return True
	//					case 6:
	//						BoxValueHelper(pReturnVal, true);
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.ItemContainer:
	//			ctl.do_query_interface(pItemContainerProvider, pTargetAsobject);
	//			if (pItemContainerProvider)
	//			{
	//				GetAutomationPropertyFromUIAXcpEnum((UIAXcp.APAutomationProperties)pValueParams[1].m_iValue, &pAutomationProperty);
	//				GetPropertyValueFromCValue((UIAXcp.APAutomationProperties)pValueParams[1].m_iValue, pValueParams[2], &propertyValueAsInspectable);
	//				switch (eFunction)
	//				{
	//					case 0:
	//						if (pValueParams[0].m_pdoValue)
	//						{
	//							pCore.GetPeer(pValueParams[0].m_pdoValue, &pStartAfter);
	//							ctl.do_query_interface(pStartAfterAsAutomationPeer, pStartAfter);
	//							(AutomationPeer*)(pStartAfterAsAutomationPeer).ProviderFromPeer(pStartAfterAsAutomationPeer, &pStartAfterAsRaw);
	//						}

	//						pItemContainerProvider.FindItemByProperty(pStartAfterAsRaw, pAutomationProperty, propertyValueAsInspectable, &pProvider);
	//						if (pProvider)
	//						{
	//							(IRawElementProviderSimple*)(pProvider).GetAutomationPeer(&pAP);
	//							if (pAP)
	//							{
	//								pReturnVal.m_pdoValue = (AutomationPeer*)(pAP).GetHandle();
	//							}
	//						}
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.VirtualizedItem:
	//			ctl.do_query_interface(pVirtualizedItemProvider, pTargetAsobject);
	//			if (pVirtualizedItemProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						pVirtualizedItemProvider.Realize();
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.Window:
	//			ctl.do_query_interface(pWindowProvider, pTargetAsobject);
	//			if (pWindowProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						pWindowProvider.Close();
	//						break;
	//					case 1:
	//						bValue = pWindowProvider.IsModal;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 2:
	//						bValue = pWindowProvider.IsTopmost;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 3:
	//						bValue = pWindowProvider.Maximizable;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 4:
	//						bValue = pWindowProvider.Minimizable;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 5:
	//						pWindowProvider.SetVisualState((xaml_automation.WindowVisualState)(&pValueParams[0]).m_nValue);
	//						break;
	//					case 6:
	//						pWindowProvider.WaitForInputIdle((&pValueParams[0]).m_iValue, &bValue);
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 7:
	//						xaml_automation.WindowInteractionState eWindowInteractionState;
	//						eWindowInteractionState = pWindowProvider.InteractionState;
	//						BoxEnumValueHelper(pReturnVal, eWindowInteractionState);
	//						break;
	//					case 8:
	//						xaml_automation.WindowVisualState eWindowVisualState;
	//						eWindowVisualState = pWindowProvider.VisualState;
	//						BoxEnumValueHelper(pReturnVal, eWindowVisualState);
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.Text:
	//			ctl.do_query_interface(pTextProvider, pTargetAsobject);
	//			if (pTextProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0: // The first method is to get the length of buffer then second call is for copying buffer
	//						pTextProvider.GetSelection(&nLength, &pTextRangeSelections);
	//						pReturnVal.SetSigned(nLength);
	//						break;
	//					case 1:
	//						pTextProvider.GetSelection(&nLength, &pTextRangeSelections);
	//						pReturnVal.SetArrayElementCount(MIN(pReturnVal.GetArrayElementCount(), nLength));
	//						ppCurrentItem = (void**)pReturnVal.m_pvValue;
	//						for (uint i = 0; i < pReturnVal.GetArrayElementCount(); i++)
	//						{
	//							_Analysis_assume_(i == 0);
	//							if (pTextRangeSelections[i])
	//							{
	//								ExternalObjectReference.ConditionalWrap(pTextRangeSelections[i], &pPatternObject);
	//								ppCurrentItem[i] = pPatternObject.GetHandle();

	//								// Peg the object; the core is responsible for un-pegging.
	//								pPatternObject.PegNoRef();
	//							}
	//							ctl.release_interface(pPatternObject);
	//						}
	//						break;
	//					case 2:
	//						pTextProvider.GetVisibleRanges(&nLength, &pTextRangeSelections);
	//						pReturnVal.SetSigned(nLength);
	//						break;
	//					case 3:
	//						pTextProvider.GetVisibleRanges(&nLength, &pTextRangeSelections);
	//						pReturnVal.SetArrayElementCount(MIN(pReturnVal.GetArrayElementCount(), nLength));
	//						ppCurrentItem = (void**)pReturnVal.m_pvValue;
	//						for (uint i = 0; i < pReturnVal.GetArrayElementCount(); i++)
	//						{
	//							_Analysis_assume_(i == 0);
	//							if (pTextRangeSelections[i])
	//							{
	//								ExternalObjectReference.ConditionalWrap(pTextRangeSelections[i], &pPatternObject);
	//								ppCurrentItem[i] = pPatternObject.GetHandle();

	//								// Peg the object; the core is responsible for un-pegging.
	//								pPatternObject.PegNoRef();
	//							}
	//							ctl.release_interface(pPatternObject);
	//						}
	//						break;

	//					case 4:
	//						pCore.GetPeer((&pValueParams[0]).m_pdoValue, &pChildDO);
	//						ctl.do_query_interface(pAP, pChildDO);
	//						(AutomationPeer*)(pAP).ProviderFromPeer(pAP, &pRawElementProvider);
	//						pTextProvider.RangeFromChild(pRawElementProvider, &pTextRangeProvider);
	//						if (pTextRangeProvider)
	//						{
	//							ExternalObjectReference.ConditionalWrap(pTextRangeProvider, &pPatternObject);
	//							pReturnVal.m_pdoValue = pPatternObject.GetHandle();

	//							// Peg the object; the core is responsible for un-pegging.
	//							pPatternObject.PegNoRef();
	//						}
	//						else
	//						{
	//							pReturnVal.m_pdoValue = null;
	//						}
	//						break;
	//					case 5:
	//						(pValueParams[0]).GetPoint(pPoint);
	//						screenPoint.X = pPoint.x;
	//						screenPoint.Y = pPoint.y;
	//						pTextProvider.RangeFromPoint(screenPoint, &pTextRangeProvider);
	//						if (pTextRangeProvider)
	//						{
	//							ExternalObjectReference.ConditionalWrap(pTextRangeProvider, &pPatternObject);
	//							pReturnVal.m_pdoValue = pPatternObject.GetHandle();

	//							// Peg the object; the core is responsible for un-pegging.
	//							pPatternObject.PegNoRef();
	//						}
	//						else
	//						{
	//							pReturnVal.m_pdoValue = null;
	//						}
	//						break;
	//					case 6:
	//						pTextRangeProvider = pTextProvider.DocumentRange;
	//						if (pTextRangeProvider)
	//						{
	//							ExternalObjectReference.ConditionalWrap(pTextRangeProvider, &pPatternObject);
	//							pReturnVal.m_pdoValue = pPatternObject.GetHandle();

	//							// Peg the object; the core is responsible for un-pegging.
	//							pPatternObject.PegNoRef();
	//						}
	//						else
	//						{
	//							pReturnVal.m_pdoValue = null;
	//						}
	//						break;
	//					case 7:
	//						supportedTextSelection = pTextProvider.SupportedTextSelection;
	//						pReturnVal.m_nValue = (uint)supportedTextSelection;
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.Text2:
	//			pTextProvider2 = ctl.query_interface<xaml_automation.Provider.ITextProvider2>(pTargetAsobject);
	//			if (pTextProvider2)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						pCore.GetPeer((&pValueParams[0]).m_pdoValue, &pAnnotationDO);
	//						ctl.do_query_interface(pAP, pAnnotationDO);
	//						(AutomationPeer*)(pAP).ProviderFromPeer(pAP, &pRawElementProvider);
	//						pTextProvider2.RangeFromAnnotation(pRawElementProvider, &pTextRangeProvider);
	//						if (pTextRangeProvider)
	//						{
	//							ExternalObjectReference.ConditionalWrap(pTextRangeProvider, &pPatternObject);
	//							pReturnVal.m_pdoValue = pPatternObject.GetHandle();

	//							// Peg the object; the core is responsible for un-pegging.
	//							pPatternObject.PegNoRef();
	//						}
	//						else
	//						{
	//							pReturnVal.m_pdoValue = null;
	//						}
	//						break;

	//					case 1:
	//						pTextProvider2.GetCaretRange(&bValue, &pTextRangeProvider);
	//						BoxValueHelper(pReturnVal, bValue);
	//						if (pTextRangeProvider)
	//						{
	//							ExternalObjectReference.ConditionalWrap(pTextRangeProvider, &pPatternObject);
	//							pReturnVal.m_pdoValue = pPatternObject.GetHandle();

	//							// Peg the object; the core is responsible for un-pegging.
	//							pPatternObject.PegNoRef();
	//						}
	//						else
	//						{
	//							pReturnVal.m_pdoValue = null;
	//						}
	//						break;
	//					// For IsITextProvider2, If the object is of type ITextProvider2 return True
	//					case 2:
	//						BoxValueHelper(pReturnVal, true);
	//						break;
	//				}
	//			}
	//			else
	//			{
	//				if (eFunction == 2)
	//				{
	//					// For IsITextProvider2, If the object is not of type ITextProvider2 return False
	//					BoxValueHelper(pReturnVal, false);
	//				}
	//				else
	//				{
	//					E_FAIL;
	//				}
	//			}
	//			break;

	//		case PatternInterface.TextChild:
	//			ctl.do_query_interface(pTextChildProvider, pTargetAsobject);
	//			if (pTextChildProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						pProvider = pTextChildProvider.TextContainer;
	//						if (pProvider)
	//						{
	//							(IRawElementProviderSimple*)(pProvider).GetAutomationPeer(&pAP);
	//							if (pAP)
	//							{
	//								pReturnVal.m_pdoValue = (AutomationPeer*)(pAP).GetHandle();
	//							}
	//						}
	//						break;
	//					case 1:
	//						pTextRangeProvider = pTextChildProvider.TextRange;
	//						if (pTextRangeProvider)
	//						{
	//							ExternalObjectReference.ConditionalWrap(pTextRangeProvider, &pPatternObject);
	//							pReturnVal.m_pdoValue = pPatternObject.GetHandle();

	//							// Peg the object; the core is responsible for un-pegging.
	//							pPatternObject.PegNoRef();
	//						}
	//						else
	//						{
	//							pReturnVal.m_pdoValue = null;
	//						}
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.Annotation:
	//			ctl.do_query_interface(pAnnotationProvider, pTargetAsobject);
	//			if (pAnnotationProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						iValue = pAnnotationProvider.AnnotationTypeId;
	//						BoxValueHelper(pReturnVal, iValue);
	//						break;
	//					case 1:
	//						pAnnotationProvider.get_AnnotationTypeName(strValue.GetAddressOf());
	//						psTextTemp = strValue.GetRawBuffer(&pcTextTemp);
	//						BoxValueHelper(pReturnVal, (INT)pcTextTemp);
	//						break;
	//					case 2:
	//						pAnnotationProvider.get_AnnotationTypeName(strValue.GetAddressOf());
	//						SetCValueFromString(pReturnVal, strValue);
	//						break;
	//					case 3:
	//						pAnnotationProvider.get_Author(strValue.GetAddressOf());
	//						psTextTemp = strValue.GetRawBuffer(&pcTextTemp);
	//						BoxValueHelper(pReturnVal, (INT)pcTextTemp);
	//						break;
	//					case 4:
	//						pAnnotationProvider.get_Author(strValue.GetAddressOf());
	//						SetCValueFromString(pReturnVal, strValue);
	//						break;
	//					case 5:
	//						pAnnotationProvider.get_DateTime(strValue.GetAddressOf());
	//						psTextTemp = strValue.GetRawBuffer(&pcTextTemp);
	//						BoxValueHelper(pReturnVal, (INT)pcTextTemp);
	//						break;
	//					case 6:
	//						pAnnotationProvider.get_DateTime(strValue.GetAddressOf());
	//						SetCValueFromString(pReturnVal, strValue);
	//						break;
	//					case 7:
	//						pProvider = pAnnotationProvider.Target;
	//						if (pProvider)
	//						{
	//							((IRawElementProviderSimple*)(pProvider)).GetAutomationPeer(&pAP);
	//							if (pAP)
	//							{
	//								pReturnVal.m_pdoValue = (AutomationPeer*)(pAP).GetHandle();
	//							}
	//						}
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.Drag:
	//			ctl.do_query_interface(pDragProvider, pTargetAsobject);
	//			if (pDragProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						bValue = pDragProvider.IsGrabbed;
	//						BoxValueHelper(pReturnVal, bValue);
	//						break;
	//					case 1:
	//						pDragProvider.get_DropEffect(strValue.GetAddressOf());
	//						BoxValueHelper(pReturnVal, strValue);
	//						break;
	//					case 2:
	//						nLength, &phsArray = pDragProvider.DropEffects;
	//						BoxArrayOfHStrings(pReturnVal, nLength, phsArray);
	//						break;
	//					case 3:
	//						pDragProvider.GetGrabbedItems(&nLength, &pNoDeleteRepsArray);
	//						BoxArrayOfRawElementProviderSimple(pReturnVal, nLength, pNoDeleteRepsArray);
	//						break;
	//					default:
	//						MUX_ASSERT(!"Missing function for IDragProvider.");
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.DropTarget:
	//			ctl.do_query_interface(pDropTargetProvider, pTargetAsobject);
	//			if (pDropTargetProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						pDropTargetProvider.get_DropEffect(strValue.GetAddressOf());
	//						BoxValueHelper(pReturnVal, strValue);
	//						break;
	//					case 1:
	//						nLength, &phsArray = pDropTargetProvider.DropEffects;
	//						BoxArrayOfHStrings(pReturnVal, nLength, phsArray);
	//						break;
	//					default:
	//						MUX_ASSERT(!"Missing function for IDropTargetProvider.");
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.ObjectModel:
	//			ctl.do_query_interface(pObjectModelProvider, pTargetAsobject);
	//			if (pObjectModelProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						pObjectModelProvider.GetUnderlyingObjectModel(&objetModelInspectable);
	//						ctl.do_query_interface(objetModelIUnknown, objetModelInspectable);
	//						pReturnVal.SetIUnknownNoRef(objetModelIUnknown);
	//						objetModelIUnknown = null;
	//						break;

	//					default:
	//						MUX_ASSERT(!"Missing function for IObjectModelProvider.");
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.Spreadsheet:
	//			ctl.do_query_interface(pSpreadsheetProvider, pTargetAsobject);
	//			if (pSpreadsheetProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						IFCPTR(pReturnVal);
	//						IFCPTR(pValueParams);
	//						GetStringFromCValue(&pValueParams[0], strValue.GetAddressOf());
	//						pSpreadsheetProvider.GetItemByName(strValue, &pProvider);
	//						if (pProvider)
	//						{
	//							((IRawElementProviderSimple*)(pProvider)).GetAutomationPeer(&pAP);
	//							if (pAP)
	//							{
	//								pReturnVal.m_pdoValue = (AutomationPeer*)(pAP).GetHandle();
	//							}
	//						}
	//						break;
	//					default:
	//						MUX_ASSERT(!"Missing function for ISpreadsheetProvider.");
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.SpreadsheetItem:
	//			ctl.do_query_interface(pSpreadsheetItemProvider, pTargetAsobject);
	//			if (pSpreadsheetItemProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						pSpreadsheetItemProvider.get_Formula(strValue.GetAddressOf());
	//						BoxValueHelper(pReturnVal, strValue);
	//						break;
	//					case 1: // The first method is to get the length of buffer then second call is for copying buffer
	//						pSpreadsheetItemProvider.GetAnnotationObjects(&nLength, &pAnnotationObjects);
	//						pReturnVal.SetSigned(nLength);
	//						break;
	//					case 2:
	//						pSpreadsheetItemProvider.GetAnnotationObjects(&nLength, &pAnnotationObjects);
	//						pReturnVal.SetArrayElementCount(MIN(pReturnVal.GetArrayElementCount(), nLength));
	//						ppCurrentItem = (void**)pReturnVal.m_pvValue;
	//						for (uint i = 0; i < pReturnVal.GetArrayElementCount(); i++)
	//						{
	//							_Analysis_assume_(i == 0);
	//							if (pAnnotationObjects[i])
	//							{
	//								(IRawElementProviderSimple*)(pAnnotationObjects[i]).GetAutomationPeer(&pAP);
	//								if (pAP)
	//								{
	//									ppCurrentItem[i] = (AutomationPeer*)(pAP).GetHandle();
	//								}
	//							}
	//							ReleaseInterface(pAP);
	//						}
	//						break;
	//					case 3: // The first method is to get the length of buffer then second call is for copying buffer
	//						pSpreadsheetItemProvider.GetAnnotationTypes(&nLength, &pAnnotationType);
	//						pReturnVal.SetSigned(nLength);
	//						break;
	//					case 4:
	//						pSpreadsheetItemProvider.GetAnnotationTypes(&nLength, &pAnnotationType);
	//						pReturnVal.SetArrayElementCount(MIN(pReturnVal.GetArrayElementCount(), nLength));
	//						pCurrentAnnotationType = (UIAXcp.AnnotationType*)pReturnVal.m_pvValue;
	//						for (uint i = 0; i < pReturnVal.GetArrayElementCount(); i++)
	//						{
	//							_Analysis_assume_(i == 0);
	//							pCurrentAnnotationType[i] = (UIAXcp.AnnotationType)pAnnotationType[i];
	//						}
	//						break;

	//					default:
	//						MUX_ASSERT(!"Missing function for ISpreadsheetItemProvider.");
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.Styles:
	//			ctl.do_query_interface(pStylesProvider, pTargetAsobject);
	//			if (pStylesProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						pStylesProvider.get_ExtendedProperties(strValue.GetAddressOf());
	//						BoxValueHelper(pReturnVal, strValue);
	//						break;
	//					case 1:
	//						color = pStylesProvider.FillColor;
	//						BoxValueHelper(pReturnVal, color);
	//						break;
	//					case 2:
	//						color = pStylesProvider.FillPatternColor;
	//						BoxValueHelper(pReturnVal, color);
	//						break;
	//					case 3:
	//						pStylesProvider.get_FillPatternStyle(strValue.GetAddressOf());
	//						BoxValueHelper(pReturnVal, strValue);
	//						break;
	//					case 4:
	//						pStylesProvider.get_Shape(strValue.GetAddressOf());
	//						BoxValueHelper(pReturnVal, strValue);
	//						break;
	//					case 5:
	//						iValue = pStylesProvider.StyleId;
	//						BoxValueHelper(pReturnVal, iValue);
	//						break;
	//					case 6:
	//						pStylesProvider.get_StyleName(strValue.GetAddressOf());
	//						BoxValueHelper(pReturnVal, strValue);
	//						break;
	//					default:
	//						MUX_ASSERT(!"Missing function for IStylesProvider.");
	//						break;
	//				}
	//			}
	//			break;

	//		case PatternInterface.SynchronizedInput:
	//			ctl.do_query_interface(pSynchronizedInputProvider, pTargetAsobject);
	//			if (pSynchronizedInputProvider)
	//			{
	//				switch (eFunction)
	//				{
	//					case 0:
	//						pSynchronizedInputProvider.Cancel();
	//						break;
	//					case 1:
	//						pSynchronizedInputProvider.StartListening((xaml_automation.SynchronizedInputType)pValueParams[0].m_iValue);
	//						break;
	//					default:
	//						MUX_ASSERT(!"Missing function for ISynchronizedInputProvider.");
	//						break;
	//				}
	//			}
	//			break;
	//		case PatternInterface.TextEdit:
	//			{
	//				object spTargetAsobject(pTargetAsobject);
	//				spTextEditProvider = spTargetAsobject.AsOrNull<xaml_automation.Provider.ITextEditProvider>();
	//				if (spTextEditProvider)
	//				{
	//					switch (eFunction)
	//					{
	//						case 0:
	//							spTextEditProvider.GetActiveComposition(&pTextRangeProvider);
	//							if (pTextRangeProvider)
	//							{
	//								ExternalObjectReference.ConditionalWrap(pTextRangeProvider, &pPatternObject);
	//								pReturnVal.SetObjectAddRef(pPatternObject.GetHandle());

	//								// Peg the object; the core is responsible for un-pegging.
	//								pPatternObject.PegNoRef();
	//							}
	//							else
	//							{
	//								pReturnVal.SetNull();
	//							}
	//							break;
	//						case 1:
	//							spTextEditProvider.GetConversionTarget(&pTextRangeProvider);
	//							if (pTextRangeProvider)
	//							{
	//								ExternalObjectReference.ConditionalWrap(pTextRangeProvider, &pPatternObject);
	//								pReturnVal.SetObjectAddRef(pPatternObject.GetHandle());

	//								// Peg the object; the core is responsible for un-pegging.
	//								pPatternObject.PegNoRef();
	//							}
	//							else
	//							{
	//								pReturnVal.SetNull();
	//							}
	//							break;
	//						default:
	//							MUX_ASSERT(!"Missing function for ITextEditProvider.");
	//							break;
	//					}
	//				}
	//			}
	//			break;
	//		case PatternInterface.CustomNavigation:
	//			{
	//				object spTargetAsobject(pTargetAsobject);
	//				object spResult;
	//				xref_ptr <.DependencyObject > spReturnAPAsDO;
	//				IUnknown spReturnIREPFAsUnk;
	//				spCustomNavigationProvider = spTargetAsobject.AsOrNull<xaml_automation.Provider.ICustomNavigationProvider>();
	//				if (spCustomNavigationProvider)
	//				{
	//					switch (eFunction)
	//					{
	//						case 0:
	//							navigationDirection = (AutomationNavigationDirection)(&pValueParams[0]).m_nValue;
	//							spCustomNavigationProvider.NavigateCustom(navigationDirection, &spResult);
	//							if (spResult)
	//							{
	//								RetrieveNativeNodeOrAPFromobject(spResult, spReturnAPAsDO.ReleaseAndGetAddressOf(), &spReturnIREPFAsUnk);
	//							}
	//							IFCEXPECT(2 == pReturnVal.GetArrayElementCount());
	//							ppCurrentItem = (void**)pReturnVal.m_pvValue;
	//							ppCurrentItem[0] = spReturnAPAsDO.detach();
	//							ppCurrentItem[1] = spReturnIREPFAsUnk.Detach();
	//							break;
	//						default:
	//							MUX_ASSERT(!"Missing function for ICustomNavigationProvider.");
	//							break;
	//					}
	//				}
	//			}
	//			break;

	//		default:
	//			MUX_ASSERT(!"Unknown pattern interface.");
	//			break;
	//	}


	//Cleanup:
	//	ctl.release_interface(pMOROrTarget);
	//	ctl.release_interface(pStartAfter);
	//	ctl.release_interface(pChildDO);
	//	ctl.release_interface(pAnnotationDO);
	//	ctl.release_interface(pPatternObject);
	//	ReleaseInterface(pAP);
	//	ReleaseInterface(pStartAfterAsAutomationPeer);
	//	ReleaseInterface(pStartAfterAsRaw);
	//	ReleaseInterface(pAutomationProperty);
	//	ReleaseInterface(propertyValueAsInspectable);
	//	ReleaseInterface(objetModelInspectable);
	//	ReleaseInterface(pProvider);
	//	ReleaseInterface(pRawElementProvider);
	//	ReleaseInterface(pMOROrTargetAsobject);
	//	ReleaseInterface(pTargetAsobject);
	//	ReleaseInterface(pInvokeProvider);
	//	ReleaseInterface(pDockProvider);
	//	ReleaseInterface(pExpandCollapseProvider);
	//	ReleaseInterface(pValueProvider);
	//	ReleaseInterface(pGridItemProvider);
	//	ReleaseInterface(pGridProvider);
	//	ReleaseInterface(pMultipleViewProvider);
	//	ReleaseInterface(pRangeValueProvider);
	//	ReleaseInterface(pScrollItemProvider);
	//	ReleaseInterface(pScrollProvider);
	//	ReleaseInterface(pSelectionItemProvider);
	//	ReleaseInterface(pSelectionProvider);
	//	ReleaseInterface(pTableItemProvider);
	//	ReleaseInterface(pTableProvider);
	//	ReleaseInterface(pToggleProvider);
	//	ReleaseInterface(pTransformProvider);
	//	ReleaseInterface(pVirtualizedItemProvider);
	//	ReleaseInterface(pItemContainerProvider);
	//	ReleaseInterface(pWindowProvider);
	//	ReleaseInterface(pTextProvider);
	//	ReleaseInterface(pTextProvider2);
	//	ReleaseInterface(pTextChildProvider);
	//	ReleaseInterface(pTextRangeProvider);
	//	ReleaseInterface(pRawElementProvider);
	//	ReleaseInterface(pDragProvider);
	//	ReleaseInterface(pDropTargetProvider);
	//	ReleaseInterface(pTransformProvider2);
	//	ReleaseInterface(pObjectModelProvider);
	//	ReleaseInterface(pSpreadsheetProvider);
	//	ReleaseInterface(pSpreadsheetItemProvider);
	//	ReleaseInterface(pStylesProvider);
	//	ReleaseInterface(pSynchronizedInputProvider);

	//	DELETE_ARRAY(pAnnotationType);
	//	if (pSelections)
	//	{
	//		for (uint i = 0; i < nLength; i++)
	//		{
	//			ReleaseInterface(pSelections[i]);
	//		}
	//	}
	//	DELETE_ARRAY(pSelections);
	//	if (pAnnotationObjects)
	//	{
	//		for (uint i = 0; i < nLength; i++)
	//		{
	//			ReleaseInterface(pAnnotationObjects[i]);
	//		}
	//	}
	//	DELETE_ARRAY(pAnnotationObjects);
	//	if (pTableItems)
	//	{
	//		for (uint i = 0; i < nLength; i++)
	//		{
	//			ReleaseInterface(pTableItems[i]);
	//		}
	//	}
	//	DELETE_ARRAY(pTableItems);
	//	if (pTableHeaders)
	//	{
	//		for (uint i = 0; i < nLength; i++)
	//		{
	//			ReleaseInterface(pTableHeaders[i]);
	//		}
	//	}
	//	DELETE_ARRAY(pTableHeaders);
	//	if (pTextRangeSelections)
	//	{
	//		for (uint i = 0; i < nLength; i++)
	//		{
	//			ReleaseInterface(pTextRangeSelections[i]);
	//		}
	//	}
	//	DELETE_ARRAY(pTextRangeSelections);
	//	if (phsArray)
	//	{
	//		for (uint i = 0; i < nLength; i++)
	//		{
	//			DELETE_STRING(phsArray[i]);
	//		}
	//	}
	//	DELETE_ARRAY(phsArray);
	//	DELETE_ARRAY(pIntArray);
	//	RRETURN(hr);
	//}

	//// Callback from Core to Notify Owner to realse AP as no UIA client is holding on to it.
	//private void NotifyNoUIAClientObjectForAP(DependencyObject* nativeTarget)
	//{

	//	DependencyObject* pTarget = null;
	//	IAutomationPeer* pAP = null;

	//	IFCPTR(nativeTarget);
	//	DXamlCore.GetCurrent().TryGetPeer(nativeTarget, &pTarget);
	//	if (pTarget)
	//	{
	//		ctl.do_query_interface(pAP, pTarget);
	//		IFCPTR(pAP);
	//		(AutomationPeer*)(pAP)).NotifyNoUIAClientObjectToOwner(;
	//	}

	//Cleanup:
	//	ctl.release_interface(pTarget);
	//	ReleaseInterface(pAP);
	//	RRETURN(hr);
	//}

	//// Callback from Core to generate EventsSource for this AP if one exist.
	//private void GenerateAutomationPeerEventsSource(DependencyObject* nativeTarget, DependencyObject* nativeTargetParent)
	//{

	//	DependencyObject* pTarget = null;
	//	DependencyObject* pTargetParent = null;
	//	IAutomationPeer* pAP = null;
	//	IAutomationPeer* pAPParent = null;
	//	DXamlCore* pCore = DXamlCore.GetCurrent();

	//	IFCPTR(nativeTarget);
	//	IFCPTR(nativeTargetParent);

	//	pCore.TryGetPeer(nativeTarget, &pTarget);
	//	pCore.TryGetPeer(nativeTargetParent, &pTargetParent);
	//	if (pTarget && pTargetParent)
	//	{
	//		ctl.do_query_interface(pAP, pTarget);
	//		ctl.do_query_interface(pAPParent, pTargetParent);
	//		IFCPTR(pAP);
	//		IFCPTR(pAPParent);
	//		((AutomationPeer*)(pAP)).GenerateAutomationPeerEventsSource(pAPParent);
	//	}

	//Cleanup:
	//	ctl.release_interface(pTarget);
	//	ctl.release_interface(pTargetParent);
	//	ReleaseInterface(pAP);
	//	ReleaseInterface(pAPParent);
	//	RRETURN(hr);
	//}

	//void RetrieveNativeNodeOrAPFromobject(
	//	 object pAccessibleNode,
	//	out result_maybenull_.DependencyObject** ppReturnAPAsDO,
	//	out result_maybenull_ IUnknown**ppReturnIREPFAsUnk)
	//{
	//	object spAccessibleNode(pAccessibleNode);
	//	IAutomationPeer spAP;
	//	IUnknown spIREPFAsUnk;
	//	xref_ptr <.DependencyObject > spReturnAPAsDO;

	//	*ppReturnAPAsDO = null;
	//	*ppReturnIREPFAsUnk = null;

	//	spAP = spAccessibleNode.AsOrNull<IAutomationPeer>();
	//	if (spAP)
	//	{
	//		spReturnAPAsDO = spAP.Cast<AutomationPeer>().GetHandle();
	//	}
	//	else
	//	{
	//		spIREPFAsUnk = spAccessibleNode.AsOrNull<IUnknown>();
	//	}

	//	*ppReturnAPAsDO = spReturnAPAsDO.detach();
	//	*ppReturnIREPFAsUnk = spIREPFAsUnk.Detach();
	//}

	//private void GetAutomationPropertyFromUIAXcpEnum(UIAXcp.APAutomationProperties eProperty, out xaml_automation.IAutomationProperty** ppAutomationProperty)
	//{

	//	xaml_automation.IAutomationElementIdentifiersStatics spAutomationElementIdentifiersStatics;
	//	xaml_automation.ISelectionItemPatternIdentifiersStatics spSelectionItemPatternIdentifiersStatics;
	//	xaml_automation.IAutomationProperty spAutomationProperty;

	//	*ppAutomationProperty = null;

	//	// AutomationElementIdentifiers MUST be cached. Because AutomationProperties don't expose any public API
	//	// surface to tell them apart we must rely on pointer comparisons. We rely on Jupiter's ActivationFactory cache
	//	// for this behavior.
	//	(ctl.GetActivationFactory(
	//		stringReference(RuntimeClass_Microsoft_UI_Xaml_Automation_AutomationElementIdentifiers),
	//		&spAutomationElementIdentifiersStatics));

	//	switch ((AutomationPropertiesEnum)(eProperty))
	//	{
	//		case AutomationPropertiesEnum.EmptyProperty:
	//			// spAutomationProperty is already set to null.
	//			break;
	//		case AutomationPropertiesEnum.AutomationIdProperty:
	//			spAutomationProperty = spAutomationElementIdentifiersStatics.AutomationIdProperty;
	//			break;
	//		case AutomationPropertiesEnum.NameProperty:
	//			spAutomationProperty = spAutomationElementIdentifiersStatics.NameProperty;
	//			break;
	//		case AutomationPropertiesEnum.ControlTypeProperty:
	//			spAutomationProperty = spAutomationElementIdentifiersStatics.ControlTypeProperty;
	//			break;
	//		case AutomationPropertiesEnum.IsSelectedProperty:
	//			(ctl.GetActivationFactory(
	//				stringReference(RuntimeClass_Microsoft_UI_Xaml_Automation_SelectionItemPatternIdentifiers),
	//				&spSelectionItemPatternIdentifiersStatics));
	//			spAutomationProperty = spSelectionItemPatternIdentifiersStatics.IsSelectedProperty;
	//			break;
	//		default:
	//			ErrorHelper.OriginateError(AgError(UIA_OPERATION_CANNOT_BE_PERFORMED));
	//			(HRESULT)(UIA_E_INVALIDOPERATION);
	//			break;
	//	}

	//	*ppAutomationProperty = spAutomationProperty.Detach();

	//Cleanup:
	//	RRETURN(hr);
	//}

	//private void GetPropertyValueFromCValue(UIAXcp.APAutomationProperties eProperty, Automation.CValue value, out object* ppInspectable)
	//{

	//	string strValue;
	//	object pInspectable = null;
	//	switch ((AutomationPropertiesEnum)(eProperty))
	//	{
	//		case AutomationPropertiesEnum.AutomationIdProperty:
	//		case AutomationPropertiesEnum.NameProperty:
	//			GetStringFromCValue(&value, strValue.GetAddressOf());
	//			DirectUI.PropertyValue.CreateFromString(strValue, &pInspectable);
	//			break;
	//		case AutomationPropertiesEnum.ControlTypeProperty:
	//			DirectUI.PropertyValue.CreateFromInt32(value.m_iValue, &pInspectable);
	//			break;
	//		case AutomationPropertiesEnum.IsSelectedProperty:
	//			DirectUI.PropertyValue.CreateFromBoolean(!!value.m_nValue, &pInspectable);
	//			break;
	//	}
	//	*ppInspectable = pInspectable;
	//	pInspectable = null;

	//Cleanup:
	//	ReleaseInterface(pInspectable);
	//	RRETURN(hr);
	//}

	//private void GetStringFromCValue(
	//	 Automation.object pBox,
	//	out string* phValue)
	//{


	//	IFCPTR(pBox);
	//	IFCPTR(phValue);

	//	if (pBox.IsNull())
	//	{
	//		*phValue = null;
	//	}
	//	else
	//	{
	//		IFCEXPECT(pBox.GetType() == valueString);
	//		xruntime_string_ptr strRuntimeValue;
	//		pBox.AsString().Promote(&strRuntimeValue);
	//		*phValue = strRuntimeValue.Detachstring();
	//	}

	//Cleanup:
	//	RRETURN(hr);

	//}

	//private void SetCValueFromString(
	//	 Automation.object pBox,
	//	 string value)
	//{


	//	IFCPTR(pBox);

	//	if (value != null)
	//	{
	//		xstring_ptr strValue;
	//		xstring_ptr.CloneRuntimeStringHandle(value, &strValue);
	//		pBox.SetString(std.move(strValue));
	//	}
	//	else
	//	{
	//		pBox.SetString(xstring_ptr.NullString());
	//	}
	//Cleanup:
	//	RRETURN(hr);
	//}

	//// Internal helper method to call ListenExists()
	//private void ListenerExistsHelper(
	//	 AutomationEvents eventId,
	//	out bool* pReturnValue)
	//{

	//	bool bAutomationListener = false;
	//	IActivationFactory* pActivationFactory = null;
	//	IAutomationPeerStatics* pAutomationPeerStatics = null;

	//	IFCPTR(pReturnValue);

	//	pActivationFactory = ctl.ActivationFactoryCreator<DirectUI.AutomationPeerFactory>.CreateActivationFactory();
	//	ctl.do_query_interface(pAutomationPeerStatics, pActivationFactory);
	//	pAutomationPeerStatics.ListenerExists(eventId, &bAutomationListener);

	//	*pReturnValue = bAutomationListener;

	//Cleanup:
	//	ReleaseInterface(pActivationFactory);
	//	ReleaseInterface(pAutomationPeerStatics);

	//	RRETURN(hr);
	//}

	//private void AutomationPeerFactory.ListenerExistsImpl(
	// AutomationEvents eventId,
	// out bool* returnValue)
	//{

	//	bool bListenerExist = false;

	//	CoreImports.AutomationListenerExists((CCoreServices*)(DXamlCore.GetCurrent().GetHandle()), (UIAXcp.APAutomationEvents)eventId, &bListenerExist);
	//	*returnValue = !!bListenerExist;

	//Cleanup:
	//	RRETURN(hr);
	//}

	// Static function to provide a mechanism for making sure uniqueness of runtimeIds across the board.
	// As with this change we are allowing a merge of XAML APs and native UIA nodes, there needs to be a
	// common place for managing runtimeIds, this static function serves the task.
	public static RawElementProviderRuntimeId GenerateRawElementProviderRuntimeId()
	{

		RawElementProviderRuntimeId rawElementProviderRuntimeId;
		IDXamlCore* pCore = DXamlServices.GetDXamlCore();

		if (pCore)
		{
			rawElementProviderRuntimeId.Part2 = pCore.GenerateRawElementProviderRuntimeId();

			// Each node provides a locally unique ID which is appended to the ID of the containing provider fragment root.
			// This is accomplished by specifying UiaAppendRuntimeId as the first value in the array.
			rawElementProviderRuntimeId.Part1 = UiaAppendRuntimeId;
		}

		return rawElementProviderRuntimeId;
	}

	//private void BoxArrayOfHStrings(Automation.object pReturnVal, Xint nLength, string* phsArray)
	//{

	//	Automation.object pValueArray = null;
	//	Xint cValueArray = 0;

	//	pReturnVal.SetNull();
	//	if (nLength < 1)
	//	{
	//		goto Cleanup;
	//	}

	//	pValueArray = new Automation.CValue[nLength];

	//	for (Xint i = 0; i < nLength; ++i)
	//	{
	//		BoxValueHelper(&pValueArray[cValueArray], phsArray[i]);
	//		++cValueArray;
	//	}

	//	pReturnVal.SetPointer(pValueArray);
	//	pReturnVal.SetArrayElementCount(cValueArray);
	//	pValueArray = null;

	//Cleanup:
	//	delete[] pValueArray;
	//	RRETURN(hr);
	//}

	//bool AutomationPeer.ArePropertyChangedListeners()
	//{
	//	bool bListenerExist = false;
	//	IGNOREHR(CoreImports.AutomationListenerExists((CCoreServices*)(DXamlCore.GetCurrent().GetHandle()), UIAXcp.APAutomationEvents.AEPropertyChanged, &bListenerExist));
	//	return !!bListenerExist;
	//}

	//private void RaiseEventIfListener(
	//	 UIElement* pUie,
	//	 AutomationEvents eventId)
	//{

	//	bool bListener = false;

	//	IFCPTR(pUie);

	//	AutomationPeer.ListenerExistsHelper(eventId, &bListener);
	//	if (bListener)
	//	{
	//		IAutomationPeer spAP;

	//		pUie.GetOrCreateAutomationPeer(&spAP);
	//		if (spAP)
	//		{
	//			spAP.RaiseAutomationEvent(eventId);
	//		}
	//	}

	//Cleanup:
	//	RRETURN(hr);
	//}

	//private void BoxArrayOfRawElementProviderSimple(Automation.object pReturnVal, Xint nLength, xaml_automation.Provider.IIRawElementProviderSimple** pRepsArray)
	//{

	//	DependencyObject** pDoArray = null;
	//	Xint cDoArray = 0;

	//	pReturnVal.SetNull();
	//	if (nLength < 1)
	//	{
	//		goto Cleanup;
	//	}

	//	pDoArray = new DependencyObject*[nLength];

	//	for (int i = 0; i < nLength; ++i)
	//	{
	//		IAutomationPeer spAp = null;
	//		(IRawElementProviderSimple*)(pRepsArray[i]).GetAutomationPeer(&spAp);
	//		pDoArray[cDoArray++] = spAp.Cast<AutomationPeer>().GetHandleAddRef();
	//	}

	//	pReturnVal.SetPointer(pDoArray);
	//	pReturnVal.SetArrayElementCount(cDoArray);
	//	pDoArray = null;
	//	cDoArray = 0;

	//Cleanup:
	//	if (pDoArray)
	//	{
	//		while (cDoArray--)
	//		{
	//			ReleaseInterface(pDoArray[cDoArray]);
	//		}
	//		delete[] pDoArray;
	//	}

	//	RRETURN(hr);
	//}

	//private void GetAutomationPeerDOValueFromIterable(
	//	 DependencyObject* nativeTarget,
	//	 UIAXcp.APAutomationProperties eProperty,
	//	out .DependencyObject** ppReturnDO)
	//{
	//	IFCPTR_RETURN(nativeTarget);
	//	IFCPTR_RETURN(ppReturnDO);
	//	*ppReturnDO = null;

	//	DependencyObject spTarget;
	//	DXamlCore.GetCurrent().GetPeer(nativeTarget, &spTarget);

	//	IIterable<AutomationPeer*> spAutomationPeerIterable;

	//	AutomationPeer spTargetAsAutomationPeer;
	//	spTarget.As(&spTargetAsAutomationPeer);

	//	switch (eProperty)
	//	{
	//		case UIAXcp.APDescribedByProperty:
	//			spTargetAsAutomationPeer.GetDescribedByCoreProtected(&spAutomationPeerIterable);
	//			break;
	//		case UIAXcp.APFlowsToProperty:
	//			spTargetAsAutomationPeer.GetFlowsToCoreProtected(&spAutomationPeerIterable);
	//			break;
	//		case UIAXcp.APFlowsFromProperty:
	//			spTargetAsAutomationPeer.GetFlowsFromCoreProtected(&spAutomationPeerIterable);
	//			break;
	//	}

	//	unsigned size = 0;
	//	AutomationPeerCollection spCollection;

	//	if (spAutomationPeerIterable)
	//	{
	//		// AGCore doesn't know about DXAML Iterables. So, we have to marshal to a CAutomationPeerCollection.
	//		IIterator<AutomationPeer*> spIter;
	//		IAutomationPeer spAP;
	//		spCollection = new();

	//		spAutomationPeerIterable.First(&spIter);
	//		boolean hasCurrent = false;
	//		hasCurrent = spIter.HasCurrent;
	//		while (hasCurrent)
	//		{
	//			spAP = spIter.Current;

	//			// Bug 25840857: Narrator touch investigation outside Launcher context menus crashes Shell
	//			// We're encountering timing issues where we try to access FlowsTo of a PopupRootAutomationPeer after all
	//			// popups have been closed. In that case PopupRootAutomationPeer.GetLightDismissingPopupAP will call
	//			// CPopupRoot.GetTopmostPopupInLightDismissChain to get the next popup, but there won't be one.
	//			// We'll then add null to the spAutomationPeerIterable list. Skip this entry - AutomationPeerCollection
	//			// returns E_INVALIDARG when you try to add null into it, which causes a fail fast for the 10X shell.
	//			if (spAP)
	//			{
	//				++size;
	//				spCollection.Add(spAP);
	//			}

	//			spIter.MoveNext(&hasCurrent);
	//		}
	//	}

	//	if (size == 0)
	//	{
	//		return S_OK;
	//	}

	//	*ppReturnDO = spCollection.GetHandle();
	//	CoreImports.DependencyObject_AddRef(*ppReturnDO);

	//	return S_OK;
	//}

}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.



namespace Windows.UI.Xaml.Controls.Primitives
{
	public partial class LoopingSelectorItem
	{

LoopingSelectorItem.LoopingSelectorItem()
	: _state(State.Normal)
	, _visualIndex(0)
	, _pParentNoRef(null)
	, _hasPeerBeenCreated(false)
{}


void
LoopingSelectorItem.InitializeImpl()
{
	
	wrl.ComPtr<xaml_controls.IContentControlFactory> spInnerFactory;
	wrl.ComPtr<xaml_controls.IContentControl> spInnerInstance;
	wrl.ComPtr<DependencyObject> spInnerInspectable;

	LoopingSelectorItemGenerated.InitializeImpl();
	(wf.GetActivationFactory(
		wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Controls_ContentControl),
		&spInnerFactory));

	(spInnerFactory.CreateInstance(
		(DependencyObject)((ILoopingSelectorItem)(this)),
		&spInnerInspectable,
		&spInnerInstance));

	(SetComposableBasePointers(
			spInnerInspectable,
			spInnerFactory));

	(Private.SetDefaultStyleKey(
			spInnerInspectable,
			"Microsoft.UI.Xaml.Controls.Primitives.LoopingSelectorItem"));

// Cleanup
	// return hr;
}

 void LoopingSelectorItem.OnPointerEnteredImpl( xaml.Input.IPointerRoutedEventArgs pEventArgs)
{
	var pointerDeviceType = wdei.PointerDeviceType_Touch;
	wrl.ComPtr<wui.IPointerPoint> spPointerPoint;
	wrl.ComPtr<wdei.IPointerDevice> spPointerDevice;

	pEventArgs.GetCurrentPoint(null, &spPointerPoint);
	if(spPointerPoint == null) { return; }
spPointerDevice =     spPointerPoint.PointerDevice;
	if(spPointerDevice == null) { return; }
pointerDeviceType =     spPointerDevice.PointerDeviceType;

	if (pointerDeviceType == wdei.PointerDeviceType_Mouse)
	{
		if (_state != State.Selected)
		{
			SetState(State.PointerOver, false);
		}
	}

	return;
}

 void LoopingSelectorItem.OnPointerPressedImpl( xaml.Input.IPointerRoutedEventArgs)
{
	if (_state != State.Selected)
	{
		SetState(State.Pressed, false);
	}

	return;
}

 void LoopingSelectorItem.OnPointerExitedImpl( xaml.Input.IPointerRoutedEventArgs)
{
	if (_state != State.Selected)
	{
		SetState(State.Normal, false);
	}

	return;
}

 void LoopingSelectorItem.OnPointerCaptureLostImpl( xaml_input.IPointerRoutedEventArgs)
{
	if (_state != State.Selected)
	{
		SetState(State.Normal, false);
	}

	return;
}

#region UIElementOverrides
 void
LoopingSelectorItem.OnCreateAutomationPeerImpl(
	out  xaml.Automation.Peers.IAutomationPeer returnValue)
{
	
	wrl.ComPtr<xaml_automation_peers.LoopingSelectorItemAutomationPeer> spLoopingSelectorItemAutomationPeer;

	(wrl.MakeAndInitialize<xaml_automation_peers.LoopingSelectorItemAutomationPeer>
			(spLoopingSelectorItemAutomationPeer, this));

	_hasPeerBeenCreated = true;

	spLoopingSelectorItemAutomationPeer.CopyTo(returnValue);
// Cleanup
	// return hr;
}
#endregion 

 void LoopingSelectorItem.GoToState( State newState,  bool useTransitions)
{
	string strState = null;

	switch (newState)
	{
	case State.Normal:
		strState = "Normal";
		break;
	case State.Expanded:
		strState = "Expanded";
		break;
	case State.Selected:
		strState = "Selected";
		break;
	case State.PointerOver:
		strState = "PointerOver";
		break;
	case State.Pressed:
		strState = "Pressed";
		break;
	}

	wrl.ComPtr<xaml.IVisualStateManagerStatics> spVSMStatics;
	wrl.ComPtr<xaml_controls.IControl> spThisAsControl;

	(wf.GetActivationFactory(
		wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_VisualStateManager),
		&spVSMStatics));

	QueryInterface(__uuidof(xaml_controls.IControl), &spThisAsControl);

	boolean returnValue = false;
	spVSMStatics.GoToState(spThisAsControl, wrl_wrappers.Hstring(strState), useTransitions, &returnValue);

	return;
}

 void LoopingSelectorItem.SetState( State newState,  bool useTransitions)
{
	
	// NOTE: Not calling GoToState when the LSI is already in the target
	// state allows us to keep animations looking smooth when the following
	// sequence of events happens:
	// LS starts closing . Items changes . LSIs are Recycled/Realized/Assigned New Content
	if (newState != _state)
	{
		GoToState(newState, useTransitions);
		_state = newState;
	}

// Cleanup
	// return hr;
}

 void LoopingSelectorItem.AutomationSelect()
{
	

	LoopingSelector pLoopingSelectorNoRef = null;
	GetParentNoRef(pLoopingSelectorNoRef);
	pLoopingSelectorNoRef.AutomationScrollToVisualIdx(_visualIndex, true /* ignoreScrollingState */);
// Cleanup
	// return hr;
}

 void LoopingSelectorItem.AutomationGetSelectionContainerUIAPeer(out xaml.Automation.Peers.IAutomationPeer ppPeer)
{
	

	wrl.ComPtr<xaml.Automation.Peers.FrameworkElementAutomationPeerStatics> spAutomationPeerStatics;
	wrl.ComPtr<xaml.Automation.Peers.IAutomationPeer> spAutomationPeer;
	wrl.ComPtr<UIElement> spLoopingSelectorAsUI;
	LoopingSelector pLoopingSelectorNoRef = null;

	GetParentNoRef(pLoopingSelectorNoRef);
	(pLoopingSelectorNoRef.QueryInterface(
		__uuidof(UIElement),
		&spLoopingSelectorAsUI));

	(wf.GetActivationFactory(
		  wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_Peers_FrameworkElementAutomationPeer),
		  &spAutomationPeerStatics));
	spAutomationPeerStatics.CreatePeerForElement(spLoopingSelectorAsUI, &spAutomationPeer);
	spAutomationPeer.CopyTo(ppPeer);
// Cleanup
	// return hr;
}

 void LoopingSelectorItem.AutomationGetIsSelected(out bool value)
{
	LoopingSelector loopingSelectorNoRef = null;
	INT selectedIdx;

	GetParentNoRef(loopingSelectorNoRef);
selectedIdx =     loopingSelectorNoRef.SelectedIndex;

	uint itemIndex = 0;
	loopingSelectorNoRef.VisualIndexToItemIndex(_visualIndex, &itemIndex);

	value = (selectedIdx == (INT)(itemIndex));

	return;
}

 void LoopingSelectorItem.AutomationUpdatePeerIfExists( int itemIndex)
{
	

	if (_hasPeerBeenCreated)
	{
		wrl.ComPtr<xaml_automation_peers.IAutomationPeer> spAutomationPeer;
		wrl.ComPtr<xaml_automation_peers.ILoopingSelectorItemAutomationPeer> spLSIAP;
		xaml_automation_peers.LoopingSelectorItemAutomationPeer pLSIAP;
		wrl.ComPtr<UIElement> spThisAsUI;
		wrl.ComPtr<xaml_automation_peers.FrameworkElementAutomationPeerStatics> spAutomationPeerStatics;

		QueryInterface(__uuidof(UIElement), &spThisAsUI);
		(wf.GetActivationFactory(
			wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_Peers_FrameworkElementAutomationPeer),
			&spAutomationPeerStatics));
		// CreatePeerForElement does not always create one - if there is one associated with this UIElement it will reuse that.
		// We do not want to end up creating a bunch of peers for the same element causing Narrator to get confused.
		spAutomationPeerStatics.CreatePeerForElement(spThisAsUI, &spAutomationPeer);
		spAutomationPeer.As(spLSIAP);
		pLSIAP = (xaml_automation_peers.LoopingSelectorItemAutomationPeer)(spLSIAP);

		pLSIAP.UpdateEventSource();
		pLSIAP.UpdateItemIndex(itemIndex);
	}

// Cleanup
	// return hr;
}

} } } } } XAML_ABI_NAMESPACE_END

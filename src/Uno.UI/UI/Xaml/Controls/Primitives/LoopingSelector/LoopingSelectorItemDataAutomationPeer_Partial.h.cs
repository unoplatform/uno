// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Windows.UI.Xaml.Automation.Peers
{

	partial class LoopingSelectorItemDataAutomationPeer : AutomationPeer
	{

		// public
		//LoopingSelectorItemDataAutomationPeer();

		// void GetItem(out DependencyObject ppItem);
		// void SetItemIndex( int index);

		// protected
		// IAutomationPeerOverrides
		//void GetPatternCoreImpl( xaml_automation_peers.PatternInterface patternInterface, out  DependencyObject returnValue) override;
		//void GetAcceleratorKeyCoreImpl(out string returnValue) override;
		//void GetAccessKeyCoreImpl(out string returnValue) override;
		//void GetAutomationControlTypeCoreImpl(out xaml_automation_peers.AutomationControlType returnValue) override;
		//void GetAutomationIdCoreImpl(out string returnValue) override;
		//void GetBoundingRectangleCoreImpl(out wf.Rect returnValue) override;
		//void GetChildrenCoreImpl(out  wfc.IList<xaml_automation_peers.AutomationPeer> returnValue) override;
		//void GetClassNameCoreImpl(out string returnValue) override;
		//void GetClickablePointCoreImpl(out wf.Point returnValue) override;
		//void GetHelpTextCoreImpl(out string returnValue) override;
		//void GetItemStatusCoreImpl(out string returnValue) override;
		//void GetItemTypeCoreImpl(out string returnValue) override;
		//void GetLabeledByCoreImpl(out  xaml_automation_peers.IAutomationPeer returnValue) override;
		//void GetLocalizedControlTypeCoreImpl(out string returnValue) override;
		//void GetNameCoreImpl(out string returnValue) override;
		//void GetOrientationCoreImpl(out xaml_automation_peers.AutomationOrientation returnValue) override;
		//void GetLiveSettingCoreImpl(out xaml_automation_peers.AutomationLiveSetting returnValue) override;
		//void GetControlledPeersCoreImpl(out  wfc.IVectorView<xaml_automation_peers.AutomationPeer> returnValue) override;
		//void HasKeyboardFocusCoreImpl(out bool returnValue) override;
		//void IsContentElementCoreImpl(out bool returnValue) override;
		//void IsControlElementCoreImpl(out bool returnValue) override;
		//void IsEnabledCoreImpl(out bool returnValue) override;
		//void IsKeyboardFocusableCoreImpl(out bool returnValue) override;
		//void IsOffscreenCoreImpl(out bool returnValue) override;
		//void IsPasswordCoreImpl(out bool returnValue) override;
		//void IsRequiredForFormCoreImpl(out bool returnValue) override;
		//void SetFocusCoreImpl() override;

		// IAutomationPeerOverrides3
		//void GetAnnotationsCoreImpl(out  wfc.IList<xaml_automation_peers.AutomationPeerAnnotation> returnValue);
		//void GetPositionInSetCoreImpl(out INT returnValue);
		//void GetSizeOfSetCoreImpl(out INT returnValue);
		//void GetLevelCoreImpl(out INT returnValue);

		// IAutomationPeerOverrides4
		//void GetLandmarkTypeCoreImpl(out xaml_automation_peers.AutomationLandmarkType returnValue);
		//void GetLocalizedLandmarkTypeCoreImpl(out string returnValue);

		// private

		//void InitializeImpl( DependencyObject pItem,  ILoopingSelectorAutomationPeer pOwner) override;

		// public
		//void RealizeImpl();

		// private
		//void ThrowElementNotAvailableException();
		//void SetParent( xaml_automation_peers.ILoopingSelectorAutomationPeer pParent);
		//void SetItem( DependencyObject pItem);
		//void GetContainerAutomationPeer(out xaml.Automation.Peers.IAutomationPeer ppContainer);

		DependencyObject _tpItem;

		//wrl.WeakRef _wrParent;
		private WeakReference<LoopingSelectorAutomationPeer> _wrParent;
		int _itemIndex;
	};
}

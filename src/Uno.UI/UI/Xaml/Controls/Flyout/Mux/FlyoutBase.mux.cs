// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\core\elements\FlyoutBase.cpp, tag winui3/release/1.4.3, commit 685d2bf

// FlyoutBase is DependencyObject and x:Name registration for
// framework objects is performed on UIElement level, flyout needs to
// have an object in core which will override EnterImpl method to register
// its children names.
// Alternative approach of moving the registration call to CDependencyObject
// was rejected because of its negative effect on performance.

using System;
using System.Linq;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Islands;

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class FlyoutBase : DependencyObject
{
	//private void EnterImpl(DependencyObject pNamescopeOwner, EnterParams parameters)
	//{
    // IFC_RETURN(CDependencyObject::EnterImpl(pNamescopeOwner, params));

    // if(HasManagedPeer())
    // {
    //     IFC_RETURN(FxCallbacks::DependencyObject_EnterImpl(
    //         this,
    //         pNamescopeOwner,
    //         params.fIsLive,
    //         params.fSkipNameRegistration,
    //         params.fCoercedIsEnabled,
    //         params.fUseLayoutRounding
    //     ));
    // }

    // return S_OK;
	//}

	//private void LeaveImpl(DependencyObject pNamescopeOwner, LeaveParams parameters)
	//{
    // IFC_RETURN(CDependencyObject::LeaveImpl(pNamescopeOwner, params));

    // if(HasManagedPeer())
    // {
    //     IFC_RETURN(FxCallbacks::DependencyObject_LeaveImpl(
    //         this,
    //         pNamescopeOwner,
    //         params.fIsLive,
    //         params.fSkipNameRegistration,
    //         params.fCoercedIsEnabled,
    //         params.fVisualTreeBeingReset
    //     ));
    // }

    // return S_OK;
	//}

	private XamlIslandRoot GetIslandContext()
	{
		if (VisualTree.GetForElement(this) is { } visualTree)
		{
			return visualTree.RootElement as XamlIslandRoot;
		}
		return null;
	}

	private void OnPlacementUpdated(MajorPlacementMode majorPlacementMode)
	{
		for (var i = m_placementUpdatedSubscribers.Count - 1; i >= 0; i--)
		{
			var subscriber = m_placementUpdatedSubscribers[i];
			var pObject = subscriber.Target;
			if (pObject?.Target is DependencyObject target)
			{
				subscriber.Callback(target, majorPlacementMode);
			}
			else
			{
				m_placementUpdatedSubscribers.RemoveAt(i);
			}
		}
	}

	private void AddPlacementUpdatedSubscriber(
		DependencyObject target,
		Action<DependencyObject, MajorPlacementMode> callback)
	{
		m_placementUpdatedSubscribers.Add(new OnPlacementUpdatedSubscriber(WeakReferencePool.RentWeakReference(this, target), callback));
	}

	private void RemovePlacementUpdatedSubscriber(DependencyObject pTarget)
	{
		var targetPos = m_placementUpdatedSubscribers.FirstOrDefault(p => p.Target.Target == pTarget);
		if (targetPos.Target != null)
		{
			m_placementUpdatedSubscribers.Remove(targetPos);
		}
	}
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\input\KeyboardAccelerator.cpp, tag winui3/release/1.4.3, commit 685d2bf

using DirectUI;
using Uno.UI.Extensions;
using Uno.UI.Xaml;
using Windows.System;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Windows.UI.Xaml.Input;

partial class KeyboardAccelerator
{
	//  Add our events to the global event request list.
	//  Need to do this ourselves since we don't derive from CUIElement.
	private void EnterImpl(
		DependencyObject pNamescopeOwner,
		EnterParams enterParams)
	{
		// base.EnterImpl(pNamescopeOwner, enterParams);

		if (enterParams.IsLive)
		{
			// TODO Uno: We currently don't require this, but may need to
			// bring it in later.
			// If there are events registered on this element, ask the
			// EventManager to extract them and create a request for every event.
			//var core = GetContext();
			//if (m_pEventList)
			//{
			//	// Get the event manager.
			//	IFCEXPECT_ASSERT_RETURN(core);
			//	CEventManager * const pEventManager = core->GetEventManager();
			//	IFCEXPECT_ASSERT_RETURN(pEventManager);
			//	IFC_RETURN(pEventManager->AddRequestsInOrder(this, m_pEventList));
			//}
#if HAS_UNO // The logic is reversed here, as DOCollection sets its parent as parent of its items.
			var parent = this.GetParentInternal(false /* publicParentOnly */);
			UIElement parentElement = null;
			KeyboardAcceleratorCollection collection = null;
			if (parent is KeyboardAcceleratorCollection kaCollection)
			{
				collection = kaCollection;
				DependencyObject pParentElement = kaCollection.GetParentInternal(false /* publicParentOnly */);
			}
			else if (parent is UIElement parentUIElement)
			{
				parentElement = parentUIElement;
				collection = (KeyboardAcceleratorCollection)parentUIElement.KeyboardAccelerators;
			}

			if (parentElement is null || collection is null)
			{
				return;
			}
#endif

			// Do not set tooltip if
			// 1. Parent element has disabled the keyboard accelerator tooltip.
			// 2. current keyboard accelerator is disabled.

			UIElement element = (UIElement)parentElement;

			var kaPlacementMode = (KeyboardAcceleratorPlacementMode)element.GetValue(UIElement.KeyboardAcceleratorPlacementModeProperty);

			if (kaPlacementMode == KeyboardAcceleratorPlacementMode.Hidden
				|| !this.IsEnabled
				|| this.Key == VirtualKey.None)

			{
				// Don't show a tooltip for an accelerator, no need to make the popup
				return;
			}

			// Create and set tooltip on parent control, only if this is the first tooltip enabled accelerator in the collection.
			foreach (DependencyObject accelerator in collection)
			{
				MUX_ASSERT(accelerator is KeyboardAccelerator);

				if (((KeyboardAccelerator)accelerator).IsEnabled)
				{
					if (accelerator == this)
					{
						FxCallbacks.KeyboardAccelerator_SetToolTip(this, parentElement);
					}
					break;
				}
			}
		}
	}
}

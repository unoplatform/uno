// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\inc\CKeyboardAcceleratorCollection.h,

using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml.Input;

internal class KeyboardAcceleratorCollection : DependencyObjectCollection<KeyboardAccelerator>
{
	public KeyboardAcceleratorCollection(DependencyObject parent) : base(parent, true)
	{
	}

	internal void Enter(DependencyObject pNamescopeOwner, EnterParams enterParams)
	{
		//base.Enter(pNamescopeOwner, enterParams);

		if (enterParams.IsLive || enterParams.IsForKeyboardAccelerator)
		{
			ContentRoot pContentRoot = VisualTree.GetContentRootForElement(this);

			// If the parent chain doesn't lead to a content root (e.g., for elements inside
			// flyout content that isn't in the visual tree), fall back to the VisualTree
			// from the EnterParams, which was resolved at the live ancestor that started the Enter walk.
			if (pContentRoot is null && enterParams.VisualTree is not null)
			{
				pContentRoot = enterParams.VisualTree.ContentRoot;
			}

			if (pContentRoot != null)
			{
				pContentRoot.AddToLiveKeyboardAccelerators(this);
			}
		}

		// In WinUI, CDOCollection::EnterImpl propagates Enter to each child.
		// Propagate to individual KeyboardAccelerators so they can set up tooltips.
		if (enterParams.IsLive)
		{
			foreach (var ka in this)
			{
				ka.EnterImpl(pNamescopeOwner, enterParams);
			}
		}
	}

	internal void Leave(DependencyObject pNamescopeOwner, LeaveParams leaveParams)
	{
		//base.Leave(pNamescopeOwner, leaveParams);

		if (leaveParams.IsLive || leaveParams.IsForKeyboardAccelerator)
		{
			ContentRoot pContentRoot = VisualTree.GetContentRootForElement(this);

			if (pContentRoot is null && leaveParams.VisualTree is not null)
			{
				pContentRoot = leaveParams.VisualTree.ContentRoot;
			}

			if (pContentRoot != null)
			{
				pContentRoot.RemoveFromLiveKeyboardAccelerators(this);
			}
		}
	}
}

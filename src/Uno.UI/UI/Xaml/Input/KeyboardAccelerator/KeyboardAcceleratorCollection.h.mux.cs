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
			if (pContentRoot != null)
			{
				pContentRoot.RemoveFromLiveKeyboardAccelerators(this);
			}
		}
	}
}

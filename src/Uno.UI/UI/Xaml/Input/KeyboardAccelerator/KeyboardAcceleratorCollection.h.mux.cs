// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\inc\CKeyboardAcceleratorCollection.h, 

using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;

namespace Windows.UI.Xaml.Input;

internal class KeyboardAcceleratorCollection : DependencyObjectCollection<KeyboardAccelerator>
{
#if HAS_UNO // TODO: Uno specific - workaround for the lack of support for Enter/Leave on DOs.
	private ParentVisualTreeListener _parentVisualTreeListener;

	public KeyboardAcceleratorCollection(DependencyObject parent) : base(parent, true)
	{
		_parentVisualTreeListener = new ParentVisualTreeListener(this);
		_parentVisualTreeListener.ParentLoaded += (s, e) => Enter(null, new EnterParams(true));
		_parentVisualTreeListener.ParentUnloaded += (s, e) => Leave(null, new LeaveParams(true));
	}
#endif

	private void Enter(DependencyObject pNamescopeOwner, EnterParams enterParams)
	{
		//base.Enter(pNamescopeOwner, enterParams);

		if (enterParams.IsLive)// || enterParams.IsForKeyboardAccelerator)
		{
			ContentRoot pContentRoot = VisualTree.GetContentRootForElement(this);
			if (pContentRoot != null)
			{
				pContentRoot.AddToLiveKeyboardAccelerators(this);
			}
		}
	}

	private void Leave(DependencyObject pNamescopeOwner, LeaveParams leaveParams)
	{
		//base.Leave(pNamescopeOwner, leaveParams);

		if (leaveParams.IsLive)// || leaveParams.IsForKeyboardAccelerator)
		{
			ContentRoot pContentRoot = VisualTree.GetContentRootForElement(this);
			if (pContentRoot != null)
			{
				pContentRoot.RemoveFromLiveKeyboardAccelerators(this);
			}
		}
	}
}

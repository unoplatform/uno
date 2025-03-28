// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\ContentRoot\FocusManagerCoreWindowAdapter.cpp, tag winui3/release/1.5.1, commit 3d10001ba8

#nullable enable

using Windows.UI.Xaml.Input;

namespace Uno.UI.Xaml.Core;

internal class FocusManagerCoreWindowAdapter : FocusAdapter
{
	public FocusManagerCoreWindowAdapter(ContentRoot contentRoot) : base(contentRoot)
	{
	}

	internal override void SetFocus()
	{
		// TODO Uno: Implement if required
	}

#if HAS_UNO // Uno specific: WinUI implementation just returns false.
	internal override bool ShouldDepartFocus(FocusNavigationDirection direction)
	{
#if __WASM__ // In case of WASM we want to depart focus when tabbing out of the root visual.
		bool isTabbingDirection = direction == FocusNavigationDirection.Next || direction == FocusNavigationDirection.Previous;

		return isTabbingDirection;
#else
		return base.ShouldDepartFocus(direction);
#endif
	}
#endif
}

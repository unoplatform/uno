// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\ContentRoot\FocusManagerXamlIslandAdapter.cpp, tag winui3/release/1.5.1, commit 3d10001ba8

#nullable enable

using Windows.UI.Xaml.Input;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Uno.UI.Xaml.Core;

internal class FocusManagerXamlIslandAdapter : FocusAdapter
{
	public FocusManagerXamlIslandAdapter(ContentRoot contentRoot) : base(contentRoot)
	{
	}

	internal override void SetFocus()
	{
		// We have moved the focus to an element hosted in an Island
		// Make sure that this island has also focus
		bool hasFocusNow = _contentRoot.XamlIslandRoot!.TrySetFocus();
		MUX_ASSERT(hasFocusNow, "Failed to move focus to xaml island");
	}

	internal override bool ShouldDepartFocus(FocusNavigationDirection direction)
	{
		bool isTabbingDirection = direction == FocusNavigationDirection.Next || direction == FocusNavigationDirection.Previous;
		bool focusScopeIsIsland = _contentRoot.XamlIslandRoot?.IsLoaded == true; // TODO Uno: Should ideally be _contentRoot.XamlIslandRoot!.IsActive();

		return isTabbingDirection && focusScopeIsIsland;
	}
}

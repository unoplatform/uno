// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class ListViewItemPresenter : ListViewBaseItemPresenter
{
	private void OnChromePropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		OnPropertyChangedNewStyle(args);

		// Uno-specific: stands in for the NWSetContentDirty render dirty flag every presenter DP carries in WinUI.
		InvalidateRender();
	}
}

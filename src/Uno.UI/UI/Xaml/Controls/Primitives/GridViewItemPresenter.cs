// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class GridViewItemPresenter : ContentPresenter
{
	protected override bool GoToElementStateCore(string stateName, bool useTransitions)
	{
		// Return false to indicate we didn't handle the state change.
		// This allows VisualStateManager to fall through and apply visual states
		// from the control template's visual state groups.
		return false;
	}
}

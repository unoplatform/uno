// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference GridViewItemPresenter_Partial.cpp, tag winui3/release/2.5.1

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class GridViewItemPresenter
{
	/// <inheritdoc />
	protected override bool GoToElementStateCore(string stateName, bool useTransitions)
	{
		GoToChromedState(
			stateName,
			useTransitions,
			out _);

		ProcessAnimationCommands();

		// Uno-specific: WinUI overwrites the chrome result with the base call (the template's own groups);
		// returning false lets Uno's VisualStateManager run those groups.
		return false;
	}
}

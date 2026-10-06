// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewItemPresenter_Partial.cpp, tag winui3/release/2.5.1

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewItemPresenter
{
	/// <inheritdoc />
	protected override bool GoToElementStateCore(string stateName, bool useTransitions)
	{
		// DBG-only WinUI code: SetRoundedListViewBaseItemChromeFallbackColors is not ported.

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

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference IKeyboardNavigationPanel.g.h, IKeyboardHeaderNavigationPanel.g.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using Microsoft.UI.Xaml.Controls;

namespace DirectUI;

internal interface IKeyboardNavigationPanel
{
	bool SupportsKeyNavigationAction(KeyNavigationAction action);

	void GetTargetIndexFromNavigationAction(
		uint sourceIndex,
		ElementType sourceType,
		KeyNavigationAction action,
		bool allowWrap,
		uint itemIndexHintForHeaderNavigation,
		out uint computedTargetIndex,
		out ElementType computedTargetType,
		out bool actionValidForSourceIndex);
}

internal interface IKeyboardHeaderNavigationPanel
{
	void GetTargetHeaderIndexFromNavigationAction(
		uint sourceIndex,
		KeyNavigationAction action,
		out uint computedTargetIndex,
		out bool actionHandled);
}

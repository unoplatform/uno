// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemPresenter_Partial.h, tag winui3/release/2.5.1

#nullable enable

using Microsoft.UI.Xaml.Media.Animation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class ListViewBaseItemPresenter : IListViewBaseItemAnimationCommandVisitor
{
	// The empty constructor and destructor are not ported; the chrome constructor lives in ListViewBaseItemChrome.mux.cs.

	private sealed class AnimationState
	{
		internal Storyboard? tpStoryboard; // Ref is stored by parent class.
		internal ListViewBaseItemAnimationCommand? pCommand;
	}

	// Private state
	private readonly AnimationState m_pointerPressedAnimation = new();
	private readonly AnimationState m_reorderHintAnimation = new();
	private readonly AnimationState m_dragDropAnimation = new();
	private readonly AnimationState m_multiSelectAnimation = new();
	private readonly AnimationState m_indicatorSelectAnimation = new();
	private readonly AnimationState m_selectionIndicatorAnimation = new();

	// Uno-specific: the Completed event registration tokens are not needed, handlers are removed by method group.
}

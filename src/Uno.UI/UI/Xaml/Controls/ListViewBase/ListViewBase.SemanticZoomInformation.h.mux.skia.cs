// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBase_Partial.h, tag winui3/release/1.8.4, commit dc46907e92

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBase
{
	private FocusState m_semanticZoomCompletedFocusState = FocusState.Programmatic;
	private bool m_semanticZoomShouldTakeFocus;
	private bool m_semanticZoomFocusQueued;
	private object? m_semanticZoomPendingFocusItem;
}

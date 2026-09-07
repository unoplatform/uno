// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference ListViewBase_Partial.h, commit 3c9c168844

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBase
{
	private FocusState m_semanticZoomCompletedFocusState = FocusState.Programmatic;
	private object? m_tpSZRequestingItem;
}

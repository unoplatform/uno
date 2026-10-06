// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItemSecondaryChrome.h, tag winui3/release/2.5.1

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Primitives;

internal sealed partial class ListViewBaseItemSecondaryChrome : FrameworkElement
{
	// Dead WinUI code: m_pEarmarkGeometryData and m_earmarkGeometryBounds belong to the removed earmark.

	// A pointer to the primary chrome.
	internal ListViewBaseItemPresenter? m_pPrimaryChromeNoRef;

	// TODO Uno: m_fFillBrushDirty, NWSetContentDirty and NWCleanDirtyFlags have no Uno dirty-flag render model.
}

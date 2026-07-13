// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml;

// TODO Uno (Stage 4): WinUI CLayoutStorage holder. The block-layout port reads the embedded
// element's measured size through pElement->GetLayoutStorage()->m_desiredSize. On Uno the layout
// fields (m_desiredSize, ...) live directly on UIElement; this placeholder mirrors the WinUI shape
// the ported BlockLayout code expects until that access is rerouted to the UIElement fields.
internal sealed class LayoutStorage
{
	internal Size m_desiredSize;
}

partial class UIElement
{
	// TODO Uno (Stage 4): UIElement.GetLayoutStorage — returns the element's layout storage
	// (CUIElement::GetLayoutStorage). Uno keeps these fields on UIElement directly; this is a stub
	// until the BlockLayout port is wired to those fields.
	internal LayoutStorage GetLayoutStorage()
		=> throw new NotSupportedException("TODO Uno (Stage 4): UIElement.GetLayoutStorage");
}

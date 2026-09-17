// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/xcp/core/native/text/Controls/RichEditBox.h, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

using Microsoft.UI.Input;

namespace Microsoft.UI.Xaml.Controls;

partial class RichEditBox
{
	private enum LinkMouseHoverTarget
	{
		None = 0,
		HyperLink,
	}

	private LinkMouseHoverTarget m_linkHoverTarget;
	private InputSystemCursorShape m_eNonHyperlinkCursor;
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference FlyoutBase_partial.h, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75

#nullable enable

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class FlyoutBase
{
	private bool m_shouldTakeFocus = true;
	private bool m_shouldHideIfPointerMovesAway;
	private bool m_shouldOverlayPassThroughAllInput;
	private bool m_ownsOverlayInputPassThroughElement;
}

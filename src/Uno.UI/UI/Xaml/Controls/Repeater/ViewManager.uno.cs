// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// Uno-specific additions to ViewManager:
// - m_gotFocus replaces the WinUI-style auto_revoke GotFocus_revoker / LostFocus_revoker
//   tokens. Uno tracks whether the subscription was already set up with a single bool flag
//   and never detaches, which is acceptable because the lifetime of ViewManager matches
//   the lifetime of its owning ItemsRepeater.

namespace Microsoft.UI.Xaml.Controls;

partial class ViewManager
{
	// Uno specific: WinUI uses auto_revoke tokens; Uno tracks subscription state with a single bool flag.
	private bool m_gotFocus;
}

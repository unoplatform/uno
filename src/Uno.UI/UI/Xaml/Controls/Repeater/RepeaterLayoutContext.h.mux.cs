// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RepeaterLayoutContext.h, commit 5f9e851133b3

using System;

namespace Microsoft.UI.Xaml.Controls;

partial class RepeaterLayoutContext
{
	// We hold a weak reference to prevent a leaking reference
	// cycle between the ItemsRepeater and its layout.
	private readonly WeakReference<ItemsRepeater> m_owner;
}

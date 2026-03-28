// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutContextAdapter.h, commit 5f9e851133b3

using System;

namespace Microsoft.UI.Xaml.Controls;

partial class LayoutContextAdapter
{
	private readonly WeakReference<NonVirtualizingLayoutContext> m_nonVirtualizingContext;
}

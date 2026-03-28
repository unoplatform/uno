// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference VirtualLayoutContextAdapter.h, commit 5f9e851133b3

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

partial class VirtualLayoutContextAdapter
{
	private readonly WeakReference<VirtualizingLayoutContext> m_virtualizingContext;

	// Note: In the C++ header, m_children is winrt::IVectorView<UIElement>.
	// In Uno, this maps to IReadOnlyList<UIElement> (backed by ChildrenCollection).
	private IReadOnlyList<UIElement> m_children;

	// The ChildrenCollection and Iterator inner classes (defined inline in the C++ header)
	// are in VirtualLayoutContextAdapter.ChildrenCollection.cs.
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference Layout.h, commit 5f9e851133b3

using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class Layout
{
	/// <summary>
	/// When overridden in a derived class, returns an
	/// <see cref="ItemCollectionTransitionProvider"/> to apply default item
	/// transitions for the layout.
	/// </summary>
	/// <returns>
	/// An <see cref="ItemCollectionTransitionProvider"/> for the default item
	/// transitions, or <c>null</c>.
	/// </returns>
	// Uno-specific: WinRT "overridable" projects accessibly to the projection;
	// in C# we widen to protected internal so ItemsRepeater (same assembly) can call it.
	protected internal virtual ItemCollectionTransitionProvider CreateDefaultItemTransitionProvider()
	{
		return null;
	}

	// TODO: This is for debugging purposes only. It should be removed when
	// the Layout.LayoutId API is removed.
	internal string m_layoutId;

	private IndexBasedLayoutOrientation m_indexBasedLayoutOrientation = IndexBasedLayoutOrientation.None;

	// Used by LayoutsTestHooks only for testing purposes.
	private int m_logItemIndexDbg = -1;
	private (int Index, double Offset) m_layoutAnchorInfoDbg = (-1, -1.0);
	private IndexBasedLayoutOrientation m_forcedIndexBasedLayoutOrientationDbg = IndexBasedLayoutOrientation.None;
	private bool m_isForcedIndexBasedLayoutOrientationSetDbg;
}

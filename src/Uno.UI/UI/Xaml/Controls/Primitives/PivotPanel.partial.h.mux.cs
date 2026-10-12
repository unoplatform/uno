// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\phone\lib\PivotPanel_Partial.h, commit 4a1c6184c

#nullable enable

using System;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class PivotPanel
{
	private double m_availableWidth;
	private double m_headerHeight;

	private WeakReference<Pivot>? m_parentPivotWeakRef;
}

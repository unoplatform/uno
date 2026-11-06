// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference IItemLookupPanel.g.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

#nullable enable

using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.Foundation;

namespace DirectUI;

internal interface IItemLookupPanel
{
	ElementInfo GetClosestElementInfo(Point position);

	int GetInsertionIndex(Point position);

	void IsLayoutBoundary(int index, out bool isLeftBoundary, out bool isTopBoundary, out bool isRightBoundary, out bool isBottomBoundary);

	Rect GetItemsBounds();
}

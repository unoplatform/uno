// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutsTestHooks.idl, winui3/release/2.5.1

#nullable enable

namespace Microsoft.UI.Private.Controls;

internal enum LinedFlowLayoutInvalidationTrigger
{
	InvalidateLayoutCall = 0,
	SnappedAverageItemsPerLineChange = 1,
	ItemDesiredWidthChange = 2,
}

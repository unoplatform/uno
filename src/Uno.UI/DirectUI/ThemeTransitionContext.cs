// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference IChildTransitionContextProvider.g.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

namespace DirectUI;

// TODO Uno: the enum definition is generated upstream and absent from the sources, so names come from its C++ usages and numeric values are not WinUI's.
internal enum ThemeTransitionContext
{
	None,
	Entrance,
	ContentTransition,
	SingleAddList,
	SingleDeleteList,
	SingleReorderList,
	SingleAddGrid,
	SingleDeleteGrid,
	SingleReorderGrid,
	MultipleAddList,
	MultipleDeleteList,
	MultipleReorderList,
	MultipleAddGrid,
	MultipleDeleteGrid,
	MultipleReorderGrid,
	MixedOperationsList,
	MixedOperationsGrid,
	ReorderedItem,
}

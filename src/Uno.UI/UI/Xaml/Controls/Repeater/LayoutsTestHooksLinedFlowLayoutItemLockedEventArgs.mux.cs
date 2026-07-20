// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs.cpp, commit b8cfb8490

namespace Microsoft.UI.Private.Controls;

partial class LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs
{
	internal LayoutsTestHooksLinedFlowLayoutItemLockedEventArgs(int itemIndex, int lineIndex)
	{
		m_itemIndex = itemIndex;
		m_lineIndex = lineIndex;
	}

	// #pragma region ILayoutsTestHooksLinedFlowLayoutItemLockedEventArgs

	internal int ItemIndex => m_itemIndex;
	internal int LineIndex => m_lineIndex;

	// #pragma endregion
}

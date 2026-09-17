// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs.cpp, commit b8cfb8490

#nullable enable

namespace Microsoft.UI.Private.Controls;

partial class LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs
{
	internal LayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs(LinedFlowLayoutInvalidationTrigger invalidationTrigger)
	{
		m_invalidationTrigger = invalidationTrigger;
	}

	// #pragma region ILayoutsTestHooksLinedFlowLayoutInvalidatedEventArgs

	internal LinedFlowLayoutInvalidationTrigger InvalidationTrigger => m_invalidationTrigger;

	// #pragma endregion
}

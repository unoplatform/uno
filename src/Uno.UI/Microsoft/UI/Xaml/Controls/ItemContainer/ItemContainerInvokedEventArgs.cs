// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ItemContainerInvokedEventArgs.cpp, tag winui3/release/1.5.0

namespace Windows.UI.Xaml.Controls;

internal partial class ItemContainerInvokedEventArgs
{
	public ItemContainerInvokedEventArgs(ItemContainerInteractionTrigger interactionTrigger, object originalSource)
	{
		InteractionTrigger = interactionTrigger;
		OriginalSource = originalSource;
	}

	public object OriginalSource { get; }

	public ItemContainerInteractionTrigger InteractionTrigger { get; }

	public bool Handled { get; set; }
}

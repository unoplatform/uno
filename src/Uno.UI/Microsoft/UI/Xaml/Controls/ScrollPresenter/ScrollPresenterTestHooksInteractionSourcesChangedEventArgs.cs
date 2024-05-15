// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Composition.Interactions;

namespace Microsoft.UI.Private.Controls;

internal class ScrollPresenterTestHooksInteractionSourcesChangedEventArgs
{
	internal ScrollPresenterTestHooksInteractionSourcesChangedEventArgs(
		CompositionInteractionSourceCollection interactionSources)
	{
		InteractionSources = interactionSources;
	}

	#region IScrollPresenterTestHooksInteractionSourcesChangedEventArgs

	internal CompositionInteractionSourceCollection InteractionSources { get; }

	#endregion
}

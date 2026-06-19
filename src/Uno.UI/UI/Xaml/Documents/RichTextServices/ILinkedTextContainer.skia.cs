// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ILinkedTextContainer.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

//---------------------------------------------------------------------------
//
//  ILinkedTextContainer
//
//  An interface used by text controls to implement linking behavior.
//
//---------------------------------------------------------------------------
internal interface ILinkedTextContainer
{
	// Gets the ILinkedTextContainer preceding this one.
	ILinkedTextContainer? GetPrevious();

	// Gets the ILinkedTextContainer following this one.
	ILinkedTextContainer? GetNext();

	// Gets the breaking information for this container.
	TextBreak? GetBreak();

	// Gets a value indicating whether the break information for this container is valid.
	bool IsBreakValid();

	// Used by a linked container to handle updates to the previous container's break record.
	Result PreviousBreakUpdated(ILinkedTextContainer pPrevious);

	// Notifies a linked container that it has been linked as the next link for another container.
	Result PreviousLinkAttached(ILinkedTextContainer pPrevious);

	// Notifies a linked container that it has been removed as the previous link for another container.
	Result NextLinkDetached(ILinkedTextContainer pNext);

	// Notifies a linked container that it has been removed as the next link for another container.
	Result PreviousLinkDetached(ILinkedTextContainer pPrevious);

	// Gets a value indicating whether this is the master control in a linked text scenario,
	// i.e. whether it is the original owner of content.
	bool IsMaster();
}

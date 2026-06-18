// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference EmbeddedElementHost.h, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Documents.RichTextServices;

/// <summary>
/// Specifies the contract a text element must implement to host embedded
/// UIElements.
/// </summary>
internal interface IEmbeddedElementHost
{
	// Adds the given embedded element to the host. Fails if the element already
	// exists in the host.
	void AddElement(InlineUIContainer container);

	// Gets a value indicating whether the host allows adding an element in its
	// current state.
	bool CanAddElement();

	// Removes the given embedded element from the host. Fails if the element
	// does not exist in the host.
	void RemoveElement(InlineUIContainer container);

	// Updates the given embedded element position. Fails if the element does
	// not exist in the host. The position is in host element space.
	void UpdateElementPosition(InlineUIContainer container, Point position);

	// Retrieves the embedded element position in host space. Fails if the
	// element does not exist in the host.
	Point GetElementPosition(InlineUIContainer container);

	// Retrieves the available size of the host. The size is used for measuring
	// embedded elements.
	Size GetAvailableMeasureSize();
}

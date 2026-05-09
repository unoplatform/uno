// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemCollectionTransition.h, commit 5f9e851133b3

using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemCollectionTransition
{
	private readonly ItemCollectionTransitionProvider _owningProvider;
	private readonly UIElement _element;
	private readonly ItemCollectionTransitionOperation _operation;
	private readonly ItemCollectionTransitionTriggers _triggers;
	private readonly Rect _oldBounds;
	private readonly Rect _newBounds;
	private ItemCollectionTransitionProgress _progress;

	internal ItemCollectionTransitionProvider OwningProvider() => _owningProvider;

	internal UIElement ElementInternal => _element;

	/// <summary>
	/// Gets the operation that is being animated.
	/// </summary>
	public ItemCollectionTransitionOperation Operation => _operation;

	/// <summary>
	/// Gets a bitwise combination of values that indicates what caused the collection transition animation to occur.
	/// </summary>
	public ItemCollectionTransitionTriggers Triggers => _triggers;

	/// <summary>
	/// Gets the visual bounds for the element prior to the move, in the case of a Move operation.
	/// </summary>
	public Rect OldBounds => _oldBounds;

	/// <summary>
	/// Gets the visual bounds for the element after the move, in the case of a Move operation.
	/// </summary>
	public Rect NewBounds => _newBounds;

	/// <summary>
	/// Gets a value that indicates whether or not an ItemCollectionTransitionProvider-derived class has started animations for this transition.
	/// </summary>
	public bool HasStarted => _progress is not null;
}

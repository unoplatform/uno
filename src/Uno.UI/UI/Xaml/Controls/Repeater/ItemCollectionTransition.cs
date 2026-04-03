// MUX Reference ItemCollectionTransition.h + ItemCollectionTransition.cpp, tag winui3/release/1.6-stable

using System.Diagnostics;
using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemCollectionTransition
{
	private ItemCollectionTransitionProvider _owningProvider;
	private UIElement _element;
	private ItemCollectionTransitionOperation _operation;
	private ItemCollectionTransitionTriggers _triggers;
	private Rect _oldBounds;
	private Rect _newBounds;
	private ItemCollectionTransitionProgress _progress;

	// Add / Remove constructor (no bounds)
	internal ItemCollectionTransition(
		ItemCollectionTransitionProvider owningProvider,
		UIElement element,
		ItemCollectionTransitionOperation operation,
		ItemCollectionTransitionTriggers triggers)
		: this(owningProvider, element, operation, triggers, default, default)
	{
		Debug.Assert(operation != ItemCollectionTransitionOperation.Move);
	}

	// Move constructor (with old/new bounds)
	internal ItemCollectionTransition(
		ItemCollectionTransitionProvider owningProvider,
		UIElement element,
		ItemCollectionTransitionTriggers triggers,
		Rect oldBounds,
		Rect newBounds)
		: this(owningProvider, element, ItemCollectionTransitionOperation.Move, triggers, oldBounds, newBounds)
	{
	}

	private ItemCollectionTransition(
		ItemCollectionTransitionProvider owningProvider,
		UIElement element,
		ItemCollectionTransitionOperation operation,
		ItemCollectionTransitionTriggers triggers,
		Rect oldBounds,
		Rect newBounds)
	{
		_owningProvider = owningProvider;
		_element = element;
		_operation = operation;
		_triggers = triggers;
		_oldBounds = oldBounds;
		_newBounds = newBounds;
	}

	internal ItemCollectionTransitionProvider OwningProvider() => _owningProvider;

	// Internal accessor used by ItemCollectionTransitionProgress and ItemCollectionTransitionCompletedEventArgs
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

	/// <summary>
	/// Notifies the ItemCollectionTransitionProvider that this transition will be animated.
	/// </summary>
	/// <returns>An <see cref="ItemCollectionTransitionProgress"/> object that can be used to notify when the transition animation is complete.</returns>
	public ItemCollectionTransitionProgress Start()
	{
		if (_progress is null)
		{
			_progress = new ItemCollectionTransitionProgress(this);
		}

		return _progress;
	}
}

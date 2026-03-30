// MUX Reference ItemCollectionTransition.cpp, tag winui3/release/1.6-stable

using Microsoft.UI.Xaml;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemCollectionTransition
{
	// Add / Remove constructor (no bounds)
	internal ItemCollectionTransition(
		ItemCollectionTransitionProvider owningProvider,
		UIElement element,
		ItemCollectionTransitionOperation operation,
		ItemCollectionTransitionTriggers triggers)
		: this(owningProvider, element, operation, triggers, default, default)
	{
		MUX_ASSERT(operation != ItemCollectionTransitionOperation.Move);
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

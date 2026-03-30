// MUX Reference ItemCollectionTransition.h, tag winui3/release/1.6-stable

using Microsoft.UI.Xaml;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemCollectionTransition
{
	private ItemCollectionTransitionProvider _owningProvider;
	private UIElement _element;
	private ItemCollectionTransitionOperation _operation;
	private ItemCollectionTransitionTriggers _triggers;
	private Rect _oldBounds;
	private Rect _newBounds;
	private ItemCollectionTransitionProgress _progress;

	internal ItemCollectionTransitionProvider OwningProvider() => _owningProvider;

	// Internal accessor used by ItemCollectionTransitionProgress and ItemCollectionTransitionCompletedEventArgs.
	// Element() is not part of the public WinUI IDL surface; it is only used internally in C++.
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

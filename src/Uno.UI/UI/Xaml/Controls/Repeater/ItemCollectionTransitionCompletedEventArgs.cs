// MUX Reference ItemCollectionTransitionCompletedEventArgs.h + ItemCollectionTransitionCompletedEventArgs.cpp, tag winui3/release/1.6-stable

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemCollectionTransitionCompletedEventArgs
{
	private ItemCollectionTransition _transition;

	internal ItemCollectionTransitionCompletedEventArgs(ItemCollectionTransition transition)
	{
		_transition = transition;
	}

	/// <summary>
	/// Gets the ItemCollectionTransition whose animations have completed.
	/// </summary>
	public ItemCollectionTransition Transition => _transition;

	/// <summary>
	/// Gets the UIElement that was transitioned.
	/// </summary>
	public UIElement Element => _transition?.ElementInternal;
}

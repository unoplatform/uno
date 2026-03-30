// MUX Reference ItemCollectionTransitionProgress.h + ItemCollectionTransitionProgress.cpp, tag winui3/release/1.6-stable

using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls;

public partial class ItemCollectionTransitionProgress
{
	// Original C++ uses weak_ref<ItemCollectionTransition>.
	// In .NET the GC handles circular references, so a plain reference is used here.
	// ItemCollectionTransition holds a strong ref to this; using strong ref back does not leak in .NET.
	private ItemCollectionTransition _transition;

	internal ItemCollectionTransitionProgress(ItemCollectionTransition transition)
	{
		_transition = transition;
	}

	/// <summary>
	/// Gets the ItemCollectionTransition whose animations are in progress.
	/// </summary>
	public ItemCollectionTransition Transition => _transition;

	/// <summary>
	/// Gets the UIElement to be animated.
	/// </summary>
	public UIElement Element
	{
		get
		{
			if (_transition is not null)
			{
				return _transition.ElementInternal;
			}

			return null;
		}
	}

	/// <summary>
	/// Raises the TransitionCompleted event on the ItemCollectionTransitionProvider that owns the associated ItemCollectionTransition.
	/// </summary>
	public void Complete()
	{
		if (_transition is not null)
		{
			var transitionProvider = _transition.OwningProvider();
			transitionProvider?.NotifyTransitionCompleted(_transition);
		}
	}
}

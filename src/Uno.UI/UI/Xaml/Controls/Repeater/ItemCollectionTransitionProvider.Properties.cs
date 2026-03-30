// MUX Reference ItemCollectionTransitionProvider.properties.h + ItemCollectionTransitionProvider.properties.cpp,
//             tag winui3/release/1.6-stable

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ItemCollectionTransitionProvider
{
	/// <summary>
	/// Occurs when the ItemCollectionTransitionProgress.Complete method is called to indicate that a transition on a specific UIElement has completed.
	/// </summary>
	public event TypedEventHandler<ItemCollectionTransitionProvider, ItemCollectionTransitionCompletedEventArgs> TransitionCompleted;
}

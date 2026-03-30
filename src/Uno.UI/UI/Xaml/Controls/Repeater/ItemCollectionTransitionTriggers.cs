// MUX Reference (generated), tag winui3/release/1.6-stable

using System;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify what caused the collection transition animation to occur.
/// </summary>
[Flags]
public enum ItemCollectionTransitionTriggers : uint
{
	/// <summary>
	/// A data object was added to a collection.
	/// </summary>
	CollectionChangeAdd = 1,

	/// <summary>
	/// A data object was removed from a collection.
	/// </summary>
	CollectionChangeRemove = 2,

	/// <summary>
	/// A collection of data objects was significantly changed; for example, it was reset to empty.
	/// </summary>
	CollectionChangeReset = 4,

	/// <summary>
	/// A layout transition occurred; for example, the window was resized.
	/// </summary>
	LayoutTransition = 8,
}

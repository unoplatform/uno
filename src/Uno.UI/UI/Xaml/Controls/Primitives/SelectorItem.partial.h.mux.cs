// MUX Reference SelectorItem_Partial.h, tag winui3/release/1.8.1

using System;

namespace Microsoft.UI.Xaml.Controls.Primitives;

partial class SelectorItem
{
	// Weak reference to the Selector that contains this SelectorItem.
	private WeakReference<Selector> m_wrParentSelector;

	// A value indicating whether the SelectorItem's content is
	// currently a ui virtualized placeholder that will be replaced with an
	// actual value once it has been set asynchronously.
	// This almost matches the data placeholder state (m_isPlaceholder) but not quite
	// since in this case the data that is set cannot be used as an indicator.
	private bool m_isUIPlaceholder;

	// A value indicating whether the SelectorItem's content is
	// currently a data virtualized placeholder that will be replaced
	// with an actual value once it has been retrieved.
	protected internal bool m_isPlaceholder;

	// Constructor initializes:
	// m_isPlaceholder = false (default)
	// m_isUIPlaceholder = false (default)

#if HAS_UNO
	// TODO Uno: Original C++ destructor was empty. No cleanup needed.
	// Uno does not support cleanup via finalizers.
#endif

	/// <summary>
	/// Allows ItemContainerGenerator to know whether this container was considered to be a placeholder.
	/// </summary>
	internal bool GetIsPlaceholder() => m_isPlaceholder;

	/// <summary>
	/// Allows ItemsControl to indicate that the current data is not truly reflective of real data.
	/// </summary>
	internal void SetIsUIPlaceholder(bool isPlaceholder) => m_isUIPlaceholder = isPlaceholder;

	/// <summary>
	/// Gets whether the item is a UI placeholder.
	/// </summary>
	internal bool GetIsUIPlaceholder() => m_isUIPlaceholder;
}

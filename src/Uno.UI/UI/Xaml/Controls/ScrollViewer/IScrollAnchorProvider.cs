#nullable enable

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Specifies a contract for a scrolling control that supports scroll anchoring.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Scroll anchoring is when a scrolling control automatically changes the position of its
	/// viewport to prevent the content from visibly jumping. The jump is caused by a change in
	/// the content's layout. The scroll anchor provider applies a shift after observing a change
	/// in the position of an anchor element within the content.
	/// </para>
	/// <para>
	/// It's the responsibility of the implementing scroll control to determine what policy it
	/// will use in choosing a <see cref="CurrentAnchor"/> from the set of registered candidates.
	/// </para>
	/// </remarks>
	public partial interface IScrollAnchorProvider
	{
		/// <summary>
		/// Gets the currently chosen anchor element to use for scroll anchoring.
		/// </summary>
		/// <value>
		/// The most recently chosen <see cref="UIElement"/> for scroll anchoring after a layout pass,
		/// or <c>null</c>. If there are no anchor candidates registered with the
		/// <see cref="IScrollAnchorProvider"/> or none have been chosen, then CurrentAnchor is <c>null</c>.
		/// </value>
		UIElement? CurrentAnchor { get; }

		/// <summary>
		/// Registers a <see cref="UIElement"/> as a potential scroll anchor candidate.
		/// </summary>
		/// <param name="element">
		/// A <see cref="UIElement"/> within the subtree of the <see cref="IScrollAnchorProvider"/>.
		/// </param>
		/// <remarks>
		/// In the current Uno implementation, callers must invoke this method explicitly. Automatic
		/// registration driven by <see cref="UIElement.CanBeScrollAnchor"/> is not yet wired up
		/// (see <c>UpdateAnchorCandidateOnParentScrollProvider</c> in <c>UIElement.mux.cs</c>).
		/// On WinUI, when an element has <see cref="UIElement.CanBeScrollAnchor"/> set to <c>true</c>,
		/// the framework locates the first <see cref="IScrollAnchorProvider"/> in that element's chain
		/// of ancestors and automatically calls its <see cref="RegisterAnchorCandidate"/> method,
		/// both when the property is set on an existing element and when an element is added to the
		/// live tree with the property already set.
		/// </remarks>
		void RegisterAnchorCandidate(UIElement element);

		/// <summary>
		/// Unregisters a <see cref="UIElement"/> as a potential scroll anchor candidate.
		/// </summary>
		/// <param name="element">
		/// A <see cref="UIElement"/> within the subtree of the <see cref="IScrollAnchorProvider"/>.
		/// </param>
		/// <remarks>
		/// In the current Uno implementation, callers must invoke this method explicitly. Automatic
		/// unregistration driven by <see cref="UIElement.CanBeScrollAnchor"/> (or by an element
		/// leaving the visual tree) is not yet wired up. On WinUI, when the property changes to
		/// <c>false</c> (or the element is removed from the visual tree), the framework locates the
		/// first <see cref="IScrollAnchorProvider"/> in that element's chain of ancestors and
		/// automatically calls its <see cref="UnregisterAnchorCandidate"/> method.
		/// </remarks>
		void UnregisterAnchorCandidate(UIElement element);
	}
}

namespace Windows.UI.Xaml.Automation.Provider
{
	/// <summary>
	/// Exposes methods and properties to support access by a Microsoft UI Automation client to individual,
	/// selectable child controls of containers that implement ISelectionProvider. Implement this interface
	/// in order to support the capabilities that an automation client requests with a GetPattern
	/// call and PatternInterface.SelectionItem.
	/// </summary>
	public partial interface ISelectionItemProvider
	{
		/// <summary>
		/// Gets a value that indicates whether an item is selected.
		/// </summary>
		bool IsSelected { get; }

		/// <summary>
		/// Gets the UI Automation provider that implements ISelectionProvider and acts
		/// as the container for the calling object.
		/// </summary>
		IRawElementProviderSimple SelectionContainer { get; }

		/// <summary>
		/// Adds the current element to the collection of selected items.
		/// </summary>
		void AddToSelection();

		/// <summary>
		/// Removes the current element from the collection of selected items.
		/// </summary>
		void RemoveFromSelection();

		/// <summary>
		/// Clears any existing selection and then selects the current element.
		/// </summary>
		void Select();
	}
}

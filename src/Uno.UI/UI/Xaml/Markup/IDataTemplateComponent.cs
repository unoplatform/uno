namespace Windows.UI.Xaml.Markup;

/// <summary>
/// Provides methods that enable the XAML parser to communicate with generated binding code.
/// </summary>
public partial interface IDataTemplateComponent
{
	/// <summary>
	/// Disassociates item containers from their data items and saves the containers so they can be reused later for other data items.
	/// </summary>
	void Recycle();

	/// <summary>
	/// Updates the compiled data bindings.
	/// </summary>
	/// <param name="item">The data item.</param>
	/// <param name="itemIndex">The index of the data item.</param>
	/// <param name="phase">The number of times ProcessBindings has been called.</param>
	/// <param name="nextPhase">The phase on the next call to ProcessBindings.</param>
	void ProcessBindings(object item, int itemIndex, int phase, out int nextPhase);
}

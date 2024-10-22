
using Windows.Foundation.Collections;

namespace Microsoft.UI.Xaml.Data;

/// <summary>
/// Represents any grouped items within a view.
/// </summary>
public partial interface ICollectionViewGroup
{
	/// <summary>
	/// Gets or sets the grouping context used for grouping the data, which sets the data context for the default HeaderTemplate.
	/// </summary>
	object Group { get; }

	/// <summary>
	/// Gets the collection of grouped items that this ICollectionViewGroup implementation represents.
	/// </summary>
	IObservableVector<object> GroupItems { get; }
}

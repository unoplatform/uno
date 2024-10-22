namespace Microsoft.UI.Xaml.Data;

/// <summary>
/// Supports creation of the relevant ICollectionView implementation.
/// </summary>
public partial interface ICollectionViewFactory
{
	/// <summary>
	/// Creates an ICollectionView instance using default settings.
	/// </summary>
	/// <returns>The default view.</returns>
	ICollectionView CreateView();
}

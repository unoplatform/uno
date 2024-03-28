namespace Windows.UI.Xaml.Automation.Provider;

/// <summary>
/// Provides access to the underlying object model implemented by a control or app.
/// </summary>
public partial interface IObjectModelProvider
{
	/// <summary>
	/// Returns an interface used to access the underlying object model of the provider.
	/// </summary>
	/// <returns>An untyped interface for accessing the underlying object model.</returns>
	object GetUnderlyingObjectModel();
}

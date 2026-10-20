using Windows.Foundation;

namespace Microsoft.Windows.ApplicationModel.Resources;

/// <summary>
/// The interface that is implemented by the ResourceManager class,
/// which provides access to app resource maps and more advanced resource functionality.
/// </summary>
public partial interface IResourceManager
{
	/// <summary>
	/// Gets the ResourceMap that is associated with the main package of the currently running app.
	/// </summary>
	ResourceMap MainResourceMap { get; }

	/// <summary>
	/// Creates a ResourceContext with the default settings.
	/// </summary>
	/// <returns>A ResourceContext.</returns>
	ResourceContext CreateResourceContext();

	/// <summary>
	/// Occurs when an attempt to get a resource fails because the specified resource was not found.
	/// </summary>
	event TypedEventHandler<ResourceManager, ResourceNotFoundEventArgs> ResourceNotFound;
}

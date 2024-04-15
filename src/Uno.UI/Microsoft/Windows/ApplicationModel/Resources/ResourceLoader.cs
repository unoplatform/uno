#if HAS_UNO_WINUI
using WinRTResourceLoader = global::Windows.ApplicationModel.Resources.ResourceLoader;

namespace Microsoft.Windows.ApplicationModel.Resources;

/// <summary>
/// Provides simplified access to app resources such as app UI strings.
/// </summary>
public partial class ResourceLoader
{
	private readonly WinRTResourceLoader _resourceLoader;

	/// <summary>
	/// Constructs a new ResourceLoader object for the "Resources" subtree of the currently running app's main ResourceMap.
	/// </summary>
	public ResourceLoader() => _resourceLoader = new();

	/// <summary>
	/// Constructs a new ResourceLoader object for the specified ResourceMap.
	/// </summary>
	/// <param name="fileName">
	/// The resource identifier of the ResourceMap that the new resource loader
	/// uses for unqualified resource references. It can then retrieve resources
	/// relative to those references.
	/// </param>
	public ResourceLoader(string fileName) => _resourceLoader = new(fileName);

	/// <summary>
	/// Returns the most appropriate string value of a resource, specified by resource identifier.
	/// </summary>
	/// <param name="resourceId">The resource identifier of the resource to be resolved.</param>
	/// <returns>The most appropriate string value of the specified resource for the default ResourceContext.</returns>
	public string GetString(string resourceId) => _resourceLoader.GetString(resourceId);
}
#endif

using System.Collections.Generic;
using System.Linq;

namespace Windows.ApplicationModel.Resources.Core;

/// <summary>
/// Provides access to application resource maps and more advanced resource functionality.
/// </summary>
public partial class ResourceManager 
{
	private Dictionary<string, ResourceMap> _resourceMaps;

	private ResourceManager() { }

	/// <summary>
	/// Gets the ResourceManager for the currently running application.
	/// </summary>
	public static ResourceManager Current { get; } = new ResourceManager();

	/// <summary>
	/// Gets the default ResourceContext for the currently running application.
	/// Unless explicitly overridden, the default ResourceContext is used to determine
	/// the most appropriate representation of any given named resource.
	/// </summary>
	public ResourceContext DefaultContext { get; } = new ResourceContext();

	public IReadOnlyDictionary<string, ResourceMap> AllResourceMaps => new ResourceMapMapView(_resourceMaps.AsReadOnly());

	public ResourceMap MainResourceMap
	{
		get
		{
			throw new global::System.NotImplementedException("The member ResourceMap ResourceManager.MainResourceMap is not implemented in Uno.");
		}
	}

	public  void LoadPriFiles( global::System.Collections.Generic.IEnumerable<global::Windows.Storage.IStorageFile> files)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceManager", "void ResourceManager.LoadPriFiles(IEnumerable<IStorageFile> files)");
	}
	public  void UnloadPriFiles( global::System.Collections.Generic.IEnumerable<global::Windows.Storage.IStorageFile> files)
	{
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.ApplicationModel.Resources.Core.ResourceManager", "void ResourceManager.UnloadPriFiles(IEnumerable<IStorageFile> files)");
	}
	public  global::System.Collections.Generic.IReadOnlyList<NamedResource> GetAllNamedResourcesForPackage( string packageName,  ResourceLayoutInfo resourceLayoutInfo)
	{
		throw new global::System.NotImplementedException("The member IReadOnlyList<NamedResource> ResourceManager.GetAllNamedResourcesForPackage(string packageName, ResourceLayoutInfo resourceLayoutInfo) is not implemented in Uno.");
	}

	public  global::System.Collections.Generic.IReadOnlyList<ResourceMap> GetAllSubtreesForPackage( string packageName,  ResourceLayoutInfo resourceLayoutInfo)
	{
		throw new global::System.NotImplementedException("The member IReadOnlyList<ResourceMap> ResourceManager.GetAllSubtreesForPackage(string packageName, ResourceLayoutInfo resourceLayoutInfo) is not implemented in Uno.");
	}
	public static bool IsResourceReference( string resourceReference)
	{
		throw new global::System.NotImplementedException("The member bool ResourceManager.IsResourceReference(string resourceReference) is not implemented in Uno.");
	}
}

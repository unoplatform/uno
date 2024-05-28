using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.Phone.PersonalInformation;

namespace Windows.Storage;

/// <summary>
/// Represents a container for app settings. The methods and properties of this class support 
/// creating, deleting, enumerating, and traversing the container hierarchy.
/// </summary>
/// <remarks>
/// Settings are stored in platform-specific preference stores. Some keys are used internally by Uno Platform,
/// and are not surfaced via the public API. These keys are prefixed with "__".
/// To provide the concept of nested containers, we use the "__/" prefix in key names as the container path separator.
/// </remarks>
public partial class ApplicationDataContainer : IDisposable
{
	private const string ContainerPathSeparator = "__/";

	private Lazy<Dictionary<string, ApplicationDataContainer>> _containers = new(CreateContainersDictionary);
	private ApplicationDataContainer _parent;

	internal ApplicationDataContainer(ApplicationData owner, string name, ApplicationDataLocality locality)
	{
		Locality = locality;
		Name = name;

		Values = new ApplicationDataContainerSettings(this, locality);
	}

	internal ApplicationDataContainer(ApplicationDataContainer parent, string name)
	{
		_parent = parent ?? throw new ArgumentNullException(nameof(parent));
		Name = name;
		Locality = parent.Locality;
	}

	internal string ContainerPath => _parent is null ? "" : _parent.ContainerPath + ContainerPathSeparator + Name;

	public ApplicationDataLocality Locality { get; }

	public string Name { get; }

	public IPropertySet Values { get; private set; }

	public IReadOnlyDictionary<string, ApplicationDataContainer> Containers => _containers.Value.AsReadOnly();

	private static Dictionary<string, ApplicationDataContainer> CreateContainersDictionary()
	{
		return new Dictionary<string, ApplicationDataContainer>();
	}

	public ApplicationDataContainer CreateContainer(string name, ApplicationDataCreateDisposition disposition)
	{
		var containers = Containers;
		if (disposition == ApplicationDataCreateDisposition.Existing)
		{

		}
	}

	public void DeleteContainer(string name)
	{
		_containers.Value.Remove(name);
	}

	public void Dispose() => DisposePartial();

	partial void DisposePartial();
}

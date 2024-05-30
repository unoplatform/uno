using System;
using System.Collections.Generic;
using Uno.Storage;
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
	private const string InternalSettingPrefix = "__";

	private const string ContainerPathSeparator = "¬";

	private readonly Lazy<Dictionary<string, ApplicationDataContainer>> _containers;
	private readonly NativeApplicationSettings _nativeApplicationSettings;
	private readonly ApplicationDataContainerSettings _values;
	private readonly ApplicationDataContainer _parent;

	internal ApplicationDataContainer(string name, ApplicationDataLocality locality)
	{
		Locality = locality;
		Name = name;

		_nativeApplicationSettings = NativeApplicationSettings.GetForLocality(locality);
		_values = new ApplicationDataContainerSettings(this, locality);
		_containers = new(() => CreateContainersDictionary());
	}

	internal ApplicationDataContainer(ApplicationDataContainer parent, string name) : this(name, parent.Locality)
	{
		_parent = parent ?? throw new ArgumentNullException(nameof(parent));
	}

	internal string ContainerPath => _parent is null ? "" : _parent.ContainerPath + ContainerPathSeparator + Name;

	public ApplicationDataLocality Locality { get; }

	public string Name { get; }

	public IPropertySet Values => _values;

	public IReadOnlyDictionary<string, ApplicationDataContainer> Containers => _containers.Value.AsReadOnly();

	private Dictionary<string, ApplicationDataContainer> CreateContainersDictionary()
	{
		var keysWithPrefix = _nativeApplicationSettings.Select(kvp => kvp.Key).Where(k => k.StartsWith(ContainerPath + ContainerPathSeparator));
		return new Dictionary<string, ApplicationDataContainer>();
	}

	public ApplicationDataContainer CreateContainer(string name, ApplicationDataCreateDisposition disposition)
	{
		var containers = Containers;
		if (disposition == ApplicationDataCreateDisposition.Existing)
		{
			var applicationDataContainer = new ApplicationDataContainer(this, name);
		}
	}

	public void DeleteContainer(string name)
	{
		if (!_containers.Value.ContainsKey(name))
		{
			throw new KeyNotFoundException("Container does not exist.");
		}
		_containers.Value.Remove(name);
	}

	public void Dispose() => DisposePartial();

	partial void DisposePartial();
}

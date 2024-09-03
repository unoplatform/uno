using System;
using System.Collections.Generic;
using Uno.Storage;
using Windows.Foundation.Collections;

namespace Windows.Storage;

/// <summary>
/// Represents a container for app settings. The methods and properties of this class support 
/// creating, deleting, enumerating, and traversing the container hierarchy.
/// </summary>
/// <remarks>
/// Settings are stored in platform-specific preference stores. Some keys are used internally by Uno Platform,
/// and are not surfaced via the public API. These keys are prefixed with "__".
/// To provide the concept of nested containers, we use the "__¬" prefix in key names as the container path separator.
/// </remarks>
public partial class ApplicationDataContainer : IDisposable
{
	private const string InternalSettingPrefix = "__";
	private const string ContainerSeparator = "¬";
	private const string ContainerListKey = InternalSettingPrefix + "UnoContainers";

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

	internal string ContainerPath => _parent is null ? "" : _parent.ContainerPath + InternalSettingPrefix + Name + ContainerSeparator;

	public ApplicationDataLocality Locality { get; }

	public string Name { get; }

	public IPropertySet Values => _values;

	public IReadOnlyDictionary<string, ApplicationDataContainer> Containers => _containers.Value.AsReadOnly();

	private Dictionary<string, ApplicationDataContainer> CreateContainersDictionary()
	{
		var containers = new Dictionary<string, ApplicationDataContainer>();
		var prefix = ContainerPath + InternalSettingPrefix;
		var keysWithPrefix = _nativeApplicationSettings.GetKeysWithPrefix(prefix);
		foreach (var key in keysWithPrefix)
		{
			var relativeKey = key.AsSpan(prefix.Length);
			if (relativeKey.IndexOf(ContainerSeparator) is { } separatorIndex && separatorIndex == relativeKey.Length - 1)
			{
				var containerName = relativeKey.Slice(0, relativeKey.Length - 1).ToString();
				var container = new ApplicationDataContainer(this, containerName);
				containers.Add(containerName, container);
			}
		}

		return containers;
	}

	public ApplicationDataContainer CreateContainer(string name, ApplicationDataCreateDisposition disposition)
	{
		if (name.Contains(ContainerSeparator))
		{
			throw new InvalidOperationException($"Container names may not contain the '{ContainerSeparator}' character.");
		}

		var containers = _containers.Value;

		if (containers.TryGetValue(name, out var container))
		{
			return container;
		}
		else if (disposition == ApplicationDataCreateDisposition.Existing)
		{
			throw new KeyNotFoundException("Container does not exist.");
		}
		else
		{
			var newContainer = new ApplicationDataContainer(this, name);
			containers.Add(name, newContainer);

			// Add a container marker entry to the settings store
			AddContainerToList(name);

			return newContainer;
		}
	}

	public void DeleteContainer(string name)
	{
		if (name.Contains(ContainerSeparator))
		{
			throw new InvalidOperationException($"Container names may not contain the '{ContainerSeparator}' character.");
		}

		if (!_containers.Value.TryGetValue(name, out var container))
		{
			throw new KeyNotFoundException("Container does not exist.");
		}

		container.ClearIncludingInternals();

		// Remove the container marker entry from the settings store
		RemoveContainerFromList(name);

		_containers.Value.Remove(name);
	}

	internal void ClearIncludingInternals()
	{
		Clear();
		_nativeApplicationSettings.RemoveKeysWithPrefix(InternalSettingPrefix);
	}

	private void AddContainerToList(string containerName)
	{
		var containerList = _nativeApplicationSettings.Get(ContainerPath + ContainerListKey) ?? "";
		if (containerList.Length > 0)
		{
			containerList += ContainerListSeparator;
		}

		containerList += containerName;
		_nativeApplicationSettings.Set(ContainerPath + ContainerListKey) = containerList;
	}

	private void RemoveContainerFromList(string containerName)
	{
		var containerList = _nativeApplicationSettings.Get(ContainerPath + ContainerListKey) ?? "";
		var containerListParts = containerList.Split(ContainerSeparator);
		var newContainerList = string.Join(ContainerSeparator, containerListParts.Where(c => c != containerName));
		_nativeApplicationSettings.Set(ContainerPath + ContainerListKey) = newContainerList;
	}

	public void Dispose() => DisposePartial();

	partial void DisposePartial();
}

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Storage;
using Windows.Foundation.Collections;

namespace Windows.Storage;

/// <summary>
/// Represents a container for app settings. The methods and properties of this class support 
/// creating, deleting, enumerating, and traversing the container hierarchy.
/// </summary>
/// <remarks>
/// Settings are stored in platform-specific preference stores. Keys carrying the reserved
/// <see cref="InternalSettingPrefix"/> are used internally by Uno Platform — the container list, the app data
/// version, and the keys of nested containers, which are stored under a "{prefix}{name}{separator}" path — and
/// are never surfaced through the public API.
/// The prefix and separator are private-use code points: they are reserved to Uno Platform, cannot collide with
/// a key an application would realistically store, and survive a round-trip through every native settings store.
/// </remarks>
public sealed partial class ApplicationDataContainer : IDisposable
{
	internal const string InternalSettingPrefix = "\uE000";
	private const string ContainerSeparator = "\uE001";
	internal const string ContainerListKey = InternalSettingPrefix + "UnoContainers";
	internal const string VersionSettingKey = InternalSettingPrefix + "ApplicationDataVersion";

	private readonly Lazy<Dictionary<string, ApplicationDataContainer>> _containers;
	private readonly NativeApplicationSettings _nativeApplicationSettings;
	private readonly ApplicationDataContainerSettings _values;
	private readonly ApplicationDataContainer? _parent;

	internal ApplicationDataContainer(string name, ApplicationDataLocality locality)
	{
		Locality = locality;
		Name = name;

		_nativeApplicationSettings = NativeApplicationSettings.GetForLocality(locality);
		_values = new ApplicationDataContainerSettings(this, locality);
		_containers = new(CreateContainersDictionary);
	}

	internal ApplicationDataContainer(ApplicationDataContainer parent, string name) : this(name, parent.Locality)
	{
		_parent = parent ?? throw new ArgumentNullException(nameof(parent));
	}

	internal string ContainerPath => _parent is null ? "" : _parent.ContainerPath + InternalSettingPrefix + Name + ContainerSeparator;

	/// <summary>
	/// Maps a key, as the application sees it, to the key the settings store is addressed with.
	/// </summary>
	/// <remarks>
	/// A null key would silently resolve to <see cref="ContainerPath"/> itself — the container's own bookkeeping
	/// slot — so it is rejected here, at the single point every public settings API funnels through.
	/// </remarks>
	internal string GetSettingKey(string key)
	{
		if (key is null)
		{
			throw new ArgumentNullException(nameof(key));
		}

		return ContainerPath + key;
	}

	/// <summary>
	/// Determines whether a key, relative to a container, is owned by Uno Platform rather than by the application.
	/// </summary>
	/// <param name="relativeKey">The key, with the owning container's path already removed.</param>
	internal static bool IsInternalKey(string relativeKey) =>
		relativeKey.StartsWith(InternalSettingPrefix, StringComparison.Ordinal);

	/// <summary>
	/// Container names take part in the key path, so they may not carry the code points reserved to it.
	/// </summary>
	/// <remarks>
	/// An empty name is rejected too: it is indistinguishable from an empty slot in the persisted container list,
	/// so such a container would not be found again after a restart.
	/// </remarks>
	private static void ValidateContainerName(string name)
	{
		if (name is null)
		{
			throw new ArgumentNullException(nameof(name));
		}

		if (name.Length == 0)
		{
			throw new ArgumentException("Container names may not be empty.", nameof(name));
		}

		if (name.Contains(ContainerSeparator, StringComparison.Ordinal) ||
			name.StartsWith(InternalSettingPrefix, StringComparison.Ordinal))
		{
			throw new InvalidOperationException("Container names may not contain the characters reserved by Uno Platform.");
		}
	}

	public ApplicationDataLocality Locality { get; }

	public string Name { get; }

	public IPropertySet Values => _values;

	public IReadOnlyDictionary<string, ApplicationDataContainer> Containers => _containers.Value.AsReadOnly();

	private Dictionary<string, ApplicationDataContainer> CreateContainersDictionary()
	{
		var containers = new Dictionary<string, ApplicationDataContainer>();
		var containerList = _nativeApplicationSettings[ContainerPath + ContainerListKey] as string ?? "";
		if (containerList.Length > 0)
		{
			foreach (var containerName in containerList.Split(ContainerSeparator))
			{
				// The list is tolerated rather than trusted: an app upgrading from an earlier Uno Platform
				// version may already own this key, and a duplicate entry must not throw on every access.
				if (containerName.Length > 0 && !containers.ContainsKey(containerName))
				{
					containers.Add(containerName, new ApplicationDataContainer(this, containerName));
				}
			}
		}

		return containers;
	}

	public ApplicationDataContainer CreateContainer(string name, ApplicationDataCreateDisposition disposition)
	{
		ValidateContainerName(name);

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
		ValidateContainerName(name);

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
		DeleteAllSubcontainers();
		_nativeApplicationSettings.RemoveKeysWithPrefix(ContainerPath);
	}

	internal void DeleteAllSubcontainers()
	{
		foreach (var containerName in Containers.Keys.ToList())
		{
			DeleteContainer(containerName);
		}
	}

	private void AddContainerToList(string containerName)
	{
		var containerList = _nativeApplicationSettings[ContainerPath + ContainerListKey] as string ?? "";
		if (containerList.Length > 0)
		{
			containerList += ContainerSeparator;
		}

		containerList += containerName;
		_nativeApplicationSettings[ContainerPath + ContainerListKey] = containerList;
	}

	private void RemoveContainerFromList(string containerName)
	{
		var containerList = _nativeApplicationSettings[ContainerPath + ContainerListKey] as string ?? "";
		var containerListParts = containerList.Split(ContainerSeparator);
		var newContainerList = string.Join(ContainerSeparator, containerListParts.Where(c => c != containerName));
		_nativeApplicationSettings[ContainerPath + ContainerListKey] = newContainerList;
	}

	public void Dispose() => DisposePartial();

	partial void DisposePartial();
}

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Uno.UI.SourceGenerators.XamlGenerator;

public record ResourceDetails(string Assembly, string FileName, string Key);

/// <summary>
/// Resources management and lookup
/// </summary>
internal class ResourceDetailsCollection
{
	private readonly string _localAssemblyName;

	private readonly Dictionary<string, Dictionary<string, ResourceDetails>> _resourcesByFileName = new();
	private readonly Dictionary<string, Dictionary<string, List<ResourceDetails>>> _resourcesByFileNameThenUid = new();

	public ResourceDetailsCollection(string localAssemblyName)
	{
		_localAssemblyName = localAssemblyName;
	}

	public bool HasLocalResources { get; internal set; }

	/// <summary>
	/// Adds resources to the current collection
	/// </summary>
	public void AddRange(ResourceDetails[] resources)
	{
		foreach (var resource in resources)
		{
			if (resource.Assembly == _localAssemblyName)
			{
				HasLocalResources = true;
			}

			// Resources by file name
			if (!_resourcesByFileName.TryGetValue(resource.FileName, out var fileResources))
			{
				_resourcesByFileName[resource.FileName] = fileResources = new();
			}

			fileResources[resource.Key] = resource;

			// Resource by Uid
			if (!_resourcesByFileNameThenUid.TryGetValue(resource.FileName, out var partialResources))
			{
				_resourcesByFileNameThenUid[resource.FileName] = partialResources = new();
			}

			var memberIndex = resource.Key.IndexOf('.');

			if (memberIndex != -1)
			{
				var partialName = resource.Key.Substring(0, memberIndex);

				if (!partialResources.TryGetValue(partialName, out var partials))
				{
					partialResources[partialName] = partials = new();
				}

				partials.Add(resource);
			}
		}
	}

	/// <summary>
	/// Lookup resources by Uid
	/// </summary>
	/// <param name="uid">A resource UId</param>
	/// <returns>A enumerable of matching resources keys for the provided <paramref name="uid"/></returns>
	internal IEnumerable<ResourceDetails> FindByUId(string uid)
	{
		var (resourceFileName, uidName) = ParseXUid(uid);

		if (_resourcesByFileNameThenUid.TryGetValue(resourceFileName, out var fileResources))
		{
			if (fileResources.TryGetValue(uidName, out var resourceDetail))
			{
				return resourceDetail;
			}
		}

		return Enumerable.Empty<ResourceDetails>();
	}

	/// <summary>
	/// Finds a resource by its key
	/// </summary>
	/// <param name="resourceKey">The resource key</param>
	/// <returns>A <see cref="ResourceDetails"/> for the key, otherwise null.</returns>
	internal ResourceDetails? FindByKey(string resourceKey)
	{
		var (resourceFileName, keyName) = ParseXUid(resourceKey);

		if (_resourcesByFileName.TryGetValue(resourceFileName, out var fileResources))
		{
			if (fileResources.TryGetValue(keyName, out var resourceDetail))
			{
				return resourceDetail;
			}
		}

		return null;
	}

	(string resourceFileName, string uidName) ParseXUid(string uid)
	{
		if (uid.StartsWith("/", StringComparison.Ordinal))
		{
			// Skip the current assembly name for self lookup
			var startIndex = uid.StartsWith("/" + _localAssemblyName, StringComparison.Ordinal)
				? _localAssemblyName.Length + 2
				: 1;

			var separator = uid.IndexOf('/', startIndex);

			return (
				uid.Substring(startIndex, separator - startIndex),
				uid.Substring(separator + 1)
			);
		}
		else
		{
			return ("Resources", uid);
		}
	}
}

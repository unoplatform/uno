using System;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.Phone.PersonalInformation;

namespace Windows.Storage;

public partial class ApplicationDataContainer : IDisposable
{
	private const string ContainerNameSeparator = "__|";

	private Lazy<Dictionary<string, ApplicationDataContainer>> _containers = new(CreateContainersDictionary);
	private ApplicationDataContainer _parent;

	internal ApplicationDataContainer(ApplicationData owner, string name, ApplicationDataLocality locality)
	{
		Locality = locality;
		Name = name;

		InitializePartial(owner);
	}

	internal ApplicationDataContainer(ApplicationDataContainer parent, string name)
	{
		_parent = parent ?? throw new ArgumentNullException(nameof(parent));
		Name = name;
		Locality = parent.Locality;
	}

	internal string ContainerPath => _parent is null ? "" : _parent.ContainerPath + ContainerNameSeparator + Name;

	partial void InitializePartial(ApplicationData owner);

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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Uno.Storage;

internal partial class NativeApplicationSettings
{
	private static readonly ConcurrentDictionary<ApplicationDataLocality, NativeApplicationSettings> _instances = new();

	private readonly ApplicationDataLocality _locality;

	private NativeApplicationSettings(ApplicationDataLocality locality)
	{
		_locality = locality;

		InitializePlatform();
	}

	internal static NativeApplicationSettings GetForLocality(ApplicationDataLocality locality)
	{
		if (!SupportsLocality())
		{
			locality = ApplicationDataLocality.Local;
		}

		return _instances.GetOrAdd(locality, locality => new NativeApplicationSettings(locality));
	}

	private static partial bool SupportsLocality();

	partial void InitializePlatform();
}

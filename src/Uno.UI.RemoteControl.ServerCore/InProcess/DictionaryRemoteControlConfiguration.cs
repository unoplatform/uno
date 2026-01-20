using System;
using System.Collections.Generic;
using Uno.UI.RemoteControl.ServerCore.Configuration;

namespace DevServerCore;

/// <summary>
/// Basic configuration backed by an in-memory dictionary.
/// </summary>
public sealed class DictionaryRemoteControlConfiguration : IRemoteControlConfiguration
{
	private readonly IDictionary<string, string?> _values;

	public DictionaryRemoteControlConfiguration(IDictionary<string, string?>? values = null)
	{
		_values = values ?? new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
	}

	/// <inheritdoc />
	public string? GetValue(string key)
	{
		if (key is null)
		{
			throw new ArgumentNullException(nameof(key));
		}

		return _values.TryGetValue(key, out var value) ? value : null;
	}
}
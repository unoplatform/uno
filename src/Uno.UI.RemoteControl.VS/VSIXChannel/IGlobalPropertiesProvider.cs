using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Uno.IDE;

public interface IGlobalPropertiesProvider
{
	/*
	* WARNING WARNING WARNING WARNING WARNING WARNING WARNING
	*
	* This interface is shared between Uno's VS extension and the Uno.RC package, make sure to keep in sync.
	* In order to avoid versioning issues, avoid modifications and **DO NOT** remove any member from this interface.
	*
	*/

	public event EventHandler OnPropertiesChanged;

	public Task<Dictionary<string, string>> GetPropertiesAsync();
}

internal class GlobalPropertiesProvider(Func<Task<Dictionary<string, string>>> provider) : IGlobalPropertiesProvider
{
#pragma warning disable CS0067 // Event not used ===> Legacy code
	/// <inheritdoc />
	public event EventHandler? OnPropertiesChanged;
#pragma warning restore CS0067

	/// <inheritdoc />
	public Task<Dictionary<string, string>> GetPropertiesAsync()
		=> provider();
}

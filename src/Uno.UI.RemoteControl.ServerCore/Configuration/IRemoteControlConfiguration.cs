namespace Uno.UI.RemoteControl.ServerCore.Configuration;

/// <summary>
/// Provides access to configuration values required by the devserver core without binding to ASP.NET abstractions.
/// </summary>
public interface IRemoteControlConfiguration
{
	/// <summary>
	/// Retrieves a configuration value for the given key.
	/// </summary>
	string? GetValue(string key);
}

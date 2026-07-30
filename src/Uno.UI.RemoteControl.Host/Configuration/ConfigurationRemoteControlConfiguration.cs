using Microsoft.Extensions.Configuration;
using Uno.UI.RemoteControl.ServerCore.Configuration;

namespace Uno.UI.RemoteControl.Host.Configuration;

/// <summary>
/// Wraps <see cref="IConfiguration"/> to expose an <see cref="IRemoteControlConfiguration"/> for the devserver core.
/// </summary>
internal sealed class ConfigurationRemoteControlConfiguration(IConfiguration configuration) : IRemoteControlConfiguration
{
	private readonly IConfiguration _configuration = configuration;

	/// <inheritdoc />
	public string? GetValue(string key) => _configuration[key];
}
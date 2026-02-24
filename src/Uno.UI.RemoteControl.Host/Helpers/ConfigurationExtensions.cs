using System;
using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Uno.UI.RemoteControl.Host.Helpers;

internal static class ConfigurationExtensions
{
	public static int ParseOptionalInt(this IConfiguration configuration, string key)
	{
		var value = configuration[key];

		if (string.IsNullOrWhiteSpace(value))
		{
			return 0;
		}

		if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			throw new ArgumentException($"The {key} parameter is invalid {value}");
		}

		return result;
	}

	public static int ParseIntOrDefault(this IConfiguration configuration, string key, int defaultValue)
	{
		var value = configuration[key];

		if (string.IsNullOrWhiteSpace(value))
		{
			return defaultValue;
		}

		return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
			? result
			: defaultValue;
	}

	public static string? GetOptionalString(this IConfiguration configuration, string key)
	{
		var value = configuration[key];
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		var trimmed = value.Trim();
		return trimmed.Length == 0 ? null : trimmed;
	}

	/// <summary>
	/// Gets the value for the --addins key, preserving the distinction between
	/// "not present" (null → MSBuild fallback) and "empty string" (no add-ins).
	/// </summary>
	/// <seealso href="../../Uno.UI.DevServer.Cli/addin-discovery.md"/>
	public static string? GetAddinsValue(this IConfiguration configuration, string key)
	{
		var value = configuration[key];
		if (value is null)
		{
			return null;
		}

		return value.Trim();
	}
}

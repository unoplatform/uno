using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Helpers;

internal static class GlobalJsonLocator
{
	public static string? FindGlobalJson(string startPath)
	{
		string? currentPath = Path.GetFullPath(startPath);
		while (currentPath is not null)
		{
			var globalJsonPath = Path.Combine(currentPath, "global.json");
			if (File.Exists(globalJsonPath))
			{
				return globalJsonPath;
			}

			var parent = Directory.GetParent(currentPath);
			currentPath = parent?.FullName;
		}

		return null;
	}

	public static async Task<(string? globalJsonPath, string? sdkPackage, string? sdkVersion)> ParseGlobalJsonForUnoSdkAsync(string searchDirectory, ILogger? logger = null)
	{
		try
		{
			var globalJsonPath = FindGlobalJson(searchDirectory);
			if (globalJsonPath is null)
			{
				return (null, null, null);
			}

			return await ParseGlobalJsonFileForUnoSdkAsync(globalJsonPath, logger);
		}
		catch (Exception ex)
		{
			logger?.LogWarning(ex, "Failed to parse global.json from {Directory}", searchDirectory);
			return (null, null, null);
		}
	}

	public static async Task<(string? globalJsonPath, string? sdkPackage, string? sdkVersion)> ParseGlobalJsonFileForUnoSdkAsync(string globalJsonPath, ILogger? logger = null)
	{
		try
		{
			var content = await File.ReadAllTextAsync(globalJsonPath);
			using var document = JsonDocument.Parse(
				content,
				new JsonDocumentOptions
				{
					CommentHandling = JsonCommentHandling.Skip,
					AllowTrailingCommas = true,
				});

			if (document.RootElement.TryGetProperty("msbuild-sdks", out var sdksElement))
			{
				if (sdksElement.TryGetProperty("Uno.Sdk", out var unoSdkElement))
				{
					return (globalJsonPath, "Uno.Sdk", unoSdkElement.GetString());
				}

				if (sdksElement.TryGetProperty("Uno.Sdk.Private", out var unoSdkPrivateElement))
				{
					return (globalJsonPath, "Uno.Sdk.Private", unoSdkPrivateElement.GetString());
				}
			}

			return (globalJsonPath, null, null);
		}
		catch (Exception ex)
		{
			logger?.LogWarning(ex, "Failed to parse global.json file {Path}", globalJsonPath);
			return (globalJsonPath, null, null);
		}
	}
}

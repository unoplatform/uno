using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Helpers;

internal class DotNetVersionCache
{
	private static readonly TimeSpan CacheMaxAge = TimeSpan.FromHours(24);
	private static readonly JsonSerializerOptions s_jsonOptions = new() { WriteIndented = true };

	private readonly ILogger<DotNetVersionCache> _logger;
	private readonly Func<Task<(string? rawVersion, string? tfm)>> _dotNetVersionProvider;
	private readonly string? _cachePathOverride;

	public DotNetVersionCache(ILogger<DotNetVersionCache> logger)
		: this(logger, dotNetVersionProvider: null, cachePathOverride: null)
	{
	}

	internal DotNetVersionCache(
		ILogger<DotNetVersionCache> logger,
		Func<Task<(string? rawVersion, string? tfm)>>? dotNetVersionProvider,
		string? cachePathOverride = null)
	{
		_logger = logger;
		_dotNetVersionProvider = dotNetVersionProvider ?? RunDotNetVersionAsync;
		_cachePathOverride = cachePathOverride;
	}

	/// <summary>
	/// Returns the cached dotnet version info, refreshing if stale or forced.
	/// </summary>
	/// <param name="globalJsonPath">Path to global.json (used to extract sdk.version as cache key).</param>
	/// <param name="force">When true, always refresh regardless of cache validity.</param>
	public async Task<(string? rawVersion, string? tfm)> GetOrRefreshAsync(
		string? globalJsonPath,
		bool force = false)
	{
		var cachePath = _cachePathOverride ?? GetDefaultCachePath();
		var sdkVersionKey = TryGetSdkVersionFromGlobalJson(globalJsonPath) ?? "";

		if (!force)
		{
			var cached = TryReadCache(cachePath, sdkVersionKey);
			if (cached is not null)
			{
				_logger.LogDebug("DotNetVersionCache hit: rawVersion={RawVersion}, tfm={Tfm}", cached.Value.rawVersion, cached.Value.tfm);
				return cached.Value;
			}
		}

		_logger.LogDebug("DotNetVersionCache miss (force={Force}), running dotnet --version", force);
		var result = await _dotNetVersionProvider();

		if (result.rawVersion is not null && result.tfm is not null)
		{
			TryWriteCache(cachePath, result.rawVersion, result.tfm, sdkVersionKey);
		}

		return result;
	}

	private (string? rawVersion, string? tfm)? TryReadCache(string cachePath, string sdkVersionKey)
	{
		try
		{
			if (!File.Exists(cachePath))
			{
				return null;
			}

			var json = File.ReadAllText(cachePath);
			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;

			if (!root.TryGetProperty("version", out var versionProp) || versionProp.GetInt32() != 1)
			{
				_logger.LogDebug("Cache version mismatch, will refresh");
				return null;
			}

			var cachedSdkKey = root.TryGetProperty("sdkVersionKey", out var sdkKeyProp)
				? sdkKeyProp.GetString() ?? ""
				: "";

			if (cachedSdkKey != sdkVersionKey)
			{
				_logger.LogDebug("Cache sdkVersionKey mismatch (cached={CachedKey}, current={CurrentKey}), will refresh", cachedSdkKey, sdkVersionKey);
				return null;
			}

			if (root.TryGetProperty("timestamp", out var tsProp)
				&& DateTime.TryParse(tsProp.GetString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out var timestamp))
			{
				if (DateTime.UtcNow - timestamp > CacheMaxAge)
				{
					_logger.LogDebug("Cache expired (age > 24h), will refresh");
					return null;
				}
			}
			else
			{
				return null;
			}

			var rawVersion = root.TryGetProperty("rawVersion", out var rvProp) ? rvProp.GetString() : null;
			var tfm = root.TryGetProperty("tfm", out var tfmProp) ? tfmProp.GetString() : null;

			if (rawVersion is null || tfm is null)
			{
				return null;
			}

			return (rawVersion, tfm);
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, "Failed to read DotNetVersionCache, will refresh");
			return null;
		}
	}

	private void TryWriteCache(string cachePath, string rawVersion, string tfm, string sdkVersionKey)
	{
		try
		{
			var dir = Path.GetDirectoryName(cachePath);
			if (!string.IsNullOrEmpty(dir))
			{
				Directory.CreateDirectory(dir);
			}

			var cacheObj = new
			{
				version = 1,
				rawVersion,
				tfm,
				sdkVersionKey,
				timestamp = DateTime.UtcNow.ToString("O")
			};

			var json = JsonSerializer.Serialize(cacheObj, s_jsonOptions);
			File.WriteAllText(cachePath, json);
			_logger.LogDebug("DotNetVersionCache written to {CachePath}", cachePath);
		}
		catch (Exception ex)
		{
			_logger.LogDebug(ex, "Failed to write DotNetVersionCache");
		}
	}

	internal static string GetDefaultCachePath()
	{
		var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		if (string.IsNullOrWhiteSpace(basePath))
		{
			basePath = Path.GetTempPath();
		}

		return Path.Combine(basePath, "Uno Platform", "uno.devserver", "dotnet-version-cache.json");
	}

	internal static string? TryGetSdkVersionFromGlobalJson(string? globalJsonPath)
	{
		if (string.IsNullOrWhiteSpace(globalJsonPath) || !File.Exists(globalJsonPath))
		{
			return null;
		}

		try
		{
			var content = File.ReadAllText(globalJsonPath);
			using var doc = JsonDocument.Parse(
				content,
				new()
				{
					CommentHandling = JsonCommentHandling.Skip,
					AllowTrailingCommas = true
				});

			if (doc.RootElement.TryGetProperty("sdk", out var sdkElement)
				&& sdkElement.TryGetProperty("version", out var versionElement))
			{
				return versionElement.GetString();
			}
		}
		catch
		{
			// Ignore parse errors
		}

		return null;
	}

	private static async Task<(string? rawVersion, string? tfm)> RunDotNetVersionAsync()
	{
		try
		{
			var processInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = "--version",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			using var process = Process.Start(processInfo);
			if (process is null)
			{
				return (null, null);
			}

			var output = await process.StandardOutput.ReadToEndAsync();
			await process.WaitForExitAsync();

			if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
			{
				var version = output.Trim();
				var sanitizedVersion = version.Split('-')[0];

				if (Version.TryParse(sanitizedVersion, out var parsedVersion))
				{
					var tfm = $"net{parsedVersion.Major}.{parsedVersion.Minor}";
					return (version, tfm);
				}
			}
		}
		catch
		{
			// Ignore errors - caller handles null
		}

		return (null, null);
	}
}

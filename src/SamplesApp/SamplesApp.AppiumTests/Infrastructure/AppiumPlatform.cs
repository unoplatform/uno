#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

namespace SamplesApp.AppiumTests.Infrastructure;

public enum AppiumPlatform
{
	Windows,
	Mac,
	Wasm,
}

public sealed class AppiumTestOptions
{
	public const string EnvVarPlatform = "UNO_APPIUM_PLATFORM";
	public const string EnvVarAppPath = "UNO_APPIUM_SAMPLESAPP";
	public const string EnvVarAppiumServer = "UNO_APPIUM_SERVER";
	public const string EnvVarRecordSnapshots = "UNO_APPIUM_RECORD_SNAPSHOTS";
	public const string EnvVarSnapshotsDir = "UNO_APPIUM_SNAPSHOTS_DIR";
	public const string EnvVarArtifactsDir = "UNO_APPIUM_ARTIFACTS_DIR";
	public const string EnvVarTimeoutSeconds = "UNO_APPIUM_TIMEOUT_SECONDS";
	public const string EnvVarPollIntervalMilliseconds = "UNO_APPIUM_POLL_INTERVAL_MS";
	public const string EnvVarKeepBundle = "UNO_APPIUM_KEEP_BUNDLE";
	public const string EnvVarChromeBinary = "UNO_APPIUM_CHROME_BINARY";
	public const string EnvVarChromeArguments = "UNO_APPIUM_CHROME_ARGUMENTS";

	private const int DefaultTimeoutSeconds = 20;
	private const int DefaultPollIntervalMilliseconds = 200;

	private AppiumTestOptions(
		AppiumPlatform platform,
		string appPath,
		Uri serverUri,
		bool recordSnapshots,
		string? snapshotsDirectoryOverride,
		string artifactsDirectory,
		TimeSpan timeout,
		TimeSpan pollInterval,
		bool keepMacBundle,
		string? chromeBinaryPath,
		IReadOnlyList<string> chromeArguments)
	{
		Platform = platform;
		AppPath = appPath;
		ServerUri = serverUri;
		RecordSnapshots = recordSnapshots;
		SnapshotsDirectoryOverride = snapshotsDirectoryOverride;
		ArtifactsDirectory = artifactsDirectory;
		Timeout = timeout;
		PollInterval = pollInterval;
		KeepMacBundle = keepMacBundle;
		ChromeBinaryPath = chromeBinaryPath;
		ChromeArguments = chromeArguments;
	}

	public AppiumPlatform Platform { get; }

	public string AppPath { get; }

	public Uri ServerUri { get; }

	public bool RecordSnapshots { get; }

	public string? SnapshotsDirectoryOverride { get; }

	public string ArtifactsDirectory { get; }

	public TimeSpan Timeout { get; }

	public TimeSpan PollInterval { get; }

	public bool KeepMacBundle { get; }

	public string? ChromeBinaryPath { get; }

	public IReadOnlyList<string> ChromeArguments { get; }

	public string Flavor
		=> Platform switch
		{
			AppiumPlatform.Windows => "win32",
			AppiumPlatform.Mac => "macos",
			AppiumPlatform.Wasm => "wasm",
			_ => throw new NotSupportedException(),
		};

	public string DiagnosticContext(string sampleQuery)
		=> $"platform={Platform}, sample={sampleQuery}, server={ServerUri}, app={AppPath}";

	public static AppiumTestOptions LoadRequired(string defaultArtifactsDirectory)
	{
		var platformValue = RequireEnvironmentVariable(
			EnvVarPlatform,
			$"Host-required Appium tests need {EnvVarPlatform}=windows|mac|wasm.");
		var appPath = RequireEnvironmentVariable(
			EnvVarAppPath,
			$"Host-required Appium tests need {EnvVarAppPath} to point at the built SamplesApp output.");

		var platform = ParsePlatform(platformValue);
		var timeout = TimeSpan.FromSeconds(ParsePositiveInt(
			EnvVarTimeoutSeconds,
			DefaultTimeoutSeconds,
			minimumValue: 1));
		var pollInterval = TimeSpan.FromMilliseconds(ParsePositiveInt(
			EnvVarPollIntervalMilliseconds,
			DefaultPollIntervalMilliseconds,
			minimumValue: 50));

		var serverUri = ParseServerUri(Environment.GetEnvironmentVariable(EnvVarAppiumServer));
		var artifactsDirectory = Environment.GetEnvironmentVariable(EnvVarArtifactsDir);
		artifactsDirectory = string.IsNullOrWhiteSpace(artifactsDirectory)
			? defaultArtifactsDirectory
			: Path.GetFullPath(artifactsDirectory);
		var chromeBinaryPath = ParseChromeBinaryPath(
			platform,
			Environment.GetEnvironmentVariable(EnvVarChromeBinary));

		ValidateAppPath(platform, appPath);

		return new AppiumTestOptions(
			platform,
			appPath,
			serverUri,
			ParseBooleanEnvironmentVariable(EnvVarRecordSnapshots, defaultValue: false),
			Environment.GetEnvironmentVariable(EnvVarSnapshotsDir),
			Path.GetFullPath(artifactsDirectory),
			timeout,
			pollInterval,
			ParseBooleanEnvironmentVariable(EnvVarKeepBundle, defaultValue: false),
			chromeBinaryPath,
			ParseChromeArguments(Environment.GetEnvironmentVariable(EnvVarChromeArguments)));
	}

	public static AppiumPlatform ParsePlatform(string value)
		=> value.Trim().ToLowerInvariant() switch
		{
			"windows" or "win" or "win32" => AppiumPlatform.Windows,
			"mac" or "macos" or "osx" => AppiumPlatform.Mac,
			"wasm" or "web" or "browser" => AppiumPlatform.Wasm,
			_ => throw new ArgumentException(
				$"Unknown {EnvVarPlatform} value '{value}'. Expected windows|mac|wasm."),
		};

	private static string RequireEnvironmentVariable(string name, string message)
	{
		var value = Environment.GetEnvironmentVariable(name);
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}

		throw new InvalidOperationException(message);
	}

	private static Uri ParseServerUri(string? value)
	{
		var server = string.IsNullOrWhiteSpace(value)
			? "http://127.0.0.1:4723/"
			: value.Trim();

		if (Uri.TryCreate(server, UriKind.Absolute, out var uri)
			&& (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
		{
			return uri;
		}

		throw new InvalidOperationException(
			$"{EnvVarAppiumServer} must be an absolute http(s) URL. Actual: '{server}'.");
	}

	private static int ParsePositiveInt(string variableName, int defaultValue, int minimumValue)
	{
		var value = Environment.GetEnvironmentVariable(variableName);
		if (string.IsNullOrWhiteSpace(value))
		{
			return defaultValue;
		}

		if (!int.TryParse(value, out var parsed) || parsed < minimumValue)
		{
			throw new InvalidOperationException(
				$"{variableName} must be an integer greater than or equal to {minimumValue}. Actual: '{value}'.");
		}

		return parsed;
	}

	private static bool ParseBooleanEnvironmentVariable(string variableName, bool defaultValue)
	{
		var value = Environment.GetEnvironmentVariable(variableName);
		if (string.IsNullOrWhiteSpace(value))
		{
			return defaultValue;
		}

		return value.Trim().ToLowerInvariant() switch
		{
			"1" or "true" or "yes" => true,
			"0" or "false" or "no" => false,
			_ => throw new InvalidOperationException(
				$"{variableName} must be one of 1|0|true|false|yes|no. Actual: '{value}'."),
		};
	}

	private static string? ParseChromeBinaryPath(AppiumPlatform platform, string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		if (platform != AppiumPlatform.Wasm)
		{
			throw new InvalidOperationException(
				$"{EnvVarChromeBinary} is only valid when {EnvVarPlatform}=wasm.");
		}

		ValidateAbsoluteFilePath(
			value,
			EnvVarChromeBinary,
			"WASM ChromeDriver tests require a valid Chrome/Chromium executable.");
		return Path.GetFullPath(value);
	}

	private static IReadOnlyList<string> ParseChromeArguments(string? value)
		=> string.IsNullOrWhiteSpace(value)
			? Array.Empty<string>()
			: value.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

	private static void ValidateAppPath(AppiumPlatform platform, string appPath)
	{
		switch (platform)
		{
			case AppiumPlatform.Windows:
				ValidateAbsoluteFilePath(
					appPath,
					EnvVarAppPath,
					"Windows Appium tests require the built SamplesApp.Skia.Generic executable path.");
				if (!appPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
				{
					throw new InvalidOperationException(
						$"{EnvVarAppPath} must point to the Windows SamplesApp .exe. Actual: '{appPath}'.");
				}
				break;

			case AppiumPlatform.Mac:
				if (!Path.IsPathRooted(appPath))
				{
					throw new InvalidOperationException(
						$"{EnvVarAppPath} must be an absolute path to a .app bundle or .dll. Actual: '{appPath}'.");
				}

				if (appPath.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
				{
					if (!Directory.Exists(appPath))
					{
						throw new InvalidOperationException(
							$"{EnvVarAppPath} points to a missing .app bundle: '{appPath}'.");
					}
				}
				else if (!File.Exists(appPath))
				{
					throw new InvalidOperationException(
						$"{EnvVarAppPath} points to a missing macOS SamplesApp assembly: '{appPath}'.");
				}
				break;

			case AppiumPlatform.Wasm:
				if (!Uri.TryCreate(appPath, UriKind.Absolute, out var uri)
					|| (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
				{
					throw new InvalidOperationException(
						$"{EnvVarAppPath} must be an absolute http(s) URL for the hosted Skia WASM app. Actual: '{appPath}'.");
				}
				break;

			default:
				throw new NotSupportedException();
		}
	}

	private static void ValidateAbsoluteFilePath(string path, string variableName, string message)
	{
		if (!Path.IsPathRooted(path))
		{
			throw new InvalidOperationException(
				$"{message} {variableName} must be absolute. Actual: '{path}'.");
		}

		if (!File.Exists(path))
		{
			throw new InvalidOperationException(
				$"{message} File does not exist: '{path}'.");
		}
	}
}

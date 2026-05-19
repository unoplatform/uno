#nullable enable

using System;

namespace SamplesApp.AppiumTests.Infrastructure;

public enum AppiumPlatform
{
	Windows,
	Mac,
	Wasm,
}

public static class AppiumPlatformResolver
{
	public const string EnvVarPlatform = "UNO_APPIUM_PLATFORM";
	public const string EnvVarAppPath = "UNO_APPIUM_SAMPLESAPP";
	public const string EnvVarAppiumServer = "UNO_APPIUM_SERVER";

	public static AppiumPlatform? TryResolve()
	{
		var value = Environment.GetEnvironmentVariable(EnvVarPlatform);
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}

		return value.ToLowerInvariant() switch
		{
			"windows" or "win" or "win32" => AppiumPlatform.Windows,
			"mac" or "macos" or "osx" => AppiumPlatform.Mac,
			"wasm" or "web" or "browser" => AppiumPlatform.Wasm,
			_ => throw new ArgumentException(
				$"Unknown {EnvVarPlatform} value '{value}'. Expected windows|mac|wasm."),
		};
	}

	public static string ServerUrl()
		=> Environment.GetEnvironmentVariable(EnvVarAppiumServer)
			?? "http://127.0.0.1:4723/";

	public static string RequireAppPath()
	{
		var value = Environment.GetEnvironmentVariable(EnvVarAppPath);
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new InvalidOperationException(
				$"Environment variable {EnvVarAppPath} is not set. " +
				"Point it at the built SamplesApp output (e.g. " +
				"the .exe path for Windows, the .dll path for macOS, " +
				"or the http base URL for WASM).");
		}

		return value;
	}
}

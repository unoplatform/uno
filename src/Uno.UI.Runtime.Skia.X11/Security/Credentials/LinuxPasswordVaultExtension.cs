#nullable enable

using System;
using System.Diagnostics;
using Uno.Extensions.Security.Credentials;

namespace Uno.UI.Runtime.Skia.X11.Security.Credentials;

/// <summary>
/// Linux backend for <see cref="Windows.Security.Credentials.PasswordVault"/> that stores
/// the serialized vault blob in the system Secret Service (GNOME Keyring / KWallet) via
/// the <c>secret-tool</c> CLI from the <c>libsecret-tools</c> package.
/// </summary>
/// <remarks>
/// The blob is base64-encoded before being handed to <c>secret-tool</c> because the
/// tool operates on text strings.
/// </remarks>
internal sealed class LinuxPasswordVaultExtension : IPasswordVaultExtension
{
	private const string ServiceAttribute = "uno_passwordvault";
	private const string Label = "Uno PasswordVault";

	private static string AppAttribute => PasswordVaultAppIdentifier.AppId;

	private static string LookupArgs => $"lookup service {ServiceAttribute} app {Quote(AppAttribute)}";

	private static string StoreArgs => $"store --label={Quote(Label)} service {ServiceAttribute} app {Quote(AppAttribute)}";

	private static string Quote(string value) => "\"" + value.Replace("\"", "\\\"") + "\"";

	public static bool IsAvailable()
	{
		try
		{
			var psi = new ProcessStartInfo("secret-tool", "--version")
			{
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true,
			};

			using var process = Process.Start(psi);
			if (process is null)
			{
				return false;
			}

			if (!process.WaitForExit(2000))
			{
				try { process.Kill(); } catch { /* best effort */ }
				return false;
			}

			return process.ExitCode == 0;
		}
		catch
		{
			return false;
		}
	}

	public byte[]? TryRead()
	{
		var psi = new ProcessStartInfo("secret-tool", LookupArgs)
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start secret-tool.");

		var stdout = process.StandardOutput.ReadToEnd();
		process.WaitForExit();

		// Exit code 1 with empty output means "not found".
		if (process.ExitCode != 0 || string.IsNullOrWhiteSpace(stdout))
		{
			return null;
		}

		try
		{
			return Convert.FromBase64String(stdout.Trim());
		}
		catch (FormatException)
		{
			return null;
		}
	}

	public void Write(byte[] data)
	{
		var psi = new ProcessStartInfo("secret-tool", StoreArgs)
		{
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		using var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start secret-tool.");

		process.StandardInput.WriteLine(Convert.ToBase64String(data));
		process.StandardInput.Close();

		process.WaitForExit();
		if (process.ExitCode != 0)
		{
			var err = process.StandardError.ReadToEnd();
			throw new InvalidOperationException($"secret-tool store failed (exit code {process.ExitCode}): {err}");
		}
	}
}

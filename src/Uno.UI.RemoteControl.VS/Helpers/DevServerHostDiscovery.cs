using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.VS.Helpers;

/// <summary>
/// Probes the Uno DevServer CLI's <c>disco</c> command to find an already-running host
/// for the current solution. <see cref="EntryPoint.EnsureServerAsync"/> consults this
/// before spawning a host of its own, so the CLI-driven launch flow (introduced in the
/// uno.studio VS extension and the JetBrains/VS Code extensions) and the legacy
/// in-process spawn don't both end up running for the same solution.
///
/// <para>
/// This is "Phase 1": detect-and-skip. The disco payload doesn't yet expose an
/// <c>ideChannelId</c> reliably, so EntryPoint can't fully adopt the existing host's
/// IDE channel pipe — it just declines to spawn a duplicate. Phase 2 (connecting our
/// <c>IdeChannelClient</c> to the existing host's pipe) is gated on the CLI surfacing
/// <c>ideChannelId</c> in active-server entries.
/// </para>
///
/// <para>
/// The CLI is invoked through <c>dotnet dnx -y uno.devserver disco</c>. On hosts where
/// <c>dnx</c> is unavailable (.NET 9-only machines, solutions with a
/// 9.x-pinned <c>global.json</c>) the bare <c>uno-devserver</c> shim — installed by
/// the uno.studio extension's tool-install fallback — is tried as a backup. Any
/// other failure is non-fatal: callers fall through to the existing spawn path.
/// </para>
/// </summary>
internal sealed class DevServerHostDiscovery
{
	private static readonly JsonSerializerOptions _options = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true,
	};

	private static readonly TimeSpan _defaultTimeout = TimeSpan.FromSeconds(8);

	/// <summary>
	/// Returns an active server matching <paramref name="solutionPath"/> and
	/// <paramref name="devenvProcessId"/>, or <see langword="null"/> if discovery fails
	/// or no match is found. Failures are swallowed — the caller spawns when this
	/// returns null.
	/// </summary>
	public async Task<DiscoveredDevServer?> TryFindHostAsync(
		string solutionPath,
		int devenvProcessId,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(solutionPath))
		{
			return null;
		}

		try
		{
			var json = await RunDiscoAsync(solutionPath, cancellationToken).ConfigureAwait(false);
			if (string.IsNullOrEmpty(json))
			{
				return null;
			}

			var servers = ParseActiveServers(json);
			return Match(servers, solutionPath, devenvProcessId);
		}
		catch
		{
			// Discovery is best-effort. Any failure here means the caller spawns as before;
			// the worst case is a duplicate host (which is already the existing behavior).
			return null;
		}
	}

	internal static IReadOnlyList<DiscoveredDevServer> ParseActiveServers(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return Array.Empty<DiscoveredDevServer>();
		}

		try
		{
			using var document = JsonDocument.Parse(json!);
			if (!document.RootElement.TryGetProperty("activeServers", out var servers)
				|| servers.ValueKind != JsonValueKind.Array)
			{
				return Array.Empty<DiscoveredDevServer>();
			}

			var result = new List<DiscoveredDevServer>(servers.GetArrayLength());
			foreach (var item in servers.EnumerateArray())
			{
				var deserialized = item.Deserialize<DiscoveredDevServer>(_options);
				if (deserialized is not null)
				{
					result.Add(deserialized);
				}
			}
			return result;
		}
		catch (JsonException)
		{
			// Malformed payload — degrade rather than throw. Caller spawns as before.
			return Array.Empty<DiscoveredDevServer>();
		}
	}

	internal static DiscoveredDevServer? Match(
		IReadOnlyList<DiscoveredDevServer> servers,
		string solutionPath,
		int devenvProcessId)
	{
		foreach (var server in servers)
		{
			if (server.ParentProcessId != devenvProcessId)
			{
				continue;
			}

			if (server.SolutionPath is null)
			{
				continue;
			}

			// Solution paths are case-insensitive on Windows, where the VS extension lives.
			if (string.Equals(server.SolutionPath, solutionPath, StringComparison.OrdinalIgnoreCase))
			{
				return server;
			}
		}

		return null;
	}

	private async Task<string?> RunDiscoAsync(string solutionPath, CancellationToken cancellationToken)
	{
		// First try the SDK 10+ path: `dotnet dnx -y uno.devserver disco --json`.
		// On SDK 9-only / global.json-pinned hosts that fails; the uno.studio extension's
		// tool-install fallback puts a `uno-devserver` shim on PATH, which we try second.
		var json = await TryRunAsync(
				fileName: "dotnet",
				arguments: $"dnx -y uno.devserver disco --solution \"{solutionPath}\" --json",
				solutionPath,
				cancellationToken)
			.ConfigureAwait(false);

		if (!string.IsNullOrEmpty(json))
		{
			return json;
		}

		return await TryRunAsync(
				fileName: "uno-devserver",
				arguments: $"disco --solution \"{solutionPath}\" --json",
				solutionPath,
				cancellationToken)
			.ConfigureAwait(false);
	}

	private static Task<string?> TryRunAsync(string fileName, string arguments, string solutionPath, CancellationToken cancellationToken)
	{
		return Task.Run<string?>(() =>
		{
			try
			{
				var workingDirectory = Path.GetDirectoryName(solutionPath);
				using var process = new Process
				{
					StartInfo = new ProcessStartInfo
					{
						FileName = fileName,
						Arguments = arguments,
						WorkingDirectory = string.IsNullOrEmpty(workingDirectory) ? Environment.CurrentDirectory : workingDirectory,
						UseShellExecute = false,
						RedirectStandardOutput = true,
						RedirectStandardError = true,
						CreateNoWindow = true,
					},
				};

				if (!process.Start())
				{
					return null;
				}

				// Bound the wait so a hung dotnet/dnx doesn't block solution open.
				if (!process.WaitForExit((int)_defaultTimeout.TotalMilliseconds))
				{
					try { process.Kill(); } catch { /* swallow */ }
					return null;
				}

				if (process.ExitCode != 0)
				{
					return null;
				}

				return process.StandardOutput.ReadToEnd();
			}
			catch
			{
				return null;
			}
		}, cancellationToken);
	}
}

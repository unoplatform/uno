using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Uno.UI.DevServer.Cli.Helpers;

/// <summary>
/// Reads the curated "recommended Uno.Sdk version" manifest — the same source the IDE
/// extensions use (<c>https://aka.platform.uno/uno-sdk-manifest</c>, served from the
/// <c>unoplatform/uno-assets</c> repo). Used to tell whether a project's pinned Uno.Sdk
/// is behind the recommended version, so the DevServer/MCP can surface an update the same
/// way the extensions do.
/// </summary>
internal static class SdkManifest
{
	private const string ManifestUrl = "https://aka.platform.uno/uno-sdk-manifest";
	private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

	// No global Timeout on the shared client: each request bounds itself with a linked
	// CancellationTokenSource so one caller's timeout can never affect another's request.
	private static readonly HttpClient _httpClient = new();

	/// <summary>
	/// Fetches the recommended Uno.Sdk version from the manifest. Best-effort: returns
	/// <c>null</c> (never throws) when the manifest is unreachable, times out, or is
	/// malformed, so an offline environment simply reports "no update info". The request is
	/// bounded by <paramref name="timeout"/> (default 5s) via a token linked to
	/// <paramref name="ct"/>, so callers/tests can shorten the wait against a slow endpoint.
	/// </summary>
	public static async Task<string?> GetLatestUnoSdkVersionAsync(
		ILogger? logger = null,
		CancellationToken ct = default,
		TimeSpan? timeout = null)
	{
		try
		{
			using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
			cts.CancelAfter(timeout ?? DefaultTimeout);

			using var response = await _httpClient.GetAsync(ManifestUrl, cts.Token);
			if (!response.IsSuccessStatusCode)
			{
				logger?.LogDebug("Uno.Sdk manifest fetch returned {StatusCode}", (int)response.StatusCode);
				return null;
			}

			await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
			var payload = await JsonSerializer.DeserializeAsync(stream, SdkManifestJsonContext.Default.SdkManifestPayload, cts.Token);
			return payload?.Version;
		}
		catch (OperationCanceledException)
		{
			// Expected when the request times out (CancelAfter) or the caller cancels — log a
			// plain message without the exception/stack trace to avoid noisy Debug output.
			logger?.LogDebug("Uno.Sdk version manifest fetch timed out or was cancelled.");
			return null;
		}
		catch (Exception ex)
		{
			logger?.LogDebug(ex, "Failed to fetch the Uno.Sdk version manifest.");
			return null;
		}
	}

	/// <summary>
	/// True when <paramref name="candidate"/> is strictly higher than <paramref name="current"/>,
	/// compared numerically component-by-component (mirrors the IDE extension's comparison so
	/// recommendations stay consistent). Any pre-release suffix is ignored.
	/// </summary>
	public static bool IsNewer(string? candidate, string? current)
	{
		if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(current))
		{
			return false;
		}

		var c = ParseParts(candidate);
		var i = ParseParts(current);
		var length = Math.Max(c.Length, i.Length);
		for (var n = 0; n < length; n++)
		{
			var cp = n < c.Length ? c[n] : 0;
			var ip = n < i.Length ? i[n] : 0;
			if (cp > ip)
			{
				return true;
			}
			if (cp < ip)
			{
				return false;
			}
		}

		return false;
	}

	private static int[] ParseParts(string version)
	{
		// Compare only the numeric dotted core (drop any "-prerelease" suffix).
		var core = version.Split('-', 2)[0];
		var segments = core.Split('.');
		var parts = new int[segments.Length];
		for (var i = 0; i < segments.Length; i++)
		{
			parts[i] = int.TryParse(segments[i], out var value) ? value : 0;
		}

		return parts;
	}
}

/// <summary>Shape of the recommended-version manifest: <c>{ "version": "&lt;semver&gt;" }</c>.</summary>
internal sealed record SdkManifestPayload(
	[property: JsonPropertyName("version")] string? Version);

[JsonSerializable(typeof(SdkManifestPayload))]
internal partial class SdkManifestJsonContext : JsonSerializerContext;

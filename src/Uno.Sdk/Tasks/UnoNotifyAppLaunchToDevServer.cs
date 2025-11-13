using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Uno.Sdk.Tasks;

public sealed class UnoNotifyAppLaunchToDevServer_v0 : Task
{
	[Required] public string Port { get; set; } = string.Empty;
	[Required] public string TargetPath { get; set; } = string.Empty;
	public string IsDebug { get; set; } = string.Empty;
	public string Ide { get; set; } = string.Empty;
	public string Plugin { get; set; } = string.Empty;

	[Output] public bool Success { get; set; }
	[Output] public string ResponseContent { get; set; } = string.Empty;

	public override bool Execute()
	{
		// Validate
		if (string.IsNullOrWhiteSpace(Port) || !ushort.TryParse(Port, out var portNum) || portNum == 0)
		{
			Log.LogError("UnoRemoteControlPort must be a valid port number between 1 and 65535.");
			return Success = false;
		}
		if (string.IsNullOrWhiteSpace(TargetPath))
		{
			Log.LogError("TargetPath is required.");
			return Success = false;
		}

		var encodedPath = WebUtility.UrlEncode(TargetPath);

		var parts = new List<string>();
		void Add(string name, string value)
		{
			if (!string.IsNullOrEmpty(value))
				parts.Add($"{name}={Uri.EscapeDataString(value)}");
		}

		Add("ide", Ide);
		Add("plugin", Plugin);

		// IsDebug is optional: treat empty/null as "false"
		var isDebugValue = false;
		if (!string.IsNullOrEmpty(IsDebug) && bool.TryParse(IsDebug, out var parsed))
		{
			isDebugValue = parsed;
		}
		Add("isDebug", isDebugValue ? "true" : "false");

		var qs = parts.Count > 0 ? "?" + string.Join("&", parts) : string.Empty;
		var url = $"http://localhost:{portNum}/applaunch/asm/{encodedPath}{qs}";

		try
		{
			using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
			var response = client.GetAsync(url).GetAwaiter().GetResult();

			Log.LogMessage(MessageImportance.High,
				$"[NotifyDevServer] GET {url} -> {(int)response.StatusCode} {response.ReasonPhrase}");

			if (response.IsSuccessStatusCode)
			{
				ResponseContent = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
				Success = true;
				return true;
			}
			else
			{
				Success = false;
				ResponseContent = string.Empty;
				return false;
			}
		}
		catch (Exception ex)
		{
			Log.LogWarning($"[NotifyDevServer] GET {url} failed: {ex.GetType().Name}: {ex.Message}");
			Success = false;
			ResponseContent = string.Empty;
			return false;
		}
	}
}

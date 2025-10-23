using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable VSTHRD010
#pragma warning disable VSTHRD109

namespace Uno.UI.RemoteControl.VS;

partial class EntryPoint
{
	private async Task SetupMcpAsync(CancellationToken ct)
	{
		if (_devServer is null)
		{
			return;
		}

		var solutionDotVsDir = Path.Combine(Path.GetDirectoryName(_dte.Solution.FileName), ".vs");
		Directory.CreateDirectory(solutionDotVsDir);

		var targetPath = Path.Combine(solutionDotVsDir, "mcp.json");

		// Read existing content (if any)
		JsonNode root;
		var jsonOptions = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
		var nodeOptions = new JsonSerializerOptions { WriteIndented = true };

		if (File.Exists(targetPath))
		{
			using var fs = File.Open(targetPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var doc = JsonDocument.Parse(fs, jsonOptions);
			root = JsonNode.Parse(doc.RootElement.GetRawText()) ?? new JsonObject();
		}
		else
		{
			root = new JsonObject();
		}

		// Ensure an object root
		if (root is not JsonObject objRoot)
		{
			// If somehow it wasn't an object, replace with object and tuck old root under _original to avoid data loss
			objRoot = new JsonObject { ["_original"] = root };
			root = objRoot;
		}

		// Ensure "servers" object exists
		var servers = objRoot["servers"] as JsonObject ?? new JsonObject();
		objRoot["servers"] = servers;

		// Build the "uno-devserver" MCP server entry
		var unoDevServerObj = new JsonObject()
		{
			["url"] = $"http://localhost:{_devServer.Value.port}/mcp",
		};

		// Overwrite/insert "uno-devserver"
		servers["uno-devserver"] = unoDevServerObj;

		var outputText = root.ToJsonString(nodeOptions);

		await WriteMCPFileAsync(solutionDotVsDir, targetPath, outputText, ct);
	}

	private async Task WriteMCPFileAsync(string solutionDotVsDir, string targetPath, string outputText, CancellationToken ct)
	{
		// Write to a temp file in the same directory for atomic replace
		var tempPath = Path.Combine(solutionDotVsDir, $".mcp.json.{Guid.NewGuid():N}.tmp");
		var retryDelays = new[] { 50, 100, 200, 400, 800 }; // ms

		try
		{
			File.WriteAllText(tempPath, outputText, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

			// Atomic replace when destination exists; otherwise just move.
			if (File.Exists(targetPath))
			{
				// On Windows this is atomic; also creates a backup you can ignore or keep.
				var backupPath = Path.Combine(solutionDotVsDir, $".mcp.json.backup.{DateTime.UtcNow:yyyyMMddHHmmssfff}");
				int attempt = 0;
				while (!ct.IsCancellationRequested)
				{
					try
					{
						File.Replace(tempPath, targetPath, backupPath, ignoreMetadataErrors: true);
						// Optionally remove backup; comment the next line if you prefer to keep it.
						try { File.Delete(backupPath); } catch { /* best effort */ }
						break;
					}
					catch (IOException) when (attempt < retryDelays.Length)
					{
						await Task.Delay(retryDelays[attempt++], ct);
						continue;
					}
				}
			}
			else
			{
				// First write — race-safe move with retries
				int attempt = 0;
				while (!ct.IsCancellationRequested)
				{
					try
					{
						File.Move(tempPath, targetPath); // if someone beat us, this will throw
						break;
					}
					catch (IOException) when (File.Exists(targetPath) && attempt < retryDelays.Length)
					{
						// The file appeared — fallback to replace path
						try
						{
							var backupPath = Path.Combine(solutionDotVsDir, $".mcp.json.backup.{DateTime.UtcNow:yyyyMMddHHmmssfff}");
							File.Replace(tempPath, targetPath, backupPath, ignoreMetadataErrors: true);
							try { File.Delete(backupPath); } catch { /* best effort */ }
							break;
						}
						catch (IOException) when (attempt < retryDelays.Length)
						{
							await Task.Delay(retryDelays[attempt++], ct);
						}
					}
				}
			}
		}
		catch (Exception ex)
		{
			try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { /* ignore */ }

			// failed to write mcp.json; log and move on
			_warningAction?.Invoke(
				$"Failed to setup the MCP server endpoint in VS, tools might not be available in Copilot. ({ex.Message})");
		}
	}
}

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
#if __SKIA__
using Uno.Foundation;
#endif

namespace Uno.UI.Samples.UITests.Helpers
{
	public static partial class SkiaSamplesAppHelper
	{
		/// <summary>
		/// This method is for saving files on Skia-WASM where we don't have disk access.
		/// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		public static async Task SaveFile(string filePath, string content, CancellationToken? ct = null)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
#if __SKIA__
			if (OperatingSystem.IsBrowser())
			{
				// For Skia-Wasm, we assumes a web server for saving files is running at the same hostname and
				// at a port 1 more than the port that the app is currently served at (usually 8000 and 8001).
				// The web server accepts a POST request with a json body that includes the path and content of the
				// file. For more details, look at build/test-scripts/skia-browserwasm-file-creation-server.py
				var json = JsonSerializer.Serialize(new { FilePath = filePath, Content = content });
				using (var client = new HttpClient())
				{
					var protocol = WebAssemblyImports.EvalString("window.location.protocol");
					var hostname = WebAssemblyImports.EvalString("window.location.hostname");
					if (!int.TryParse(WebAssemblyImports.EvalString("window.location.port"), out var port))
					{
						port = protocol == "http:" ? 80 : 443;
					}

					Console.WriteLine($"Writing file {filePath} to {protocol}//{hostname}:{port + 1}");

					var payload = new StringContent(json, Encoding.UTF8, "application/json");
					var response = ct is { }
						? await client.PostAsync($"{protocol}//{hostname}:{port + 1}", payload, ct.Value)
						: await client.PostAsync($"{protocol}//{hostname}:{port + 1}", payload);

					if (response.StatusCode != HttpStatusCode.OK)
					{
						throw new InvalidOperationException($"Failed to write test results to disk with status code {response.StatusCode}. Response content: ${await response.Content.ReadAsStringAsync()}");
					}
				}
			}
			else
#endif
			{
				File.WriteAllText(filePath, content, System.Text.Encoding.Unicode);
			}
		}
	}
}

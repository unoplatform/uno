using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Samples.UITests.Helpers
{
	public static partial class SkiaSamplesAppHelper
	{
		[JSImport("globalThis.eval")]
		private static partial string EvalPrivate(string js);

		public static string Eval(string js)
		{
#if __SKIA__
			if (!OperatingSystem.IsBrowser())
#endif
			{
				throw new InvalidOperationException($"{nameof(Eval)} is only available on the Skia-WebAssembly target.");
			}

			return EvalPrivate(js);
		}

		/// <summary>
		/// This method is for saving files on Skia-WASM where we don't have disk access.
		/// </summary>
		public static async Task SaveFile(string filePath, string content, CancellationToken? ct = null)
		{
#if __SKIA__
			if (OperatingSystem.IsBrowser())
			{
				// For Skia-Wasm, we assumes a web server for saving files is running at the same hostname and
				// at a port 1 more than the port that the app is currently served at (usually 8000 and 8001).
				// The web server accepts a POST request with a json body that includes the path and content of the
				// file.
				var json = JsonSerializer.Serialize(new { FilePath = filePath, Content = content });
				using (var client = new HttpClient())
				{
					var protocol = Eval("window.location.protocol");
					var hostname = Eval("window.location.hostname");
					if (!int.TryParse(Eval("window.location.port"), out var port))
					{
						port = protocol == "http:" ? 80 : 443;
					}

					var payload = new StringContent(json, Encoding.UTF8, "application/json");
					var response = ct is { }
						? await client.PostAsync($"{protocol}//{hostname}:{port + 1}", payload, ct.Value)
						: await client.PostAsync($"{protocol}//{hostname}:{port + 1}", payload);

					if (response.StatusCode != HttpStatusCode.OK)
					{
						throw new InvalidOperationException("Failed to write test results to disk.");
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

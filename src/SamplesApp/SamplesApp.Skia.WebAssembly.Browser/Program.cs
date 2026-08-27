using System;
using System.Runtime.InteropServices.JavaScript;
using Uno.UI.Hosting;

// Seed environment variables from the URL before any backend/knob reads them, so a single publish
// switches drawing backends and benchmarking knobs at runtime:
//   ?env=UNO_WEBGPU=1,UNO_LOG_FRAME_PHASES=1,UNO_SHOW_FPS=1
SeedEnvironmentFromQuery();

var builder = UnoPlatformHostBuilder.Create()
	.App(() => new SamplesApp.App())
	.UseWebAssembly();

// Register the drawing backend + content seams (Skia by default; WebGPU + managed seams for a SkiaSharp-free build).
// Shared with every SamplesApp head. The WebGPU render path additionally requires publishing with
// -p:UnoWebGpuWasm=true so that Dawn/emdawnwebgpu is linked.
SamplesApp.DrawingBackendConfiguration.Configure(builder);

await builder.Build().RunAsync();

static void SeedEnvironmentFromQuery()
{
	try
	{
		var search = JSHost.GlobalThis.GetPropertyAsJSObject("location")?.GetPropertyAsString("search");
		if (string.IsNullOrEmpty(search))
		{
			return;
		}

		foreach (var param in search.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
		{
			var eq = param.IndexOf('=');
			if (eq < 0 || param[..eq] != "env")
			{
				continue;
			}

			foreach (var pair in Uri.UnescapeDataString(param[(eq + 1)..]).Split(',', StringSplitOptions.RemoveEmptyEntries))
			{
				var kv = pair.Split('=', 2);
				if (kv.Length == 2 && kv[0].StartsWith("UNO_", StringComparison.Ordinal))
				{
					Environment.SetEnvironmentVariable(kv[0], kv[1]);
					Console.WriteLine($"[env] {kv[0]}={kv[1]} (from URL)");
				}
			}
		}
	}
	catch (Exception ex)
	{
		Console.WriteLine($"[env] query parsing failed: {ex.Message}");
	}
}

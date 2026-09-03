#nullable enable
#pragma warning disable IDE0055 // dev-only capture hook; formatting analyzer disagrees with the function-pointer syntax

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace SamplesApp;

/// <summary>
/// Arms an automated RenderDoc capture when the app runs under renderdoccmd (which injects renderdoc.dll):
/// <c>UNO_RENDERDOC_CAPTURE=delaySeconds:frameCount</c> triggers a multi-frame capture after the delay,
/// saved to renderdoccmd's <c>-c</c> capture-file template. No-op when RenderDoc isn't injected.
/// </summary>
internal static unsafe class RenderDocCapture
{
	[DllImport("kernel32")]
	private static extern IntPtr GetModuleHandleW([MarshalAs(UnmanagedType.LPWStr)] string name);

	[DllImport("kernel32")]
	private static extern IntPtr GetProcAddress(IntPtr module, [MarshalAs(UnmanagedType.LPStr)] string name);

	public static void ArmFromEnvironment()
	{
		if (!OperatingSystem.IsWindows()
			|| Environment.GetEnvironmentVariable("UNO_RENDERDOC_CAPTURE") is not { Length: > 0 } spec)
		{
			return;
		}

		var parts = spec.Split(':');
		var delaySeconds = int.TryParse(parts[0], out var d) ? d : 5;
		var frameCount = parts.Length > 1 && uint.TryParse(parts[1], out var f) ? f : 1u;

		var module = GetModuleHandleW("renderdoc.dll");
		if (module == IntPtr.Zero)
		{
			Console.WriteLine("RENDERDOC: renderdoc.dll not loaded; capture not armed.");
			return;
		}

		var getApi = (delegate* unmanaged[Cdecl]<int, void**, int>)GetProcAddress(module, "RENDERDOC_GetAPI");
		void* api = null;
		if (getApi is null || getApi(10102 /* eRENDERDOC_API_Version_1_1_2 */, &api) != 1 || api is null)
		{
			Console.WriteLine("RENDERDOC: RENDERDOC_GetAPI failed; capture not armed.");
			return;
		}

		// RENDERDOC_API_1_1_2 function table entries used below (see renderdoc_app.h):
		// 12 = GetCaptureFilePathTemplate, 13 = GetNumCaptures, 14 = GetCapture, 22 = TriggerMultiFrameCapture.
		var table = (void**)api;
		var getTemplate = (delegate* unmanaged[Cdecl]<byte*>)table[12];
		var getNumCaptures = (delegate* unmanaged[Cdecl]<uint>)table[13];
		var getCapture = (delegate* unmanaged[Cdecl]<uint, byte*, uint*, ulong*, uint>)table[14];
		var trigger = (delegate* unmanaged[Cdecl]<uint, void>)table[22];

		new Thread(() =>
		{
			Thread.Sleep(TimeSpan.FromSeconds(delaySeconds));
			var template = Marshal.PtrToStringAnsi((IntPtr)getTemplate());
			Console.WriteLine($"RENDERDOC: triggering {frameCount}-frame capture (template: '{template}').");
			trigger(frameCount);

			// Poll for the capture registering, then report where it landed.
			for (var i = 0; i < 20; i++)
			{
				Thread.Sleep(500);
				var count = getNumCaptures();
				if (count > 0)
				{
					var path = stackalloc byte[512];
					uint len = 512;
					getCapture(count - 1, path, &len, null);
					Console.WriteLine($"RENDERDOC: {count} capture(s); latest: {Marshal.PtrToStringAnsi((IntPtr)path)}");
					return;
				}
			}

			Console.WriteLine("RENDERDOC: trigger fired but no capture registered after 10s.");
		})
		{
			IsBackground = true,
			Name = "RenderDoc capture trigger",
		}.Start();
	}
}

using System;
using System.IO;
using System.Runtime.InteropServices;
using Uno.Foundation;
using Uno.Foundation.Interop;
using Uno.Foundation.Logging;

namespace Windows.Storage;

partial class ApplicationData
{
	internal static void Init()
	{
		// Nothing to do here, this is only a way to explicitly poke the 'ApplicationData' so the
		// 'Current' is instantiated and the 'InitializePartial' is invoked.
	}

	async partial void InitializePartial()
	{
		try
		{
			await StorageFolder.MakePersistentAsync(
				LocalFolder,
				RoamingFolder,
				// TemporaryFolder.Path: No needs to persist it!
				// LocalCacheFolder.Path: Usually this does needs to be persisted, so keep it disable by default for perf consideration
				SharedLocalFolder);
		}
		catch (Exception ex)
		{
			this.LogError()?.LogError("Failed to initialize ApplicationData folders", ex);
		}
	}

	private static string GetLocalCacheFolder()
		=> WebAssemblyRuntime.IsWebAssembly ? "/cache" : @".\cache";

	private static string GetTemporaryFolder()
		=> WebAssemblyRuntime.IsWebAssembly ? "/temp" : @".\temp";

	private static string GetLocalFolder()
		=> WebAssemblyRuntime.IsWebAssembly ? "/local" : @".\local";

	private static string GetRoamingFolder()
		=> WebAssemblyRuntime.IsWebAssembly ? "/roaming" : @".\roaming";

	private static string GetSharedLocalFolder()
		=> WebAssemblyRuntime.IsWebAssembly ? "/shared" : @".\shared";
}

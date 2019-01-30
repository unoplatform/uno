using System;
using System.IO;
using Uno.Foundation;

namespace Windows.Storage
{
	public  partial class ApplicationData 
	{
		private static string GetLocalCacheFolder()
			=> WebAssemblyRuntime.IsWebAssembly ? "/cache" : @".\cache";

		private static string GetTemporaryFolder()
			=> WebAssemblyRuntime.IsWebAssembly ? "/temp" : @".\temp";

		private static string GetLocalFolder()
			=> WebAssemblyRuntime.IsWebAssembly ? "/local" : @".\local";
	}
}

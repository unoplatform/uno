using System;
using System.IO;

namespace Windows.Storage
{
	public  partial class ApplicationData 
	{
		private static string GetLocalCacheFolder()
			=> "/cache";

		private static string GetTemporaryFolder()
			=> "/temp";

		private static string GetLocalFolder()
			=> "/local";
	}
}

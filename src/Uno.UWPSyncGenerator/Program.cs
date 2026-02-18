using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Uno.UWPSyncGenerator
{
	class Program
	{
		const string SyncMode = "sync";
		const string DocMode = "doc";
		const string AllMode = "all";

		static async Task Main(string[] args)
		{
			Directory.SetCurrentDirectory(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

			DeleteDirectoryIfExists(@"..\..\..\Uno.UI\Generated\");
			DeleteDirectoryIfExists(@"..\..\..\Uno.UWP\Generated\");
			DeleteDirectoryIfExists(@"..\..\..\Uno.Foundation\Generated\");
			DeleteDirectoryIfExists(@"..\..\..\Uno.UI.Composition\Generated\");
			DeleteDirectoryIfExists(@"..\..\..\Uno.UI.Dispatching\Generated\");

			if (args.Length == 0)
			{
				Console.WriteLine("No mode selected. Supported modes: doc, sync & all.");
				return;
			}

			var mode = args[0].ToLowerInvariant();

			if (mode == SyncMode || mode == AllMode)
			{
				await new SyncGenerator().Build();
			}

			if (mode == DocMode || mode == AllMode)
			{
				await new DocGenerator().Build();
			}
		}

		private static void DeleteDirectoryIfExists(string path)
		{
			if (Directory.Exists(path))
				Directory.Delete(path, recursive: true);
		}
	}
}

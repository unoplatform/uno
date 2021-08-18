using System;
using System.IO;

namespace ActionRunner
{
	internal class Program
    {
		private const string UnoPathPrefix = "uno/src/Uno.UI/Microsoft/UI/Xaml/Controls";
		private const string MUXPathPrefix = "microsoft-ui-xaml/dev";

		private static int Main()
        {
			if (!Directory.Exists(MUXPathPrefix))
			{
				GitHubLogger.LogError($"Can't find '{MUXPathPrefix}' directory.");
				return 1;
			}
			else if (!Directory.Exists(UnoPathPrefix))
			{
				GitHubLogger.LogError($"Can't find '{UnoPathPrefix}' directory.");
			}

			string[] unoResourceFiles = CollectResourcesFromUno();
			GitHubLogger.LogInformation($"Found {unoResourceFiles.Length} resources files.");

			foreach (string unoPath in unoResourceFiles)
			{
				var muxPath = MapUnoToMUX(unoPath);
				if (!File.Exists(muxPath))
				{
					GitHubLogger.LogWarning($"Path '{muxPath}' was not found.");
					continue;
				}

				var muxContent = File.ReadAllText(muxPath);
				var unoContent = File.ReadAllText(unoPath);
				if (muxContent != unoContent)
				{
					GitHubLogger.LogInformation($"Files '{muxPath}' and '{unoPath}' are not identical!!!");
					File.Copy(muxPath, unoPath, overwrite: true);
				}
			}

			return 0;
        }

		private static string[] CollectResourcesFromUno()
			=> Directory.GetFiles(UnoPathPrefix, "*.resw", SearchOption.AllDirectories);

		private static string MapUnoToMUX(string unoPath)
		{
			if (!unoPath.StartsWith(UnoPathPrefix))
			{
				throw new InvalidOperationException($"Expected path to start with '{UnoPathPrefix}'. Found: '{MUXPathPrefix}'.");
			}

			return Path.Combine(MUXPathPrefix, Path.GetRelativePath(relativeTo: UnoPathPrefix, unoPath));
		}
    }
}

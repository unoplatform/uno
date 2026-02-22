namespace Uno.UI.DevServer.Cli.Helpers;

internal static class NuGetCacheHelper
{
	internal static IReadOnlyList<string> GetNuGetCachePaths()
	{
		var paths = new List<string>
		{
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				".nuget", "packages"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
				"NuGet", "packages"),
		};
		var nugetEnv = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
		if (!string.IsNullOrWhiteSpace(nugetEnv))
		{
			paths.Add(nugetEnv);
		}
		return paths;
	}
}

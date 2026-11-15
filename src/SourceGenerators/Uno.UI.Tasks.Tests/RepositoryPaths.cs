namespace Uno.UI.Tasks.Tests;

internal static class RepositoryPaths
{
	private static readonly Lazy<string> _root = new(FindRoot);

	public static string Root => _root.Value;

	public static string Get(params string[] relative) => Path.Combine([Root, .. relative]);

	private static string FindRoot()
	{
		for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
		{
			if (File.Exists(Path.Combine(directory.FullName, "build", "nuget", "uno.winui.runtime-replace.targets")))
			{
				return directory.FullName;
			}
		}

		throw new InvalidOperationException($"Could not find the repository root above '{AppContext.BaseDirectory}'.");
	}
}

namespace Uno.UI.SourceGenerators.Tests.XamlCodeGeneratorTests;

/// <summary>
/// Guards the length of the committed generator snapshot paths.
/// </summary>
/// <remarks>
/// These are among the deepest paths in the repository and a new one appears with every new test, while
/// Windows still caps a path at 260 characters for anything that has not opted into long paths. A clone
/// -- or a git worktree -- a few folders too deep then fails to check the repository out at all, which
/// is what these paths used to do before the snapshot layout was flattened.
/// </remarks>
[TestClass]
public class Given_SnapshotLayout
{
	// 260 (MAX_PATH) less ~95 characters for the folder the repository is cloned into.
	private const int MaxRepositoryRelativePathLength = 165;

	[TestMethod]
	public void When_Snapshot_Paths_Are_Within_Budget()
	{
		var projectFolder = Path.GetFullPath(Path.Combine("..", "..", ".."));
		var repositoryFolder = Path.GetFullPath(Path.Combine(projectFolder, "..", "..", ".."));
		var snapshotFolder = Path.Combine(projectFolder, "Out");

		// Without this the assertion below passes on an empty enumeration if the layout ever moves.
		Directory.Exists(snapshotFolder).Should().BeTrue($"the snapshot folder should be at {snapshotFolder}");

		var tooLong = Directory
			.EnumerateFiles(snapshotFolder, "*", SearchOption.AllDirectories)
			.Select(path => Path.GetRelativePath(repositoryFolder, path).Replace('\\', '/'))
			.Where(path => path.Length > MaxRepositoryRelativePathLength)
			.OrderByDescending(path => path.Length)
			.ToArray();

		tooLong.Should().BeEmpty(
			$"snapshot paths must stay within {MaxRepositoryRelativePathLength} characters -- shorten the " +
			"test method name, or the XAML fixture the generated file is named after");
	}

	[TestMethod]
	public void When_Snapshots_Live_Outside_The_Project_Root_Folder()
	{
		var projectFolder = Path.GetFullPath(Path.Combine("..", "..", ".."));
		var legacyFolder = Path.Combine(projectFolder, "XamlCodeGeneratorTests", "Out");

		// The csproj globs reach Out/ at the project root only. A test merged from a branch written
		// against the older layout lands its snapshots here instead, where they are compiled into the
		// assembly as ordinary sources rather than embedded, and the build breaks on the XAML they emit.
		var strays = Directory.Exists(legacyFolder)
			? Directory.EnumerateFiles(legacyFolder, "*.cs", SearchOption.AllDirectories).ToArray()
			: [];

		strays.Should().BeEmpty(
			$"snapshots belong under {Path.Combine(projectFolder, "Out")} -- move them there and delete " +
			legacyFolder);
	}
}

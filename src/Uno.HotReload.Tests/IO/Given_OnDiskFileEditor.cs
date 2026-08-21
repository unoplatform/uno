using AwesomeAssertions;
using Uno.HotReload.IO;

namespace Uno.HotReload.Tests.IO;

[TestClass]
public class Given_OnDiskFileEditor
{
	public TestContext TestContext { get; set; } = null!;

	[TestMethod]
	[Description(
		"A file creation must also create its missing parent directories: Hot Design creates pages " +
		"in folders that may not exist yet, and File.WriteAllTextAsync fails with " +
		"DirectoryNotFoundException otherwise.")]
	public async Task When_CreateFileInMissingDirectory_Then_ParentDirectoriesAreCreated()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		var root = Directory.CreateTempSubdirectory().FullName;
		try
		{
			var filePath = Path.Combine(root, "New", "Nested", "Page.xaml");

			var (result, error) = await new OnDiskFileEditor().EditAsync(
				new FileEdit(filePath, OldText: null, NewText: "<Page />", IsCreateDeleteAllowed: true),
				forceSaveOnDisk: null,
				ct);

			error.Should().BeNull();
			result.Should().Be(FileUpdateResult.Success);
			(await File.ReadAllTextAsync(filePath, ct)).Should().Be("<Page />");
		}
		finally
		{
			Directory.Delete(root, recursive: true);
		}
	}

	[TestMethod]
	[Description("A write to a missing file without create permission must keep reporting FileNotFound.")]
	public async Task When_WriteToMissingFileWithoutCreate_Then_FileNotFound()
	{
		var ct = TestContext.CancellationTokenSource.Token;
		var filePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "Page.xaml");

		var (result, _) = await new OnDiskFileEditor().EditAsync(
			new FileEdit(filePath, OldText: null, NewText: "<Page />"),
			forceSaveOnDisk: null,
			ct);

		result.Should().Be(FileUpdateResult.FileNotFound);
		Directory.Exists(Path.GetDirectoryName(filePath)).Should().BeFalse("the guard must not create directories");
	}
}

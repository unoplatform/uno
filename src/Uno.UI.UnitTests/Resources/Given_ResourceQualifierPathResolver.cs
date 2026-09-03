using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Tasks.ResourcesGenerator;
using Windows.ApplicationModel.Resources.Core;

namespace Uno.UI.Tests.Resources;

[TestClass]
public class Given_ResourceQualifierPathResolver
{
	// Absolute item spec inside the project: everything above the project is dropped.
	[DataRow(@"\P\OS\MyProject\Strings\fr\Resources.resw", null, @"Resources.resw", @"\P\OS\MyProject\", @"\P\OS\MyProject\", @"Strings\fr\Resources.resw")]
	// Link is authored intent and wins over everything else.
	[DataRow(@"\Elsewhere\Strings\de\Resources.resw", @"Strings\de\Resources.resw", @"Strings\de\Resources.resw", @"\P\MyProject\", @"\P\MyProject\", @"Strings\de\Resources.resw")]
	// Relative item specs are authored as-is, even when they climb out of the project.
	[DataRow(@"Strings\fr\Resources.resw", null, @"Strings\fr\Resources.resw", @"\P\MyProject\", @"\P\MyProject\", @"Strings\fr\Resources.resw")]
	[DataRow(@"..\Shared\Strings\de\Resources.resw", null, @"Resources.resw", @"\P\MyProject\", @"\P\MyProject\", @"..\Shared\Strings\de\Resources.resw")]
	// Items coming from another project are relative to that project's directory.
	[DataRow(@"\P\Shared\Strings\it\Resources.resw", null, @"Resources.resw", @"\P\Shared\", @"\P\MyProject\", @"Strings\it\Resources.resw")]
	// A directory prefix only matches on a segment boundary: MyProject2 is not inside MyProject.
	[DataRow(@"\P\MyProject2\Strings\fr\Resources.resw", null, @"Resources.resw", @"\P\MyProject", @"\P\MyProject", @"\P\MyProject2\Strings\fr\Resources.resw")]
	// Out of every cone: an explicit TargetPath still describes the layout...
	[DataRow(@"\Elsewhere\x.resw", null, @"Strings\de\Resources.resw", @"\P\MyProject\", @"\P\MyProject\", @"Strings\de\Resources.resw")]
	// ...but the bare file name AssignTargetPath falls back to does not, so keep the full path.
	[DataRow(@"\Elsewhere\Strings\de\Resources.resw", null, @"Resources.resw", @"\P\MyProject\", @"\P\MyProject\", @"\Elsewhere\Strings\de\Resources.resw")]
	[TestMethod]
	public void When_Resolve(string itemSpec, string link, string targetPath, string definingProjectDirectory, string projectDirectory, string expected)
	{
		var resolved = ResourceQualifierPathResolver.Resolve(
			Native(itemSpec),
			Native(link),
			Native(targetPath),
			Native(definingProjectDirectory),
			Native(projectDirectory));

		Assert.AreEqual(Native(expected), resolved);
	}

	// https://github.com/unoplatform/uno/issues/3157 (folder named `OS`, i.e. Ossetic)
	// https://github.com/unoplatform/uno/issues/4657 (folder named `cs`, i.e. Czech)
	[DataRow(@"\P\OS\MyProject\Strings\Resources.resw", @"\P\OS\MyProject\", null)]
	[DataRow(@"\code\cs\MyUnoProject\Strings\Resources.resw", @"\code\cs\MyUnoProject\", null)]
	[DataRow(@"\P\OS\MyProject\Strings\fr\Resources.resw", @"\P\OS\MyProject\", "fr")]
	[DataRow(@"\code\cs\MyUnoProject\Strings\en\Resources.resw", @"\code\cs\MyUnoProject\", "en")]
	[TestMethod]
	public void When_Project_Directory_Contains_A_Language_Tag(string itemSpec, string projectDirectory, string expectedLanguage)
	{
		var itemSpecPath = Native(itemSpec);
		var projectDirectoryPath = Native(projectDirectory);

		var qualifierPath = ResourceQualifierPathResolver.Resolve(
			itemSpecPath,
			link: null,
			targetPath: itemSpecPath.Substring(projectDirectoryPath.Length),
			definingProjectDirectory: projectDirectoryPath,
			projectDirectory: projectDirectoryPath);

		var resourceCandidate = ResourceCandidate.Parse(itemSpecPath, qualifierPath);

		Assert.AreEqual(expectedLanguage, resourceCandidate.GetQualifierValue("language"));
	}

	// `Link` and `TargetPath` keep the separator the project authored, which is not necessarily
	// the build host's. Qualifiers are split on the host separator, so an unaligned path yields none.
	[DataRow('/')]
	[DataRow('\\')]
	[TestMethod]
	public void When_Authored_Separator_Is_Not_The_Host_Separator(char separator)
	{
		string Authored(string path) => path.Replace('\\', separator);

		var itemSpec = Native(@"\Elsewhere\Resources.resw");

		var qualifierPath = ResourceQualifierPathResolver.Resolve(
			itemSpec,
			link: Authored(@"Strings\fr\Resources.resw"),
			targetPath: null,
			definingProjectDirectory: Native(@"\P\MyProject\"),
			projectDirectory: Native(@"\P\MyProject\"));

		Assert.AreEqual(Native(@"Strings\fr\Resources.resw"), qualifierPath);
		Assert.AreEqual("fr", ResourceCandidate.Parse(itemSpec, qualifierPath).GetQualifierValue("language"));
	}

	// The item spec and the project directory may also disagree on separators.
	[DataRow('/')]
	[DataRow('\\')]
	[TestMethod]
	public void When_Project_Directory_Separator_Is_Not_The_Host_Separator(char separator)
	{
		string Authored(string path) => path.Replace('\\', separator);

		var qualifierPath = ResourceQualifierPathResolver.Resolve(
			Authored(@"\P\MyProject\Strings\de\Resources.resw"),
			link: null,
			targetPath: null,
			definingProjectDirectory: Authored(@"\P\MyProject\"),
			projectDirectory: Authored(@"\P\MyProject\"));

		Assert.AreEqual(Native(@"Strings\de\Resources.resw"), qualifierPath);
	}

	private static string Native(string path)
		=> path?.Replace('\\', Path.DirectorySeparatorChar);
}

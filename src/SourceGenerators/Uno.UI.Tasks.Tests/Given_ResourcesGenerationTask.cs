using AwesomeAssertions;
using Microsoft.Build.Utilities;
using Uno.UI.Tasks.ResourcesGenerator;

namespace Uno.UI.Tasks.Tests;

[TestClass]
public class Given_ResourcesGenerationTask
{
	private static readonly DateTime OldStamp = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	[TestMethod]
	public void When_Android_Resources_Generated_Then_Stamp_Is_Written()
	{
		using var project = new ResourceProject(nameof(When_Android_Resources_Generated_Then_Stamp_Is_Written));
		project.AddResw("nb-NO");

		project.Run().Should().BeTrue();

		File.Exists(project.StampFile).Should().BeTrue();
	}

	[TestMethod]
	public void When_Android_Resources_Unchanged_Then_Stamp_Is_Untouched()
	{
		using var project = new ResourceProject(nameof(When_Android_Resources_Unchanged_Then_Stamp_Is_Untouched));
		project.AddResw("nb-NO");
		project.Run().Should().BeTrue();
		File.SetLastWriteTimeUtc(project.StampFile, OldStamp);

		project.Run().Should().BeTrue();

		File.GetLastWriteTimeUtc(project.StampFile).Should().Be(OldStamp);
	}

	[TestMethod]
	public void When_Android_Resource_Paths_Change_Then_Stamp_Is_Updated()
	{
		using var project = new ResourceProject(nameof(When_Android_Resource_Paths_Change_Then_Stamp_Is_Updated));
		project.AddResw("nb-NO");
		project.Run().Should().BeTrue();
		File.SetLastWriteTimeUtc(project.StampFile, OldStamp);

		// A different build host can map the same culture to a different directory (nb vs no),
		// which from the Android SDK's point of view is the same as the set of files changing.
		project.RemoveResw("nb-NO");
		project.AddResw("fr-FR");
		project.Run().Should().BeTrue();

		File.GetLastWriteTimeUtc(project.StampFile).Should().BeAfter(OldStamp);
	}

	[TestMethod]
	public void When_Android_Resource_Paths_Change_Then_Stale_Files_Are_Removed()
	{
		using var project = new ResourceProject(nameof(When_Android_Resource_Paths_Change_Then_Stale_Files_Are_Removed));
		project.AddResw("nb-NO");
		project.Run().Should().BeTrue();
		var nbFiles = project.GeneratedAndroidFiles();

		project.RemoveResw("nb-NO");
		project.AddResw("fr-FR");
		project.Run().Should().BeTrue();

		project.GeneratedAndroidFiles().Should().ContainSingle().Which.Should().NotBe(nbFiles.Single());
		Directory.Exists(Path.GetDirectoryName(nbFiles.Single())).Should().BeFalse();
	}

	[TestMethod]
	public void When_Android_Resource_Content_Changes_Then_Stamp_Is_Updated()
	{
		using var project = new ResourceProject(nameof(When_Android_Resource_Content_Changes_Then_Stamp_Is_Updated));
		project.AddResw("nb-NO");
		project.Run().Should().BeTrue();
		File.SetLastWriteTimeUtc(project.StampFile, OldStamp);
		project.BackdateGeneratedFiles(OldStamp);

		project.AddResw("nb-NO", value: "Endret");
		project.Run().Should().BeTrue();

		File.GetLastWriteTimeUtc(project.StampFile).Should().BeAfter(OldStamp);
	}

	private sealed class ResourceProject : IDisposable
	{
		private readonly string _root;

		public ResourceProject(string name)
		{
			_root = Path.Combine(Path.GetTempPath(), "Uno.UI.Tasks.Tests", name + "_" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_root);
		}

		public string OutputPath => Path.Combine(_root, "obj", "g", "ResourcesGenerator");

		public string StampFile => Path.Combine(_root, "obj", "g", "uno.stamp");

		private string ResourcePath(string language) => Path.Combine(_root, "Strings", language, "Resources.resw");

		public void AddResw(string language, string value = "Hei")
		{
			var path = ResourcePath(language);
			Directory.CreateDirectory(Path.GetDirectoryName(path)!);
			File.WriteAllText(path, $"""
				<?xml version="1.0" encoding="utf-8"?>
				<root>
					<data name="Greeting" xml:space="preserve">
						<value>{value}</value>
					</data>
				</root>
				""");
		}

		public void RemoveResw(string language) => Directory.Delete(Path.GetDirectoryName(ResourcePath(language))!, recursive: true);

		public string[] GeneratedAndroidFiles()
			=> Directory.GetFiles(Path.Combine(OutputPath, "r"), "*.xml", SearchOption.AllDirectories);

		public void BackdateGeneratedFiles(DateTime time)
		{
			foreach (var file in Directory.EnumerateFiles(OutputPath, "*", SearchOption.AllDirectories))
			{
				File.SetLastWriteTimeUtc(file, time);
			}
		}

		public bool Run()
		{
			var resources = Directory
				.EnumerateFiles(Path.Combine(_root, "Strings"), "*.resw", SearchOption.AllDirectories)
				.Select(path => new TaskItem(path, new Dictionary<string, string>
				{
					["TargetPath"] = Path.GetRelativePath(_root, path),
					["DefiningProjectDirectory"] = _root + Path.DirectorySeparatorChar,
				}))
				.ToArray();

			var task = new ResourcesGenerationTask_v0
			{
				BuildEngine = new RecordingBuildEngine(),
				Resources = resources,
				TargetPlatform = "android",
				TargetProjectDirectory = _root + Path.DirectorySeparatorChar,
				OutputPath = OutputPath,
				AndroidStampFile = StampFile,
				DefaultLanguage = "en",
				ProjectName = "TestApp",
				IsUnoHead = true,
			};

			return task.Execute();
		}

		public void Dispose()
		{
			try
			{
				Directory.Delete(_root, recursive: true);
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				// Leaving a temp folder behind must never fail a test run.
			}
		}
	}
}

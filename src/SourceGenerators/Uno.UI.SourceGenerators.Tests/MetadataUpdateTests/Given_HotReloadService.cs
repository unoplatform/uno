using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Uno.UI.SourceGenerators.MetadataUpdates;

namespace Uno.UI.SourceGenerators.Tests.MetadataUpdateTests;

[TestClass]
public class Given_HotReloadService
{
	[TestMethod]
	[DynamicData(nameof(GetScenarios))]
	public async Task HR(string name, Scenario? scenario, Project[]? projects)
	{
		// Generated C# files can be found in the bin\Debug\net10.0\work sub dir.

		if (scenario != null)
		{
#if DEBUG //&& false
			if (!name.Contains("IRL_Case_001"))
			{
				Assert.Inconclusive("Ignored case.");
				return;
			}
#endif

			if (scenario.IsCrashingRoslyn)
			{
				Assert.Inconclusive("Case is known to crash roslyn.");
				return;
			}

			var results = await ApplyScenario(projects, scenario.IsDebug, scenario.IsMono, scenario.UseXamlReaderReload, name);

			if (scenario.PassResults.Length != results.Length)
			{
				Assert.Fail($"Scenario describes {scenario.PassResults.Length} results while the tests produced {results.Length} (you should have n+1 scenario.PassResults for n directory on disk).");
			}

			for (var i = 0; i < scenario.PassResults.Length; i++)
			{
				var resultValidation = scenario.PassResults[i];

				Assert.AreEqual(
					resultValidation.Diagnostics?.Length ?? 0,
					results[i].Diagnostics.Length,
					$"Diagnostics: {string.Join("\n", results[i].Diagnostics)}, " +
					$"expected {string.Join("\n", resultValidation.Diagnostics?.Select(d => d.Id) ?? Array.Empty<string>())}");
				Assert.AreEqual(resultValidation.MetadataUpdates, results[i].MetadataUpdates.Length);
			}
		}
	}

	public record ScenariosDescriptor(
		Project[] Projects,
		Scenario[] Scenarios);

	public record Project(string Name, ProjectReference[]? ProjectReferences);
	public record ProjectReference(string Name);
	public record Scenario(bool IsDebug, bool IsMono, bool IsCrashingRoslyn, bool UseXamlReaderReload, params PassResult[] PassResults)
	{
		public override string ToString()
			=> $"{(IsDebug ? "Debug" : "Release")},{(IsMono ? "MonoVM" : "NetCore")},XR:{UseXamlReaderReload}";
	}

	public record PassResult(int MetadataUpdates, params DiagnosticsResult[] Diagnostics);
	public record DiagnosticsResult(string Id);

	private static IEnumerable<object?[]> GetScenarios()
	{
		foreach (var scenarioFolder in Directory.EnumerateDirectories(ScenariosFolder, "*.*", SearchOption.TopDirectoryOnly))
		{
			var scenarioName = Path.GetFileName(scenarioFolder);
			var scenarioConfig = Path.Combine(scenarioFolder, "Scenario.json");

			if (File.Exists(scenarioConfig))
			{
				var scenariosDescriptor = ReadScenarioConfig(scenarioConfig);

				if (scenariosDescriptor is not null)
				{
					foreach (var scenario in scenariosDescriptor.Scenarios)
					{
						yield return new object?[] {
							scenarioName,
							scenario,
							scenariosDescriptor.Projects
						};
					}
				}
			}
			else
			{
				yield return new[] { scenarioName, null };
			}
		}

		static ScenariosDescriptor? ReadScenarioConfig(string path)
		{
			try
			{
				var detailsContent = File.ReadAllText(path);

				var scenarioDescriptor = System.Text.Json.JsonSerializer.Deserialize<ScenariosDescriptor>(
					detailsContent,
					new System.Text.Json.JsonSerializerOptions()
					{
						ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip,
						AllowTrailingCommas = true
					});
				return scenarioDescriptor;
			}
			catch (Exception e)
			{
				throw new InvalidOperationException($"Failed to setup scenario in {path}", e);
			}
		}
	}

	private static string ScenariosFolder
		=> Path.Combine(
			Path.GetDirectoryName(typeof(HotReloadWorkspace).Assembly.Location)!,
			"MetadataUpdateTests",
			"Scenarios");

	private async Task<HotReloadWorkspace.UpdateResult[]> ApplyScenario(
		Project[]? projects
		, bool isDebugCompilation
		, bool isMono
		, bool useXamlReaderReload
		, [CallerMemberName] string? name = null
		, CancellationToken ct = default)
	{
		if (name is null)
		{
			throw new InvalidOperationException($"A test scenario name must be provided.");
		}

		var scenarioFolder = Path.Combine(ScenariosFolder, name);
		var scenarioFile = Path.Combine(scenarioFolder, "Scenario.json");

		HotReloadWorkspace SUT = new(isDebugCompilation, isMono, useXamlReaderReload);
		List<HotReloadWorkspace.UpdateResult> results = new();

		if (projects is not null)
		{
			foreach (var project in projects)
			{
				SUT.AddProject(project.Name, (project.ProjectReferences ?? []).Select(r => r.Name).ToArray());
			}
		}

		var steps = Directory
			.GetFiles(scenarioFolder, "*.*", SearchOption.AllDirectories)
			.Where(file => file != scenarioFile)
			.OrderBy(file => file)
			.Select(file => ScenarioFileDescriptor.Create(scenarioFolder, file))
			.GroupBy(file => file.StepIndex)
			.Select(step => new Step(
				step.Key,
				step
					.GroupBy(file => file.ProjectName)
					.Select(filesPerProject => new StepProject(filesPerProject.Key, filesPerProject.Select(file => new StepFile(file.File)).ToImmutableList()))
					.ToImmutableDictionary(project => project.Name)))
			.ToImmutableDictionary(step => step.Index);

		var initialStep = steps[0];
		foreach (var project in initialStep.Projects.Values)
		{
			foreach (var file in project.Files)
			{
				var content = await File.ReadAllTextAsync(Path.Combine(scenarioFolder, initialStep.Index.ToString(), project.Name, file.Path), ct);
				if (file.IsCs)
				{
					SUT.UpdateSourceFile(project.Name, file.Path, content);
				}
				else
				{
					SUT.UpdateAdditionalFile(project.Name, file.Path, content);
				}
			}
		}
		await SUT.Initialize(ct);

		var previousStep = initialStep;
		for (var i = 1; i < steps.Count; i++)
		{
			var currentStep = steps[i];
			if (!currentStep.Projects.Keys.SequenceEqual(previousStep.Projects.Keys))
			{
				throw new InvalidOperationException("Projects removal is not yet supported.");
			}

			foreach (var project in currentStep.Projects.Values)
			{
				foreach (var file in project.Files)
				{
					var content = await File.ReadAllTextAsync(Path.Combine(scenarioFolder, currentStep.Index.ToString(), project.Name, file.Path), ct);
					if (file.IsCs)
					{
						SUT.UpdateSourceFile(project.Name, file.Path, content);
					}
					else
					{
						SUT.UpdateAdditionalFile(project.Name, file.Path, content);
					}
				}

				foreach (var removedFile in previousStep.Projects[project.Name].Files.ExceptBy(project.Files.Select(file => file.Path), previousFile => previousFile.Path))
				{
					if (removedFile.IsCs)
					{
						SUT.UpdateSourceFile(project.Name, removedFile.Path, null);
					}
					else
					{
						SUT.UpdateAdditionalFile(project.Name, removedFile.Path, null);
					}
				}
			}

			results.Add(await SUT.Update());
			previousStep = currentStep;
		}

		return results.ToArray();
	}

	private readonly record struct ScenarioFileDescriptor(int StepIndex, string ProjectName, string File)
	{
		public static ScenarioFileDescriptor Create(string scenarioFolder, string filePath)
		{
			var parts = Path.GetRelativePath(scenarioFolder, filePath).Split(Path.DirectorySeparatorChar, 3);
			return new ScenarioFileDescriptor(int.Parse(parts[0]), parts[1], parts[2]);
		}
	}

	private record Step(int Index, IImmutableDictionary<string, StepProject> Projects);
	private record StepProject(string Name, IImmutableList<StepFile> Files);
	private record StepFile(string Path) // Path relative to the scenario/step/project (i.e. scenario/step/project/{PATH})
	{
		public bool IsCs { get; } = System.IO.Path.GetExtension(Path) == ".cs";
	}
}

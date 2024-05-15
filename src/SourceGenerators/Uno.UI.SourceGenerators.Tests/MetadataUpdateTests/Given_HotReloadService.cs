using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.RemoteControl.Host.HotReload.MetadataUpdates;
using Uno.UI.SourceGenerators.MetadataUpdates;

namespace Uno.UI.SourceGenerators.Tests.MetadataUpdateTests;

[TestClass]
public class Given_HotReloadService
{
	[DataTestMethod]
	[DynamicData(nameof(GetScenarios), DynamicDataSourceType.Method)]
	public async Task HR(string name, Scenario? scenario, Project[]? projects)
	{
		if (scenario != null)
		{
			var results = await ApplyScenario(projects, scenario.IsDebug, scenario.IsMono, scenario.UseXamlReaderReload, name);

			for (int i = 0; i < scenario.PassResults.Length; i++)
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
	public record Scenario(bool IsDebug, bool IsMono, bool UseXamlReaderReload, params PassResult[] PassResults)
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
			var path = Path.Combine(scenarioFolder, "Scenario.json");

			if (File.Exists(path))
			{
				var scenariosDescriptor = ReadScenarioConfig(path);

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
		, [CallerMemberName] string? name = null)
	{
		if (name is null)
		{
			throw new InvalidOperationException($"A test scenario name must be provided.");
		}

		var scenarioFolder = Path.Combine(ScenariosFolder, name);

		HotReloadWorkspace SUT = new(isDebugCompilation, isMono, useXamlReaderReload);
		List<HotReloadWorkspace.UpdateResult> results = new();

		if (projects is not null)
		{
			foreach (var project in projects)
			{
				SUT.AddProject(
					project.Name
					, (project.ProjectReferences ?? Array.Empty<ProjectReference>()).Select(r => r.Name).ToArray());
			}
		}

		var steps = Directory
			.GetFiles(scenarioFolder, "*.*", SearchOption.AllDirectories)
			.OrderBy(f => f)
			.GroupBy(f => Path.GetRelativePath(scenarioFolder, f).Split(Path.DirectorySeparatorChar)[0]);

		int index = 0;
		foreach (var step in steps)
		{
			foreach (var file in step)
			{
				if (file == Path.Combine(scenarioFolder, "Scenario.json"))
				{
					continue;
				}

				var pathParts = Path.GetRelativePath(scenarioFolder, file).Split(Path.DirectorySeparatorChar);

				var fileContent = File.ReadAllText(file);

				if (Path.GetExtension(file) == ".cs")
				{
					SUT.SetSourceFile(pathParts[1], pathParts[2], fileContent);
				}
				else
				{
					SUT.SetAdditionalFile(pathParts[1], pathParts[2], fileContent);
				}
			}

			if (index++ == 0)
			{
				await SUT.Initialize(CancellationToken.None);
			}
			else
			{
				results.Add(await SUT.Update());
			}
		}

		return results.ToArray();
	}
}

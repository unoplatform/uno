#nullable enable

using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamplesApp.AppiumTests.Infrastructure;

namespace SamplesApp.AppiumTests.Tests;

[TestClass]
public sealed class AccessibilitySnapshotDefinitionTests
{
	[TestMethod]
	[TestCategory(TestCategories.HostIndependent)]
	public void Definitions_HaveUniqueIdsAndAutomationIds()
	{
		foreach (var definition in AccessibilityScreenReaderSnapshotDefinition.All)
		{
			definition.Elements.Select(element => element.Id)
				.Should().OnlyHaveUniqueItems();
			definition.Elements.Select(element => element.AutomationId)
				.Should().OnlyHaveUniqueItems();
		}
	}

	[TestMethod]
	[TestCategory(TestCategories.HostIndependent)]
	public void Verified_Baselines_Match_Their_Definitions()
	{
		foreach (var definition in AccessibilityScreenReaderSnapshotDefinition.All)
		{
			foreach (var platform in new[] { AppiumPlatform.Wasm })
			{
				var path = SnapshotPaths.ResolveBaselinePath(platform, definition);
				File.Exists(path).Should().BeTrue($"Missing committed baseline for {platform}: {path}");

				var snapshot = SnapshotSerializer.Read(path);
				snapshot.Should().NotBeNull();
				snapshot!.Elements.Select(element => element.Id)
					.Should().BeEquivalentTo(
						definition.ElementsFor(platform).Select(element => element.Id),
						options => options.WithoutStrictOrdering());
			}
		}
	}
}

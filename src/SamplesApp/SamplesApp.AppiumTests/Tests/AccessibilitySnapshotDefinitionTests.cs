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
	public void ComboBox_Definition_And_Baseline_Require_Independent_Value()
	{
		var definition = AccessibilityScreenReaderSnapshotDefinition.Definition;
		var comboBox = definition.Elements.Single(element => element.Id == AccessibilityScreenReaderIds.FavoriteColorComboBox);
		foreach (var platform in new[] { AppiumPlatform.Windows, AppiumPlatform.Mac, AppiumPlatform.Wasm })
		{
			comboBox.FieldsFor(platform).HasFlag(AccessibilitySnapshotFields.Value).Should().BeTrue();
		}

		var baseline = SnapshotSerializer.Read(SnapshotPaths.ResolveBaselinePath(AppiumPlatform.Wasm, definition));
		var value = baseline!.Elements.Single(element => element.Id == AccessibilityScreenReaderIds.FavoriteColorComboBox);
		value.Name.Should().Be("Favorite color");
		value.Value.Should().Be("Red");
	}

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
	public void SnapshotPath_RequiresOverride_WhenCallerPathIsUnavailable()
	{
		using var scope = new EnvironmentVariableScope();
		scope.Set(AppiumTestOptions.EnvVarSnapshotsDir, null);

		var action = () => SnapshotPaths.ResolveSnapshotsDirectory(callerFilePath: @"Z:\unavailable\AccessibilitySnapshotTests.cs");

		action.Should().Throw<InvalidDataException>()
			.WithMessage($"*{AppiumTestOptions.EnvVarSnapshotsDir}*");
	}

	[TestMethod]
	[TestCategory(TestCategories.HostIndependent)]
	public void SnapshotPath_UsesEnvironmentOverride_WhenCallerPathIsUnavailable()
	{
		using var scope = new EnvironmentVariableScope();
		var snapshotsDirectory = Path.GetFullPath("Snapshots");
		scope.Set(AppiumTestOptions.EnvVarSnapshotsDir, snapshotsDirectory);

		SnapshotPaths.ResolveSnapshotsDirectory(callerFilePath: @"Z:\unavailable\AccessibilitySnapshotTests.cs")
			.Should().Be(snapshotsDirectory);
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

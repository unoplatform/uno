using System.Collections;
using System.Collections.Immutable;
using AwesomeAssertions;
using Microsoft.CodeAnalysis;
using Uno.HotReload.Microsoft;

namespace Uno.HotReload.Tests.Microsoft;

[TestClass]
public sealed class Given_WatchHotReloadService_EngineShape
{
	private sealed class Results
	{
		public ImmutableArray<ProjectId> ProjectsToRebuild { get; init; } = [];

		public ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>> ProjectsToRestart { get; init; }
			= ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>>.Empty;
	}

	[TestMethod]
	[DataRow(nameof(Results.ProjectsToRebuild), DisplayName = "ProjectId entries (both Roslyn lines)")]
	[DataRow(nameof(Results.ProjectsToRestart), DisplayName = "Entries keyed by ProjectId (ProjectsToRestart on 5.x)")]
	public void When_ProjectSetRead_Then_EveryEntryYieldsItsProject(string name)
	{
		var id = ProjectId.CreateNewId();
		var results = new Results
		{
			ProjectsToRebuild = [id],
			ProjectsToRestart = ImmutableDictionary<ProjectId, ImmutableArray<ProjectId>>.Empty.Add(id, []),
		};

		var set = WatchHotReloadService.EngineShape.RequireProjectSet(typeof(Results), name);

		((IEnumerable)set.Property.GetValue(results)!).Cast<object>().Select(set.ReadId).Should().Equal(id);
	}
}

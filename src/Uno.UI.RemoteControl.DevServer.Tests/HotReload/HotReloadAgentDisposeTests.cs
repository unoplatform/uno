using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using Uno.UI.RemoteControl.HotReload.MetadataUpdater;

namespace Uno.UI.RemoteControl.DevServer.Tests.HotReload;

/// <summary>
/// Regression guards for the collectible-context teardown fix (issue #23704). Disposing the
/// hot-reload client agents must release the per-context state that otherwise pins the owning
/// collectible AssemblyLoadContext. The retained maps key on (or capture) types and assemblies
/// from that context, so a host that previews apps in collectible contexts leaks one context per
/// load/unload cycle until the agents are disposed. These tests fail against the pre-fix Dispose,
/// which only detached the AppDomain.AssemblyLoad subscription and left the maps populated.
/// </summary>
[TestClass]
public class HotReloadAgentDisposeTests
{
	[TestMethod]
	public void When_ElementUpdateAgentDisposed_Then_HandlerMapCleared()
	{
		// Arrange — the assembly-level [ElementMetadataUpdateHandlerAttribute]s declared for these tests
		// cause the agent to discover FrameworkElement handlers at construction.
		var agent = new ElementUpdateAgent(_ => { }, (_, _) => { });
		agent.ElementHandlerActions.Should().NotBeEmpty("handlers are discovered at construction");

		// Act
		agent.Dispose();

		// Assert — the Type-keyed handler map is released so the collectible context's element types
		// are no longer retained by this agent.
		agent.ElementHandlerActions.Should().BeEmpty(
			"Dispose must clear _elementHandlerActions so the owning collectible context is not pinned");
	}

	[TestMethod]
	public void When_HotReloadAgentDisposed_Then_DeltaAndAssemblyMapsCleared()
	{
		// Arrange
		var agent = new HotReloadAgent(_ => { });

		// Populate _deltas through the public delta path. A random ModuleId matches no loaded module, so
		// nothing is applied to the runtime — the delta is only stashed into the cache.
		agent.ApplyDeltas(new[]
		{
			new UpdateDelta
			{
				ModuleId = Guid.NewGuid(),
				MetadataDelta = Array.Empty<byte>(),
				ILDelta = Array.Empty<byte>(),
			},
		});

		// Seed _appliedAssemblies (no public path) so its clearing is also asserted.
		var applied = (ConcurrentDictionary<Assembly, Assembly>)GetField(agent, "_appliedAssemblies").GetValue(agent)!;
		var self = typeof(HotReloadAgentDisposeTests).Assembly;
		applied.TryAdd(self, self);

		Count(agent, "_deltas").Should().BeGreaterThan(0, "a delta was stashed");
		Count(agent, "_appliedAssemblies").Should().BeGreaterThan(0, "an applied assembly was seeded");

		// Act
		agent.Dispose();

		// Assert — both caches are released so the collectible context's modules/assemblies are no longer
		// retained by this agent.
		Count(agent, "_deltas").Should().Be(0, "Dispose must clear _deltas");
		Count(agent, "_appliedAssemblies").Should().Be(0, "Dispose must clear _appliedAssemblies");
	}

	private static FieldInfo GetField(object target, string name) =>
		target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
		?? throw new InvalidOperationException($"Field '{name}' not found on {target.GetType()}.");

	private static int Count(object target, string fieldName) =>
		((ICollection)GetField(target, fieldName).GetValue(target)!).Count;
}

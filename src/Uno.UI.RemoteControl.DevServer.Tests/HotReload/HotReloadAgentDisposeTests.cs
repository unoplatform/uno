using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using Uno.UI.RemoteControl.HotReload;
using Uno.UI.RemoteControl.HotReload.MetadataUpdater;

namespace Uno.UI.RemoteControl.DevServer.Tests.HotReload;

/// <summary>
/// Regression guards for the collectible-context teardown fix (issue #23704). Disposing the
/// hot-reload client agents must release the per-context state that otherwise pins the owning
/// collectible AssemblyLoadContext. The retained maps key on (or capture) types and assemblies
/// from that context, so a host that previews apps in collectible contexts leaks one context per
/// load/unload cycle until the agents are disposed. The agent-level tests fail against the pre-fix
/// Dispose, which only detached the AppDomain.AssemblyLoad subscription and left the maps populated.
/// The processor-level tests load a real copy of Uno.UI.RemoteControl into a collectible
/// AssemblyLoadContext (the copy's own <c>_processorAlc</c> then resolves to that context, so the
/// production collectible gate is exercised, not simulated) and prove that a host-driven
/// <see cref="ClientHotReloadProcessor.Dispose"/> — the load-bearing release path on browser-wasm,
/// where <see cref="AssemblyLoadContext.Unloading"/> is not raised — also releases the static
/// element agent, while a default-context processor never tears the shared agent down.
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

	[TestMethod]
	public void When_HotReloadAgentDisposed_Then_HandlerActionsCacheCleared()
	{
		// Arrange
		var agent = new HotReloadAgent(_ => { });

		// Populate the metadata-update-handler cache. Each action in UpdateHandlerActions is a delegate
		// built from a MethodInfo on a handler Type discovered by scanning this context's assemblies, so a
		// non-null cache captures delegates/Types owned by the owning collectible context and pins it.
		var handlerField = GetField(agent, "_handlerActions");
		handlerField.SetValue(agent, new HotReloadAgent.UpdateHandlerActions());
		handlerField.GetValue(agent).Should().NotBeNull("the handler-action cache was seeded");

		// Act
		agent.Dispose();

		// Assert — the cache is released so the collectible context's handler delegates/Types are no longer
		// retained by this agent. Fails against the pre-fix Dispose, which left _handlerActions populated.
		handlerField.GetValue(agent).Should().BeNull(
			"Dispose must null _handlerActions so the owning collectible context is not pinned");
	}

	[TestMethod]
	public void When_ElementUpdateAgentDisposed_Then_AssemblyLoadSubscriptionDetached()
	{
		// Arrange
		var agent = new ElementUpdateAgent(_ => { }, (_, _) => { });
		IsSubscribedToAssemblyLoad(agent).Should().BeTrue(
			"the agent subscribes to AppDomain.AssemblyLoad at construction");

		// Act
		agent.Dispose();

		// Assert — the process-wide subscription is the primary pin on the owning collectible
		// context (the delegate target is the agent, which captures the context's types).
		IsSubscribedToAssemblyLoad(agent).Should().BeFalse(
			"Dispose must detach the process-wide AppDomain.AssemblyLoad subscription");
	}

	[TestMethod]
	public void When_CollectibleContextProcessorDisposed_Then_PerContextStaticsReleased()
	{
		// Arrange — real collectible-context copy of the processor assembly (see class remarks).
		var (copy, processorType) = LoadCollectibleProcessorCopy(
			nameof(When_CollectibleContextProcessorDisposed_Then_PerContextStaticsReleased));

		var elementAgentField = GetStaticField(processorType, "_elementAgent");
		var elementAgent = CreateElementAgentIn(copy);
		elementAgentField.SetValue(null, elementAgent);
		IsSubscribedToAssemblyLoad(elementAgent).Should().BeTrue(
			"the seeded element agent subscribes to AppDomain.AssemblyLoad at construction");

		var processor = (IDisposable)Activator.CreateInstance(processorType, new object?[] { null })!;

		// Act — host-driven dispose, the load-bearing release path on runtimes where
		// AssemblyLoadContext.Unloading is not raised (browser-wasm, dotnet/runtime#34072).
		processor.Dispose();

		// Assert — the per-context statics are released so nothing pins the collectible context.
		elementAgentField.GetValue(null).Should().BeNull(
			"Dispose on a collectible-context processor must release the static element agent");
		IsSubscribedToAssemblyLoad(elementAgent).Should().BeFalse(
			"the released agent's process-wide AppDomain.AssemblyLoad subscription must be detached");

		// Double-dispose must be a safe no-op (idempotent teardown).
		var secondDispose = () => processor.Dispose();
		secondDispose.Should().NotThrow("the collectible teardown flow must be idempotent");
	}

	[TestMethod]
	public void When_DefaultContextProcessorDisposed_Then_SharedElementAgentUntouched()
	{
		// Arrange — this test assembly loads Uno.UI.RemoteControl into the default ALC, so the
		// processor under test runs the default-context (live host) Dispose path.
		var elementAgentField = GetStaticField(typeof(ClientHotReloadProcessor), "_elementAgent");
		elementAgentField.GetValue(null).Should().BeNull(
			"test isolation — no other test should leave a shared element agent behind");

		var agent = new ElementUpdateAgent(_ => { }, (_, _) => { });
		try
		{
			elementAgentField.SetValue(null, agent);
			var processor = new ClientHotReloadProcessor(null!);

			// Act
			processor.Dispose();

			// Assert — a live host-context processor must never tear down the shared element agent.
			elementAgentField.GetValue(null).Should().BeSameAs(agent,
				"disposing a default-context processor must not release the shared element agent");
			IsSubscribedToAssemblyLoad(agent).Should().BeTrue(
				"the shared element agent must stay subscribed to AppDomain.AssemblyLoad");
		}
		finally
		{
			elementAgentField.SetValue(null, null);
			agent.Dispose();
		}
	}

	[TestMethod]
	public void When_TearDownForAlcUnload_WithLiveInstance_Then_TearsDownWithoutRecursion()
	{
		// Arrange — separate collectible copy, with both the static instance and the static element
		// agent populated, mirroring a fully-initialized processor at context-unload time.
		var (copy, processorType) = LoadCollectibleProcessorCopy(
			nameof(When_TearDownForAlcUnload_WithLiveInstance_Then_TearsDownWithoutRecursion));

		var elementAgentField = GetStaticField(processorType, "_elementAgent");
		var instanceField = GetStaticField(processorType, "_instance");
		var elementAgent = CreateElementAgentIn(copy);
		elementAgentField.SetValue(null, elementAgent);

		var processor = Activator.CreateInstance(processorType, new object?[] { null })!;
		instanceField.SetValue(null, processor);

		var tearDown = processorType.GetMethod("TearDownForAlcUnload", BindingFlags.NonPublic | BindingFlags.Static)
			?? throw new InvalidOperationException($"Method 'TearDownForAlcUnload' not found on {processorType}.");

		// Act — the Unloading-path teardown disposes the instance, whose Dispose in turn releases
		// the per-context statics; this must not recurse back into TearDownForAlcUnload (a recursive
		// flow would overflow the stack and crash the test host here).
		tearDown.Invoke(null, null);

		// Assert
		instanceField.GetValue(null).Should().BeNull("teardown must clear the static processor instance");
		elementAgentField.GetValue(null).Should().BeNull("teardown must release the static element agent");
		IsSubscribedToAssemblyLoad(elementAgent).Should().BeFalse(
			"the released agent's process-wide AppDomain.AssemblyLoad subscription must be detached");

		// Re-running the teardown (e.g. Unloading firing after an explicit host dispose) must be safe.
		var secondTearDown = () => tearDown.Invoke(null, null);
		secondTearDown.Should().NotThrow("the collectible teardown flow must be idempotent");
	}

	/// <summary>
	/// Loads a fresh copy of the Uno.UI.RemoteControl assembly into a new collectible
	/// AssemblyLoadContext, the way a downstream host loads previewed apps into their own
	/// collectible contexts. The copy has its own statics, and its <c>_processorAlc</c> resolves
	/// to the collectible context, so the production collectible gates are exercised for real.
	/// </summary>
	private static (Assembly Copy, Type ProcessorType) LoadCollectibleProcessorCopy(string name)
	{
		var alc = new AssemblyLoadContext(name, isCollectible: true);
		var copy = alc.LoadFromAssemblyPath(typeof(ClientHotReloadProcessor).Assembly.Location);
		var processorType = copy.GetType(typeof(ClientHotReloadProcessor).FullName!, throwOnError: true)!;

		return (copy, processorType);
	}

	/// <summary>
	/// Instantiates the given assembly copy's own ElementUpdateAgent type (the constructor
	/// parameter types live in System.Private.CoreLib, so they are shared across contexts).
	/// </summary>
	private static object CreateElementAgentIn(Assembly copy)
	{
		var agentType = copy.GetType(typeof(ElementUpdateAgent).FullName!, throwOnError: true)!;

		return Activator.CreateInstance(
			agentType,
			new Action<string>(_ => { }),
			new Action<MethodInfo, Exception>((_, _) => { }))!;
	}

	/// <summary>
	/// True when a delegate targeting <paramref name="target"/> is subscribed to
	/// AppDomain.CurrentDomain.AssemblyLoad. On .NET the AppDomain event forwards to the internal
	/// static AssemblyLoadContext.AssemblyLoad field-like event, whose backing field is read here.
	/// </summary>
	private static bool IsSubscribedToAssemblyLoad(object target)
	{
		var field = typeof(AssemblyLoadContext).GetField("AssemblyLoad", BindingFlags.NonPublic | BindingFlags.Static)
			?? throw new InvalidOperationException(
				"AssemblyLoadContext.AssemblyLoad backing field not found — the runtime's AppDomain.AssemblyLoad forwarding layout changed.");

		var subscribers = ((Delegate?)field.GetValue(null))?.GetInvocationList() ?? Array.Empty<Delegate>();

		return subscribers.Any(d => ReferenceEquals(d.Target, target));
	}

	private static FieldInfo GetField(object target, string name) =>
		target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
		?? throw new InvalidOperationException($"Field '{name}' not found on {target.GetType()}.");

	private static FieldInfo GetStaticField(Type type, string name) =>
		type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
		?? throw new InvalidOperationException($"Static field '{name}' not found on {type}.");

	private static int Count(object target, string fieldName) =>
		((ICollection)GetField(target, fieldName).GetValue(target)!).Count;
}

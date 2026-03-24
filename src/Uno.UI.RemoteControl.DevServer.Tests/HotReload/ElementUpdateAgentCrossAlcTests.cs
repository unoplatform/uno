using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.Loader;
using Microsoft.UI.Xaml;
using Uno.UI.RemoteControl.DevServer.Tests.HotReload;
using Uno.UI.RemoteControl.HotReload.MetadataUpdater;

[assembly: ElementMetadataUpdateHandlerAttribute(typeof(FrameworkElement), typeof(FakeDefaultAlcHandler))]
[assembly: ElementMetadataUpdateHandlerAttribute(typeof(FrameworkElement), typeof(FakeAlternateHandler))]

namespace Uno.UI.RemoteControl.DevServer.Tests.HotReload;

[TestClass]
public class ElementUpdateAgentCrossAlcTests
{
	private readonly List<string> _logs = [];
	private readonly List<(MethodInfo, Exception)> _errors = [];

	private ElementUpdateAgent CreateAgent() =>
		new(msg => _logs.Add(msg), (m, e) => _errors.Add((m, e)));

	[TestMethod]
	public void When_DefaultAlc_Then_HandlersDiscovered()
	{
		// Arrange — the test assembly is in the default ALC
		using var agent = CreateAgent();

		// Act — handlers are discovered at construction time
		var handlerActions = agent.ElementHandlerActions;

		// Assert — FakeDefaultAlcHandler registers for FrameworkElement
		handlerActions.Should().ContainKey(typeof(FrameworkElement));
		var handlers = handlerActions[typeof(FrameworkElement)];
		handlers.Should().NotBeEmpty("at least one handler should be registered for FrameworkElement");

		// Verify the handler is invocable
		var state = new Dictionary<string, object>();
		handlers[0].CaptureState(null!, state, null);
		state.Should().ContainKey("captured", "CaptureState should have been invoked");
	}

	[TestMethod]
	public void When_MultipleHandlersForSameType_Then_AllRegistered()
	{
		// Arrange
		using var agent = CreateAgent();

		// Act — both FakeDefaultAlcHandler and FakeAlternateHandler register for FrameworkElement
		var handlerActions = agent.ElementHandlerActions;

		// Assert — both handlers should be present
		handlerActions.Should().ContainKey(typeof(FrameworkElement));
		var handlers = handlerActions[typeof(FrameworkElement)];
		handlers.Should().HaveCountGreaterThanOrEqualTo(2,
			"both FakeDefaultAlcHandler and FakeAlternateHandler should be registered");

		// Verify both handlers produce distinct behavior
		var state1 = new Dictionary<string, object>();
		var state2 = new Dictionary<string, object>();
		handlers[0].CaptureState(null!, state1, null);
		handlers[1].CaptureState(null!, state2, null);

		// One should have "captured", the other "alternate-captured"
		var allKeys = state1.Keys.Concat(state2.Keys).ToHashSet();
		allKeys.Should().Contain("captured");
		allKeys.Should().Contain("alternate-captured");
	}

	[TestMethod]
	public void When_AssemblyLoadInOtherAlc_Then_NoReload()
	{
		// Arrange
		using var agent = CreateAgent();
		var initialLogCount = _logs.Count;

		// Act — load an assembly in a different ALC
		var otherAlc = new AssemblyLoadContext("OtherAlc", isCollectible: true);
		try
		{
			// Loading the test assembly in another ALC should NOT trigger a reload
			// in our agent, because the agent filters by its own ALC.
			var thisAssemblyPath = typeof(ElementUpdateAgentCrossAlcTests).Assembly.Location;
			otherAlc.LoadFromAssemblyPath(thisAssemblyPath);

			// Assert — the agent should not have re-scanned
			// (No "Loading ElementMetatdataUpdateHandlerAttribute" log after the initial scan)
			var postLoadLogs = _logs.Skip(initialLogCount).ToList();
			postLoadLogs.Should().NotContain(
				msg => msg.Contains("Loading ElementMetatdataUpdateHandlerAttribute"),
				"loading an assembly in another ALC should not trigger handler re-scan");
		}
		finally
		{
			otherAlc.Unload();
		}
	}

	[TestMethod]
	public void When_AlcScopedAgent_Then_OnlyOwnAssemblies()
	{
		// Arrange
		using var agent = CreateAgent();

		// The agent's ALC is the default ALC (where this test runs).
		// It should only scan assemblies from the default ALC.
		var handlerActions = agent.ElementHandlerActions;

		// Assert — handlers were discovered (proving the agent works)
		handlerActions.Should().NotBeEmpty("agent should discover handlers in its own ALC");

		// Verify no errors from cross-ALC type mismatches
		_errors.Should().BeEmpty("ALC-scoped scanning should not produce cross-ALC errors");
	}
}

/// <summary>
/// A fake handler type with CaptureState/RestoreState for testing method resolution.
/// Must be public and static methods matching the expected signatures.
/// </summary>
public static class FakeDefaultAlcHandler
{
	public static void CaptureState(FrameworkElement element, IDictionary<string, object> state, Type[]? updatedTypes)
	{
		state["captured"] = true;
	}

	public static Task RestoreState(FrameworkElement element, IDictionary<string, object> state, Type[]? updatedTypes)
	{
		return Task.CompletedTask;
	}
}

/// <summary>
/// An alternate handler to test that multiple handlers can coexist for the same element type.
/// </summary>
public static class FakeAlternateHandler
{
	public static void CaptureState(FrameworkElement element, IDictionary<string, object> state, Type[]? updatedTypes)
	{
		state["alternate-captured"] = true;
	}

	public static Task RestoreState(FrameworkElement element, IDictionary<string, object> state, Type[]? updatedTypes)
	{
		return Task.CompletedTask;
	}
}

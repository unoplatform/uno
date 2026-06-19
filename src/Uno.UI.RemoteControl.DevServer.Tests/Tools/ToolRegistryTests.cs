#nullable enable

using System;
using System.Collections.Immutable;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.Tools;

namespace Uno.UI.RemoteControl.DevServer.Tests.Tools;

[TestClass]
public class ToolRegistryTests
{
	private static ToolDescriptor Tool(string name)
		=> new(name, "desc", ImmutableArray<ToolParameter>.Empty, IsReadOnly: false);

	private static ResourceDescriptor Resource(string uri)
		=> new(uri, "name", "desc", MimeType: null);

	private static ToolHandler Ok(string text = "ok")
		=> (_, _) => new ValueTask<ToolResult>(ToolResult.Text(text));

	private static ResourceReader Content(string text = "content")
		=> _ => new ValueTask<ToolResult>(ToolResult.Text(text));

	// --- registration & snapshot ---

	[TestMethod]
	[Description("Snapshot is the only way a consumer reads the catalogue, so a registered tool that doesn't surface there would be invisible to any consumer/MCP — this guards that registration reaches the snapshot.")]
	public void RegisterTool_ThenSnapshot_ContainsDescriptor()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("a_x"), Ok());

		var (tools, _) = registry.Snapshot();

		tools.Should().ContainSingle().Which.Name.Should().Be("a_x");
	}

	[TestMethod]
	[Description("Registrations are lifetime-scoped: disposing must remove the tool, and Changed must fire on both add and remove so consumers re-publish (list-changed semantics).")]
	public void RegisterTool_Dispose_RemovesDescriptor_AndRaisesChanged()
	{
		var registry = new ToolRegistryImpl();
		var changed = 0;
		registry.Changed += (_, _) => changed++;

		var registration = registry.RegisterTool(Tool("a_x"), Ok());
		changed.Should().Be(1);

		registration.Dispose();

		registry.Snapshot().Tools.Should().BeEmpty();
		changed.Should().Be(2);
	}

	[TestMethod]
	[Description("Name is the unique key: a late duplicate must neither hijack nor evict the live tool, and disposing the rejected (no-op) registration must leave the original winner intact.")]
	public async Task RegisterTool_DuplicateName_ReturnsNoOpDisposable_DoesNotEvictWinner()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("a_x"), Ok("first"));

		var duplicate = registry.RegisterTool(Tool("a_x"), Ok("second"));
		duplicate.Dispose(); // must NOT evict the winner

		registry.Snapshot().Tools.Should().ContainSingle();
		var result = await registry.InvokeAsync("a_x", new JsonObject(), default);
		result.Content[0].Text.Should().Be("first");
	}

	[TestMethod]
	[Description("Consumers diff successive snapshots; returning a stable (name-sorted) order prevents the unspecified ImmutableDictionary enumeration order from producing spurious list-changed churn.")]
	public void Snapshot_OrdersToolsByName_Stable()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("c_t"), Ok());
		registry.RegisterTool(Tool("a_t"), Ok());
		registry.RegisterTool(Tool("b_t"), Ok());

		registry.Snapshot().Tools.Select(t => t.Name).Should().Equal("a_t", "b_t", "c_t");
	}

	[TestMethod]
	[Description("The registry is a single many-to-many hub: tools contributed by independent publishers must all aggregate into one catalogue.")]
	public void MultiplePublishers_Snapshot_AggregatesAll()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("a_1"), Ok());
		registry.RegisterTool(Tool("b_1"), Ok());

		registry.Snapshot().Tools.Should().HaveCount(2);
	}

	// --- invocation ---

	[TestMethod]
	[Description("The happy path: a registered handler is actually reached and its result flows back to the consumer unaltered.")]
	public async Task InvokeAsync_Success_ReturnsResult()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("echo"), (inv, _) => new ValueTask<ToolResult>(ToolResult.Text(inv.GetString("v"))));

		var result = await registry.InvokeAsync("echo", new JsonObject { ["v"] = "hi" }, default);

		result.IsError.Should().BeFalse();
		result.Content[0].Text.Should().Be("hi");
	}

	[TestMethod]
	[Description("A handler fault must never escape as an exception to the consumer, and the raw exception message must not leak across the boundary — only the server-side log keeps the detail.")]
	public async Task InvokeAsync_HandlerThrows_ReturnsIsError()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("boom"), (_, _) => throw new InvalidOperationException("nope"));

		var result = await registry.InvokeAsync("boom", new JsonObject(), default);

		result.IsError.Should().BeTrue();
		result.Content[0].Text.Should().NotContain("nope");
	}

	[TestMethod]
	[Description("A throwing typed accessor inside a handler (here: a missing argument) is caught and surfaced as an error result, not an unhandled exception. The validator path is covered separately.")]
	public async Task InvokeAsync_HandlerThrowsOnMissingArg_ReturnsIsError()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("needs"), (inv, _) => new ValueTask<ToolResult>(ToolResult.Text(inv.GetString("absent"))));

		var result = await registry.InvokeAsync("needs", new JsonObject(), default);

		result.IsError.Should().BeTrue();
	}

	[TestMethod]
	[Description("Invoking a tool that was never registered (or already removed) must be a graceful error result, not a throw the consumer has to catch.")]
	public async Task InvokeAsync_UnknownTool_ReturnsIsError()
	{
		var registry = new ToolRegistryImpl();

		var result = await registry.InvokeAsync("does_not_exist", new JsonObject(), default);

		result.IsError.Should().BeTrue();
	}

	[TestMethod]
	[Description("When a tool opts into UI-thread affinity and the caller is off the UI thread, the registry must marshal the handler through the wired dispatcher.")]
	public async Task InvokeAsync_OffUIThread_UsesDispatcher()
	{
		var dispatcher = new FakeDispatcher(hasThreadAccess: false);
		var registry = new ToolRegistryImpl { Dispatcher = dispatcher };
		registry.RegisterTool(Tool("t"), Ok(), runOnUIThread: true);

		await registry.InvokeAsync("t", new JsonObject(), default);

		dispatcher.WasUsed.Should().BeTrue();
	}

	[TestMethod]
	[Description("Deadlock guard: when already on the UI thread the registry must run inline and NOT re-dispatch — a re-dispatch would self-deadlock on the single-threaded WASM runtime.")]
	public async Task InvokeAsync_AlreadyOnUIThread_RunsInline_NoDispatch()
	{
		var dispatcher = new FakeDispatcher(hasThreadAccess: true);
		var registry = new ToolRegistryImpl { Dispatcher = dispatcher };
		registry.RegisterTool(Tool("t"), Ok(), runOnUIThread: true);

		await registry.InvokeAsync("t", new JsonObject(), default);

		dispatcher.WasUsed.Should().BeFalse();
	}

	[TestMethod]
	[Description("Disposing a tool mid-call must not cancel an in-flight invocation: it runs to completion against the handler captured at invoke time.")]
	public async Task InvokeAsync_DisposeDuringInvoke_CompletesInFlight()
	{
		var registry = new ToolRegistryImpl();
		var gate = new TaskCompletionSource();
		var registration = registry.RegisterTool(Tool("slow"), async (_, ct) =>
		{
			await gate.Task;
			return ToolResult.Text("done");
		});

		var invocation = registry.InvokeAsync("slow", new JsonObject(), default).AsTask();
		registration.Dispose();

		registry.Snapshot().Tools.Should().BeEmpty();

		gate.SetResult();
		var result = await invocation;
		result.Content[0].Text.Should().Be("done");
	}

	// --- resources ---

	[TestMethod]
	[Description("A registered resource's reader is reached and its content returned to the consumer.")]
	public async Task ReadResourceAsync_ReturnsContent()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterResource(Resource("u://1"), Content("payload"));

		var result = await registry.ReadResourceAsync("u://1", default);

		result.Content[0].Text.Should().Be("payload");
	}

	[TestMethod]
	[Description("Reading an unknown uri must be a graceful error result, not a throw.")]
	public async Task ReadResourceAsync_Unknown_ReturnsIsError()
	{
		var registry = new ToolRegistryImpl();

		var result = await registry.ReadResourceAsync("u://missing", default);

		result.IsError.Should().BeTrue();
	}

	[TestMethod]
	[Description("A resource signalling a change must reach consumers carrying the correct uri, so each consumer knows which resource to re-read.")]
	public void NotifyUpdated_RaisesResourceUpdated_WithUri()
	{
		var registry = new ToolRegistryImpl();
		string? updated = null;
		registry.ResourceUpdated += (_, e) => updated = e.Uri;

		var registration = registry.RegisterResource(Resource("u://1"), Content());
		registration.NotifyUpdated();

		updated.Should().Be("u://1");
	}

	[TestMethod]
	[Description("Once a resource registration is disposed, signalling an update must be silently ignored — no consumer should be notified about a resource that no longer exists.")]
	public void NotifyUpdated_AfterDispose_IsNoOp()
	{
		var registry = new ToolRegistryImpl();
		var registration = registry.RegisterResource(Resource("u://1"), Content());
		registration.Dispose();

		string? updated = null;
		registry.ResourceUpdated += (_, e) => updated = e.Uri;
		registration.NotifyUpdated();

		updated.Should().BeNull();
	}

	[TestMethod]
	[Description("ResourceUpdated is a multicast event: every subscribed consumer must be notified of the update.")]
	public void MultipleConsumers_ResourceUpdated_Multicast()
	{
		var registry = new ToolRegistryImpl();
		int first = 0, second = 0;
		registry.ResourceUpdated += (_, _) => first++;
		registry.ResourceUpdated += (_, _) => second++;

		registry.RegisterResource(Resource("u://1"), Content()).NotifyUpdated();

		first.Should().Be(1);
		second.Should().Be(1);
	}

	// --- events: multicast, isolation, reentrancy ---

	[TestMethod]
	[Description("Changed is a multicast event: every consumer must be notified so each can re-publish its own view of the catalogue.")]
	public void MultipleConsumers_Changed_AllNotified()
	{
		var registry = new ToolRegistryImpl();
		int first = 0, second = 0;
		registry.Changed += (_, _) => first++;
		registry.Changed += (_, _) => second++;

		registry.RegisterTool(Tool("a"), Ok());

		first.Should().Be(1);
		second.Should().Be(1);
	}

	[TestMethod]
	[Description("Per-subscriber exception isolation: one faulty consumer handler must not prevent the others from being notified, nor surface back to the publisher that triggered the raise.")]
	public void Event_SubscriberThrows_OthersStillNotified()
	{
		var registry = new ToolRegistryImpl();
		var reached = 0;
		registry.Changed += (_, _) => throw new InvalidOperationException("isolated");
		registry.Changed += (_, _) => reached++;

		registry.RegisterTool(Tool("a"), Ok());

		reached.Should().Be(1);
	}

	[TestMethod]
	[Description("A subscriber that mutates the registry while handling Changed must be coalesced into a single follow-up raise, never cause unbounded recursion / StackOverflow.")]
	public void Event_ReentrantRegisterDuringRaise_DoesNotRecurseUnbounded()
	{
		var registry = new ToolRegistryImpl();
		var raises = 0;
		registry.Changed += (_, _) =>
		{
			raises++;
			if (raises == 1)
			{
				// Reentrant registration while handling the first Changed.
				registry.RegisterTool(Tool("b"), Ok());
			}
		};

		registry.RegisterTool(Tool("a"), Ok());

		raises.Should().Be(2);
		registry.Snapshot().Tools.Should().HaveCount(2);
	}

	// --- facade ---

	[TestMethod]
	[Description("The two segregated faces must be views over the same singleton, so what a publisher registers is exactly what a consumer reads.")]
	public void Publisher_And_Catalog_ResolveToSameInstance()
	{
		using (ToolRegistry.SetForTesting(new ToolRegistryImpl()))
		{
			ToolRegistry.Publisher.RegisterTool(Tool("a"), Ok());

			ToolRegistry.Catalog.Snapshot().Tools.Should().ContainSingle();
		}
	}

	[TestMethod]
	[Description("The test seam must swap the process-wide singleton and restore the previous instance on dispose, so a test using it can't leak registry state into other tests.")]
	public void SetForTesting_SwapsAndRestores()
	{
		var original = ToolRegistry.Catalog;

		using (ToolRegistry.SetForTesting(new ToolRegistryImpl()))
		{
			ToolRegistry.Catalog.Should().NotBeSameAs(original);
		}

		ToolRegistry.Catalog.Should().BeSameAs(original);
	}

	[TestMethod]
	[Description("With no dispatcher wired (the state this PR ships in), a UI-thread tool must still run inline rather than fail — the registry is usable before the host wires a dispatcher.")]
	public async Task InvokeAsync_RunOnUIThread_NoDispatcher_RunsInline()
	{
		var registry = new ToolRegistryImpl(); // no dispatcher wired
		registry.RegisterTool(Tool("t"), Ok("inline"), runOnUIThread: true);

		var result = await registry.InvokeAsync("t", new JsonObject(), default);

		result.Content[0].Text.Should().Be("inline");
	}

	[TestMethod]
	[Description("Cancellation is a thrown signal, never an error result: a token cancelled before invocation must throw OperationCanceledException.")]
	public async Task InvokeAsync_CancelledToken_Throws()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("t"), Ok());
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		Func<Task> act = async () => await registry.InvokeAsync("t", new JsonObject(), cts.Token);

		await act.Should().ThrowExactlyAsync<OperationCanceledException>();
	}

	[TestMethod]
	[Description("Cancellation propagates unconditionally — it is checked before the tool lookup — so even an unknown tool throws rather than returning an error result.")]
	public async Task InvokeAsync_CancelledToken_UnknownTool_Throws()
	{
		var registry = new ToolRegistryImpl();
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		Func<Task> act = async () => await registry.InvokeAsync("does_not_exist", new JsonObject(), cts.Token);

		await act.Should().ThrowExactlyAsync<OperationCanceledException>();
	}

	[TestMethod]
	[Description("A faulting resource reader is caught and surfaced as an error result, not an exception the consumer must handle.")]
	public async Task ReadResourceAsync_ReaderThrows_ReturnsIsError()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterResource(Resource("u://1"), _ => throw new InvalidOperationException("boom"));

		var result = await registry.ReadResourceAsync("u://1", default);

		result.IsError.Should().BeTrue();
	}

	// --- concurrency & reentrancy (lock-free invariants) ---

	[TestMethod]
	[Description("Under a parallel register/dispose storm the lock-free store must converge to a consistent (empty) state with no corruption, and the coalescing guard must not wedge — a later raise still fires. Changed is coalesced, so this asserts convergence + liveness, not a per-change raise count.")]
	public void ConcurrentRegisterUnregister_RemainsConsistentAndLive()
	{
		var registry = new ToolRegistryImpl();
		var raised = false;
		registry.Changed += (_, _) => raised = true;

		Parallel.For(0, 500, i =>
		{
			using (registry.RegisterTool(Tool($"t_{i}"), Ok()))
			{
			}
		});

		registry.Snapshot().Tools.Should().BeEmpty();
		raised.Should().BeTrue();

		var after = 0;
		registry.Changed += (_, _) => after++;
		using (registry.RegisterTool(Tool("final"), Ok()))
		{
		}

		after.Should().Be(2); // register + dispose each raise
	}

	[TestMethod]
	[Description("A ResourceUpdated handler that signals another update must be observed via the queued drain, not synchronous recursion — proving updates aren't lost and don't recurse.")]
	public void NotifyUpdated_ReentrantDuringRaise_DrainsWithoutUnboundedRecursion()
	{
		var registry = new ToolRegistryImpl();
		var registration = registry.RegisterResource(Resource("u://1"), Content());
		var raises = 0;
		registry.ResourceUpdated += (_, _) =>
		{
			raises++;
			if (raises == 1)
			{
				// Reentrant update while handling the first ResourceUpdated.
				registration.NotifyUpdated();
			}
		};

		registration.NotifyUpdated();

		raises.Should().Be(2);
	}

	[TestMethod]
	[Description("Deeply nested re-entrant updates must drain iteratively (via the queue), proving the drain can never StackOverflow no matter how deep the re-entrancy.")]
	public void NotifyUpdated_DeepReentrancy_DoesNotStackOverflow()
	{
		var registry = new ToolRegistryImpl();
		var registration = registry.RegisterResource(Resource("u://1"), Content());
		var raises = 0;
		registry.ResourceUpdated += (_, _) =>
		{
			if (++raises < 10_000)
			{
				registration.NotifyUpdated();
			}
		};

		registration.NotifyUpdated();

		raises.Should().Be(10_000);
	}

	[TestMethod]
	[Description("The genuine-concurrency variant of in-flight completion: disposing while a handler is really parked on another thread must still let the in-flight call finish (the same-thread sequential case alone wouldn't prove it).")]
	public async Task InvokeAsync_DisposeWhileHandlerParkedOnOtherThread_CompletesInFlight()
	{
		var registry = new ToolRegistryImpl();
		var entered = new TaskCompletionSource();
		var release = new TaskCompletionSource();
		var registration = registry.RegisterTool(Tool("slow"), async (_, _) =>
		{
			entered.SetResult();
			await release.Task;
			return ToolResult.Text("done");
		});

		var invocation = Task.Run(() => registry.InvokeAsync("slow", new JsonObject(), default).AsTask());
		await entered.Task; // handler is genuinely running and parked

		registration.Dispose();
		registry.Snapshot().Tools.Should().BeEmpty();

		release.SetResult();
		var result = await invocation;
		result.Content[0].Text.Should().Be("done");
	}

	// --- argument validation ---

	[TestMethod]
	[Description("A required parameter that is absent must be rejected at the boundary, so an obviously-invalid call never reaches the handler or the devserver.")]
	public void Validate_MissingRequiredArgument_ReturnsError()
	{
		var descriptor = ToolWith(new ToolParameter("name", "d", ToolParameterKind.String, IsRequired: true));

		ToolArgumentValidator.Validate(descriptor, new JsonObject()).Should().NotBeNull();
	}

	[TestMethod]
	[Description("An argument whose JSON type doesn't match the declared kind (string given for an Integer) must be rejected before dispatch.")]
	public void Validate_WrongType_ReturnsError()
	{
		var descriptor = ToolWith(new ToolParameter("count", "d", ToolParameterKind.Integer));

		ToolArgumentValidator.Validate(descriptor, new JsonObject { ["count"] = "not-a-number" }).Should().NotBeNull();
	}

	[TestMethod]
	[Description("Integer validation must agree with ToolInvocation.GetInt32: both a fractional (3.7) and a whole-valued double (3.0) are CLR-double-backed and rejected by GetInt32, so validation must reject them too — no drift between validator and accessor.")]
	public void Validate_FractionalNumberForIntegerParam_ReturnsError()
	{
		var descriptor = ToolWith(new ToolParameter("count", "d", ToolParameterKind.Integer));

		ToolArgumentValidator.Validate(descriptor, new JsonObject { ["count"] = 3.7 }).Should().NotBeNull();
		ToolArgumentValidator.Validate(descriptor, new JsonObject { ["count"] = 3.0 }).Should().NotBeNull();
	}

	[TestMethod]
	[Description("An enum-constrained parameter (AllowedValues) must reject a value outside the allowed set.")]
	public void Validate_AllowedValuesViolation_ReturnsError()
	{
		var descriptor = ToolWith(new ToolParameter(
			"color", "d", ToolParameterKind.String, AllowedValues: ImmutableArray.Create("red", "green")));

		ToolArgumentValidator.Validate(descriptor, new JsonObject { ["color"] = "blue" }).Should().NotBeNull();
	}

	[TestMethod]
	[Description("The JsonSchema escape hatch owns the parameter's shape, so flat kind validation must be intentionally bypassed and delegated to the consumer's schema mapping.")]
	public void Validate_JsonSchemaPresent_SkipsFlatValidation()
	{
		var descriptor = ToolWith(new ToolParameter(
			"payload", "d", ToolParameterKind.String, JsonSchema: "{\"type\":\"object\"}"));

		ToolArgumentValidator.Validate(descriptor, new JsonObject { ["payload"] = 123 }).Should().BeNull();
	}

	[TestMethod]
	[Description("A JSON null is treated as absent (consistent with TryGet*): an optional null is ignored, while a required null yields the clearer 'missing' error rather than a misleading type mismatch.")]
	public void Validate_NullArgument_TreatedAsAbsent()
	{
		var optional = ToolWith(new ToolParameter("name", "d", ToolParameterKind.String));
		ToolArgumentValidator.Validate(optional, new JsonObject { ["name"] = null }).Should().BeNull();

		var required = ToolWith(new ToolParameter("name", "d", ToolParameterKind.String, IsRequired: true));
		ToolArgumentValidator.Validate(required, new JsonObject { ["name"] = null }).Should().NotBeNull();
	}

	[TestMethod]
	[Description("Conforming arguments must pass validation — guards against false positives that would wrongly block a valid call.")]
	public void Validate_ValidArguments_ReturnsNull()
	{
		var descriptor = ToolWith(
			new ToolParameter("name", "d", ToolParameterKind.String, IsRequired: true),
			new ToolParameter("count", "d", ToolParameterKind.Integer));

		ToolArgumentValidator.Validate(descriptor, new JsonObject { ["name"] = "x", ["count"] = 3 }).Should().BeNull();
	}

	[TestMethod]
	[Description("Validation runs at the InvokeAsync boundary: a failing argument must surface as an error result before the handler is ever reached.")]
	public async Task InvokeAsync_InvalidArguments_ReturnsIsError()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(
			new ToolDescriptor("t", "d", ImmutableArray.Create(new ToolParameter("v", "d", ToolParameterKind.String, IsRequired: true)), IsReadOnly: false),
			Ok());

		var result = await registry.InvokeAsync("t", new JsonObject(), default);

		result.IsError.Should().BeTrue();
	}

	// --- timeout ---

	[TestMethod]
	[Description("A hung handler must be abandoned by the watchdog as an error result (not a thrown cancellation), so a misbehaving tool can never block the consumer indefinitely.")]
	public async Task InvokeAsync_HandlerExceedsTimeout_ReturnsIsError_NotCancellation()
	{
		var registry = new ToolRegistryImpl { InvocationTimeout = TimeSpan.FromMilliseconds(50) };
		registry.RegisterTool(Tool("hang"), async (_, ct) =>
		{
			await Task.Delay(Timeout.Infinite, ct);
			return ToolResult.Text("never");
		});

		var result = await registry.InvokeAsync("hang", new JsonObject(), default);

		result.IsError.Should().BeTrue();
	}

	[TestMethod]
	[Description("A misconfigured negative timeout must fail fast at assignment with a clear ArgumentOutOfRangeException, instead of surfacing later as an opaque CancelAfter failure on every invocation.")]
	public void InvocationTimeout_NegativeValue_Throws()
	{
		var act = () => new ToolRegistryImpl { InvocationTimeout = TimeSpan.FromSeconds(-5) };

		act.Should().ThrowExactly<ArgumentOutOfRangeException>();
	}

	[TestMethod]
	[Description("Caller cancellation while the handler is running must propagate as OperationCanceledException — distinct from the watchdog timeout, which is an error result — so consumers can tell the two causes apart.")]
	public async Task InvokeAsync_CallerCancelsDuringHandler_Throws()
	{
		var registry = new ToolRegistryImpl();
		using var cts = new CancellationTokenSource();
		var entered = new TaskCompletionSource();
		registry.RegisterTool(Tool("hang"), async (_, ct) =>
		{
			entered.SetResult();
			await Task.Delay(Timeout.Infinite, ct);
			return ToolResult.Text("never");
		});

		// Task.Run keeps the awaited task in-context for the threading analyzer; the gate ensures the
		// handler is genuinely running before the caller cancels.
		var invocation = Task.Run(() => registry.InvokeAsync("hang", new JsonObject(), cts.Token).AsTask());
		await entered.Task;
		await cts.CancelAsync();

		// Awaited at method scope (not inside a lambda) so the threading analyzer sees the Task.Run origin.
		// TaskCanceledException (from Task.Delay) derives from OperationCanceledException.
		OperationCanceledException? caught = null;
		try
		{
			await invocation;
		}
		catch (OperationCanceledException ex)
		{
			caught = ex;
		}

		caught.Should().NotBeNull();
	}

	[TestMethod]
	[Description("An OperationCanceledException the handler raises from its own token (neither the caller's nor the watchdog's) must propagate as a throw, never be misreported as a phantom 'timed out' result.")]
	public async Task InvokeAsync_HandlerThrowsOwnCancellation_PropagatesNotMisclassifiedAsTimeout()
	{
		var registry = new ToolRegistryImpl();
		using var unrelated = new CancellationTokenSource();
		await unrelated.CancelAsync();
		registry.RegisterTool(Tool("t"), (_, _) =>
			// Neither the caller token nor the watchdog fired — this OCE is the handler's own.
			throw new OperationCanceledException(unrelated.Token));

		Func<Task> act = async () => await registry.InvokeAsync("t", new JsonObject(), default);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	// --- typed parameter model ---

	[TestMethod]
	[Description("A parameter built without AllowedValues must expose an empty (not default/uninitialized) array, so the validator and consumers can enumerate it without an InvalidOperationException.")]
	public void ToolParameter_DefaultAllowedValues_ReadsAsEmpty_NotDefault()
	{
		var parameter = new ToolParameter("x", "d", ToolParameterKind.String);

		parameter.AllowedValues.IsDefault.Should().BeFalse();
		parameter.AllowedValues.Should().BeEmpty();
	}

	[TestMethod]
	[Description("A descriptor built with a default Parameters array must read as empty and validate/invoke without throwing on enumeration — the same default-ImmutableArray trap as AllowedValues.")]
	public async Task ToolDescriptor_DefaultParameters_ReadsAsEmpty_AndInvokesWithoutThrowing()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(new ToolDescriptor("t", "d", default, IsReadOnly: false), Ok());

		registry.Snapshot().Tools[0].Parameters.IsDefault.Should().BeFalse();
		var result = await registry.InvokeAsync("t", new JsonObject(), default);
		result.IsError.Should().BeFalse();
	}

	[TestMethod]
	[Description("The typed Get accessors must read Int32 and Boolean arguments correctly, proving the typed-parameter model is actually usable by handlers and not merely declarative.")]
	public async Task InvokeAsync_TypedAccessors_IntAndBool()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("t"), (inv, _) =>
			new ValueTask<ToolResult>(ToolResult.Text($"{inv.GetInt32("n")}:{inv.GetBoolean("b")}")));

		var result = await registry.InvokeAsync("t", new JsonObject { ["n"] = 42, ["b"] = true }, default);

		result.Content[0].Text.Should().Be("42:True");
	}

	[TestMethod]
	[Description("A null-valued JSON property must be reported as absent rather than handed back as a null string to a caller expecting a non-null value.")]
	public void TryGetString_NullValuedProperty_ReturnsFalse()
	{
		var invocation = new ToolInvocation(new JsonObject { ["x"] = null });

		invocation.TryGetString("x", out _).Should().BeFalse();
	}

	[TestMethod]
	[Description("The TryGet* accessors must return the typed value on a matching argument and report false (without coercing) for a wrong-typed or missing argument.")]
	public void TryGetInt32_And_TryGetBoolean_ReturnTypedValues()
	{
		var invocation = new ToolInvocation(new JsonObject { ["n"] = 7, ["b"] = true, ["s"] = "x" });

		invocation.TryGetInt32("n", out var n).Should().BeTrue();
		n.Should().Be(7);
		invocation.TryGetBoolean("b", out var b).Should().BeTrue();
		b.Should().BeTrue();
		invocation.TryGetInt32("s", out _).Should().BeFalse();
		invocation.TryGetBoolean("missing", out _).Should().BeFalse();
	}

	private static ToolDescriptor ToolWith(params ToolParameter[] parameters)
		=> new("t", "desc", parameters.ToImmutableArray(), IsReadOnly: false);

	private sealed class FakeDispatcher(bool hasThreadAccess) : IToolDispatcher
	{
		public bool WasUsed { get; private set; }

		public bool HasThreadAccess => hasThreadAccess;

		public Task<T> RunAsync<T>(Func<Task<T>> action)
		{
			WasUsed = true;
			return action();
		}
	}
}

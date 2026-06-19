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
	public void RegisterTool_ThenSnapshot_ContainsDescriptor()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("a_x"), Ok());

		var (tools, _) = registry.Snapshot();

		Assert.AreEqual(1, tools.Length);
		Assert.AreEqual("a_x", tools[0].Name);
	}

	[TestMethod]
	public void RegisterTool_Dispose_RemovesDescriptor_AndRaisesChanged()
	{
		var registry = new ToolRegistryImpl();
		var changed = 0;
		registry.Changed += (_, _) => changed++;

		var registration = registry.RegisterTool(Tool("a_x"), Ok());
		Assert.AreEqual(1, changed);

		registration.Dispose();

		Assert.AreEqual(0, registry.Snapshot().Tools.Length);
		Assert.AreEqual(2, changed);
	}

	[TestMethod]
	public async Task RegisterTool_DuplicateName_ReturnsNoOpDisposable_DoesNotEvictWinner()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("a_x"), Ok("first"));

		var duplicate = registry.RegisterTool(Tool("a_x"), Ok("second"));
		duplicate.Dispose(); // must NOT evict the winner

		Assert.AreEqual(1, registry.Snapshot().Tools.Length);
		var result = await registry.InvokeAsync("a_x", new JsonObject(), default);
		Assert.AreEqual("first", result.Content[0].Text);
	}

	[TestMethod]
	public void MultiplePublishers_Snapshot_AggregatesAll()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("a_1"), Ok());
		registry.RegisterTool(Tool("b_1"), Ok());

		Assert.AreEqual(2, registry.Snapshot().Tools.Length);
	}

	// --- invocation ---

	[TestMethod]
	public async Task InvokeAsync_Success_ReturnsResult()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("echo"), (inv, _) => new ValueTask<ToolResult>(ToolResult.Text(inv.GetString("v"))));

		var result = await registry.InvokeAsync("echo", new JsonObject { ["v"] = "hi" }, default);

		Assert.IsFalse(result.IsError);
		Assert.AreEqual("hi", result.Content[0].Text);
	}

	[TestMethod]
	public async Task InvokeAsync_HandlerThrows_ReturnsIsError()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("boom"), (_, _) => throw new InvalidOperationException("nope"));

		var result = await registry.InvokeAsync("boom", new JsonObject(), default);

		Assert.IsTrue(result.IsError);
		// The raw exception message must not leak to the caller — only the server-side log carries it.
		Assert.IsFalse(result.Content[0].Text!.Contains("nope"));
	}

	[TestMethod]
	public async Task InvokeAsync_HandlerThrowsOnMissingArg_ReturnsIsError()
	{
		// The descriptor declares no parameters, so the validator passes; this exercises the handler
		// surfacing a throwing typed accessor as an error result (the validator path is covered separately).
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("needs"), (inv, _) => new ValueTask<ToolResult>(ToolResult.Text(inv.GetString("absent"))));

		var result = await registry.InvokeAsync("needs", new JsonObject(), default);

		Assert.IsTrue(result.IsError);
	}

	[TestMethod]
	public async Task InvokeAsync_UnknownTool_ReturnsIsError()
	{
		var registry = new ToolRegistryImpl();

		var result = await registry.InvokeAsync("does_not_exist", new JsonObject(), default);

		Assert.IsTrue(result.IsError);
	}

	[TestMethod]
	public async Task InvokeAsync_OffUIThread_UsesDispatcher()
	{
		var dispatcher = new FakeDispatcher(hasThreadAccess: false);
		var registry = new ToolRegistryImpl { Dispatcher = dispatcher };
		registry.RegisterTool(Tool("t"), Ok(), runOnUIThread: true);

		await registry.InvokeAsync("t", new JsonObject(), default);

		Assert.IsTrue(dispatcher.WasUsed);
	}

	[TestMethod]
	public async Task InvokeAsync_AlreadyOnUIThread_RunsInline_NoDispatch()
	{
		var dispatcher = new FakeDispatcher(hasThreadAccess: true);
		var registry = new ToolRegistryImpl { Dispatcher = dispatcher };
		registry.RegisterTool(Tool("t"), Ok(), runOnUIThread: true);

		await registry.InvokeAsync("t", new JsonObject(), default);

		Assert.IsFalse(dispatcher.WasUsed);
	}

	[TestMethod]
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

		Assert.AreEqual(0, registry.Snapshot().Tools.Length);

		gate.SetResult();
		var result = await invocation;
		Assert.AreEqual("done", result.Content[0].Text);
	}

	// --- resources ---

	[TestMethod]
	public async Task ReadResourceAsync_ReturnsContent()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterResource(Resource("u://1"), Content("payload"));

		var result = await registry.ReadResourceAsync("u://1", default);

		Assert.AreEqual("payload", result.Content[0].Text);
	}

	[TestMethod]
	public async Task ReadResourceAsync_Unknown_ReturnsIsError()
	{
		var registry = new ToolRegistryImpl();

		var result = await registry.ReadResourceAsync("u://missing", default);

		Assert.IsTrue(result.IsError);
	}

	[TestMethod]
	public void NotifyUpdated_RaisesResourceUpdated_WithUri()
	{
		var registry = new ToolRegistryImpl();
		string? updated = null;
		registry.ResourceUpdated += (_, e) => updated = e.Uri;

		var registration = registry.RegisterResource(Resource("u://1"), Content());
		registration.NotifyUpdated();

		Assert.AreEqual("u://1", updated);
	}

	[TestMethod]
	public void NotifyUpdated_AfterDispose_IsNoOp()
	{
		var registry = new ToolRegistryImpl();
		var registration = registry.RegisterResource(Resource("u://1"), Content());
		registration.Dispose();

		string? updated = null;
		registry.ResourceUpdated += (_, e) => updated = e.Uri;
		registration.NotifyUpdated();

		Assert.IsNull(updated);
	}

	[TestMethod]
	public void MultipleConsumers_ResourceUpdated_Multicast()
	{
		var registry = new ToolRegistryImpl();
		int first = 0, second = 0;
		registry.ResourceUpdated += (_, _) => first++;
		registry.ResourceUpdated += (_, _) => second++;

		registry.RegisterResource(Resource("u://1"), Content()).NotifyUpdated();

		Assert.AreEqual(1, first);
		Assert.AreEqual(1, second);
	}

	// --- events: multicast, isolation, reentrancy ---

	[TestMethod]
	public void MultipleConsumers_Changed_AllNotified()
	{
		var registry = new ToolRegistryImpl();
		int first = 0, second = 0;
		registry.Changed += (_, _) => first++;
		registry.Changed += (_, _) => second++;

		registry.RegisterTool(Tool("a"), Ok());

		Assert.AreEqual(1, first);
		Assert.AreEqual(1, second);
	}

	[TestMethod]
	public void Event_SubscriberThrows_OthersStillNotified()
	{
		var registry = new ToolRegistryImpl();
		var reached = 0;
		registry.Changed += (_, _) => throw new InvalidOperationException("isolated");
		registry.Changed += (_, _) => reached++;

		registry.RegisterTool(Tool("a"), Ok());

		Assert.AreEqual(1, reached);
	}

	[TestMethod]
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

		// The reentrant registration is observed via a coalesced follow-up raise, not recursion.
		Assert.AreEqual(2, raises);
		Assert.AreEqual(2, registry.Snapshot().Tools.Length);
	}

	// --- facade ---

	[TestMethod]
	public void Publisher_And_Catalog_ResolveToSameInstance()
	{
		using (ToolRegistry.SetForTesting(new ToolRegistryImpl()))
		{
			ToolRegistry.Publisher.RegisterTool(Tool("a"), Ok());

			Assert.AreEqual(1, ToolRegistry.Catalog.Snapshot().Tools.Length);
		}
	}

	[TestMethod]
	public void SetForTesting_SwapsAndRestores()
	{
		var original = ToolRegistry.Catalog;

		using (ToolRegistry.SetForTesting(new ToolRegistryImpl()))
		{
			Assert.AreNotSame(original, ToolRegistry.Catalog);
		}

		Assert.AreSame(original, ToolRegistry.Catalog);
	}

	[TestMethod]
	public async Task InvokeAsync_RunOnUIThread_NoDispatcher_RunsInline()
	{
		var registry = new ToolRegistryImpl(); // no dispatcher wired
		registry.RegisterTool(Tool("t"), Ok("inline"), runOnUIThread: true);

		var result = await registry.InvokeAsync("t", new JsonObject(), default);

		Assert.AreEqual("inline", result.Content[0].Text);
	}

	[TestMethod]
	public async Task InvokeAsync_CancelledToken_Throws()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("t"), Ok());
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		await Assert.ThrowsExactlyAsync<OperationCanceledException>(
			async () => await registry.InvokeAsync("t", new JsonObject(), cts.Token));
	}

	[TestMethod]
	public async Task InvokeAsync_CancelledToken_UnknownTool_Throws()
	{
		var registry = new ToolRegistryImpl();
		using var cts = new CancellationTokenSource();
		await cts.CancelAsync();

		// Cancellation propagates before the unknown-tool lookup, not as an error result.
		await Assert.ThrowsExactlyAsync<OperationCanceledException>(
			async () => await registry.InvokeAsync("does_not_exist", new JsonObject(), cts.Token));
	}

	[TestMethod]
	public async Task ReadResourceAsync_ReaderThrows_ReturnsIsError()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterResource(Resource("u://1"), _ => throw new InvalidOperationException("boom"));

		var result = await registry.ReadResourceAsync("u://1", default);

		Assert.IsTrue(result.IsError);
	}

	// --- concurrency & reentrancy (lock-free invariants) ---

	[TestMethod]
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

		// All registrations were disposed: the lock-free store converges to empty with no corruption.
		// (Changed is coalesced, so this asserts convergence + liveness — not a per-change raise count.)
		Assert.AreEqual(0, registry.Snapshot().Tools.Length);
		Assert.IsTrue(raised);

		// The coalescing guard is not wedged after the storm: a later register + dispose both fire.
		var after = 0;
		registry.Changed += (_, _) => after++;
		using (registry.RegisterTool(Tool("final"), Ok()))
		{
		}

		Assert.AreEqual(2, after);
	}

	[TestMethod]
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

		// The reentrant update is observed via the queued drain, not synchronous recursion.
		Assert.AreEqual(2, raises);
	}

	[TestMethod]
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

		Assert.AreEqual(10_000, raises);
	}

	[TestMethod]
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
		Assert.AreEqual(0, registry.Snapshot().Tools.Length);

		release.SetResult();
		var result = await invocation;
		Assert.AreEqual("done", result.Content[0].Text);
	}

	// --- argument validation ---

	[TestMethod]
	public void Validate_MissingRequiredArgument_ReturnsError()
	{
		var descriptor = ToolWith(new ToolParameter("name", "d", ToolParameterKind.String, IsRequired: true));

		Assert.IsNotNull(ToolArgumentValidator.Validate(descriptor, new JsonObject()));
	}

	[TestMethod]
	public void Validate_WrongType_ReturnsError()
	{
		var descriptor = ToolWith(new ToolParameter("count", "d", ToolParameterKind.Integer));

		Assert.IsNotNull(ToolArgumentValidator.Validate(descriptor, new JsonObject { ["count"] = "not-a-number" }));
	}

	[TestMethod]
	public void Validate_FractionalNumberForIntegerParam_ReturnsError()
	{
		var descriptor = ToolWith(new ToolParameter("count", "d", ToolParameterKind.Integer));

		// A fractional Number must fail Integer validation so it agrees with ToolInvocation.GetInt32.
		Assert.IsNotNull(ToolArgumentValidator.Validate(descriptor, new JsonObject { ["count"] = 3.7 }));
		// A whole-valued double (3.0) is still CLR-double-backed, which GetInt32 rejects — so must validation.
		Assert.IsNotNull(ToolArgumentValidator.Validate(descriptor, new JsonObject { ["count"] = 3.0 }));
	}

	[TestMethod]
	public void Validate_AllowedValuesViolation_ReturnsError()
	{
		var descriptor = ToolWith(new ToolParameter(
			"color", "d", ToolParameterKind.String, AllowedValues: ImmutableArray.Create("red", "green")));

		Assert.IsNotNull(ToolArgumentValidator.Validate(descriptor, new JsonObject { ["color"] = "blue" }));
	}

	[TestMethod]
	public void Validate_JsonSchemaPresent_SkipsFlatValidation()
	{
		var descriptor = ToolWith(new ToolParameter(
			"payload", "d", ToolParameterKind.String, JsonSchema: "{\"type\":\"object\"}"));

		// A wrong flat type is tolerated because the escape hatch owns the shape.
		Assert.IsNull(ToolArgumentValidator.Validate(descriptor, new JsonObject { ["payload"] = 123 }));
	}

	[TestMethod]
	public void Validate_NullArgument_TreatedAsAbsent()
	{
		var optional = ToolWith(new ToolParameter("name", "d", ToolParameterKind.String));
		// An optional parameter passed as JSON null is ignored, not failed as a type mismatch.
		Assert.IsNull(ToolArgumentValidator.Validate(optional, new JsonObject { ["name"] = null }));

		var required = ToolWith(new ToolParameter("name", "d", ToolParameterKind.String, IsRequired: true));
		// A required parameter passed as JSON null reports the clearer "missing" error.
		Assert.IsNotNull(ToolArgumentValidator.Validate(required, new JsonObject { ["name"] = null }));
	}

	[TestMethod]
	public void Validate_ValidArguments_ReturnsNull()
	{
		var descriptor = ToolWith(
			new ToolParameter("name", "d", ToolParameterKind.String, IsRequired: true),
			new ToolParameter("count", "d", ToolParameterKind.Integer));

		Assert.IsNull(ToolArgumentValidator.Validate(descriptor, new JsonObject { ["name"] = "x", ["count"] = 3 }));
	}

	[TestMethod]
	public async Task InvokeAsync_InvalidArguments_ReturnsIsError()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(
			new ToolDescriptor("t", "d", ImmutableArray.Create(new ToolParameter("v", "d", ToolParameterKind.String, IsRequired: true)), IsReadOnly: false),
			Ok());

		var result = await registry.InvokeAsync("t", new JsonObject(), default);

		Assert.IsTrue(result.IsError);
	}

	// --- timeout ---

	[TestMethod]
	public async Task InvokeAsync_HandlerExceedsTimeout_ReturnsIsError_NotCancellation()
	{
		var registry = new ToolRegistryImpl { InvocationTimeout = TimeSpan.FromMilliseconds(50) };
		registry.RegisterTool(Tool("hang"), async (_, ct) =>
		{
			await Task.Delay(Timeout.Infinite, ct);
			return ToolResult.Text("never");
		});

		var result = await registry.InvokeAsync("hang", new JsonObject(), default);

		Assert.IsTrue(result.IsError);
	}

	[TestMethod]
	public void InvocationTimeout_NegativeValue_Throws()
	{
		// A misconfigured negative timeout is rejected at assignment, not deferred to CancelAfter.
		Assert.ThrowsExactly<ArgumentOutOfRangeException>(
			() => new ToolRegistryImpl { InvocationTimeout = TimeSpan.FromSeconds(-5) });
	}

	[TestMethod]
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

		Assert.IsNotNull(caught);
	}

	[TestMethod]
	public async Task InvokeAsync_HandlerThrowsOwnCancellation_PropagatesNotMisclassifiedAsTimeout()
	{
		var registry = new ToolRegistryImpl();
		using var unrelated = new CancellationTokenSource();
		await unrelated.CancelAsync();
		registry.RegisterTool(Tool("t"), (_, _) =>
			// Neither the caller token nor the watchdog fired — this OCE is the handler's own.
			throw new OperationCanceledException(unrelated.Token));

		// Per the contract, OCE always propagates as a throw; it must NOT become a phantom "timed out" result.
		await Assert.ThrowsAsync<OperationCanceledException>(
			async () => await registry.InvokeAsync("t", new JsonObject(), default));
	}

	// --- typed parameter model ---

	[TestMethod]
	public void ToolParameter_DefaultAllowedValues_ReadsAsEmpty_NotDefault()
	{
		var parameter = new ToolParameter("x", "d", ToolParameterKind.String);

		Assert.IsFalse(parameter.AllowedValues.IsDefault);
		Assert.IsTrue(parameter.AllowedValues.IsEmpty);
	}

	[TestMethod]
	public async Task ToolDescriptor_DefaultParameters_ReadsAsEmpty_AndInvokesWithoutThrowing()
	{
		var registry = new ToolRegistryImpl();
		// Parameters left as default (uninitialized ImmutableArray) must not throw when enumerated.
		registry.RegisterTool(new ToolDescriptor("t", "d", default, IsReadOnly: false), Ok());

		Assert.IsFalse(registry.Snapshot().Tools[0].Parameters.IsDefault);
		var result = await registry.InvokeAsync("t", new JsonObject(), default);
		Assert.IsFalse(result.IsError);
	}

	[TestMethod]
	public async Task InvokeAsync_TypedAccessors_IntAndBool()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterTool(Tool("t"), (inv, _) =>
			new ValueTask<ToolResult>(ToolResult.Text($"{inv.GetInt32("n")}:{inv.GetBoolean("b")}")));

		var result = await registry.InvokeAsync("t", new JsonObject { ["n"] = 42, ["b"] = true }, default);

		Assert.AreEqual("42:True", result.Content[0].Text);
	}

	[TestMethod]
	public void TryGetString_NullValuedProperty_ReturnsFalse()
	{
		var invocation = new ToolInvocation(new JsonObject { ["x"] = null });

		Assert.IsFalse(invocation.TryGetString("x", out _));
	}

	[TestMethod]
	public void TryGetInt32_And_TryGetBoolean_ReturnTypedValues()
	{
		var invocation = new ToolInvocation(new JsonObject { ["n"] = 7, ["b"] = true, ["s"] = "x" });

		Assert.IsTrue(invocation.TryGetInt32("n", out var n));
		Assert.AreEqual(7, n);
		Assert.IsTrue(invocation.TryGetBoolean("b", out var b));
		Assert.IsTrue(b);
		// Wrong-typed / missing values report false rather than coercing.
		Assert.IsFalse(invocation.TryGetInt32("s", out _));
		Assert.IsFalse(invocation.TryGetBoolean("missing", out _));
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

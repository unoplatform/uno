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
	}

	[TestMethod]
	public async Task InvokeAsync_MissingRequiredArg_ReturnsIsError()
	{
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
		cts.Cancel();

		await Assert.ThrowsExactlyAsync<OperationCanceledException>(
			async () => await registry.InvokeAsync("t", new JsonObject(), cts.Token));
	}

	[TestMethod]
	public async Task ReadResourceAsync_ReaderThrows_ReturnsIsError()
	{
		var registry = new ToolRegistryImpl();
		registry.RegisterResource(Resource("u://1"), _ => throw new InvalidOperationException("boom"));

		var result = await registry.ReadResourceAsync("u://1", default);

		Assert.IsTrue(result.IsError);
	}

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

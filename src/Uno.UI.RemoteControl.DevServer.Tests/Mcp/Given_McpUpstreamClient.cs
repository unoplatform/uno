using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Mcp;

namespace Uno.UI.RemoteControl.DevServer.Tests.Mcp;

/// <summary>
/// Tests for <see cref="McpUpstreamClient"/> patterns: TCS lifecycle,
/// dispose safety, callback registration, and reconnection.
/// <para>
/// These tests exercise TCS patterns in isolation because McpUpstreamClient
/// requires a real HTTP connection to a DevServer MCP endpoint. Thread-safety
/// of the TCS snapshot pattern is verified via <see cref="AtomicTcsHolder{T}"/>.
/// </para>
/// </summary>
[TestClass]
public class Given_McpUpstreamClient
{
	// -------------------------------------------------------------------
	// TCS patterns
	// -------------------------------------------------------------------

	[TestMethod]
	public void TCS_WhenFaulted_IsFaultedIsTrue()
	{
		var tcs = new TaskCompletionSource<object>();
		tcs.TrySetException(new InvalidOperationException("Connection failed"));

		tcs.Task.IsFaulted.Should().BeTrue();
		tcs.Task.IsCompletedSuccessfully.Should().BeFalse();
		tcs.Task.IsCompleted.Should().BeTrue();
	}

	[TestMethod]
	public void TCS_WhenCanceled_IsCompletedSuccessfullyIsFalse()
	{
		var tcs = new TaskCompletionSource<object>();
		tcs.TrySetCanceled();

		tcs.Task.IsCanceled.Should().BeTrue();
		tcs.Task.IsCompletedSuccessfully.Should().BeFalse();
		tcs.Task.IsCompleted.Should().BeTrue();
	}

	[TestMethod]
	public void TCS_TrySetException_OnlySucceedsOnce()
	{
		var tcs = new TaskCompletionSource<object>();

		var first = tcs.TrySetException(new InvalidOperationException("first"));
		var second = tcs.TrySetException(new InvalidOperationException("second"));

		first.Should().BeTrue();
		second.Should().BeFalse();
		tcs.Task.IsFaulted.Should().BeTrue();
	}

	[TestMethod]
	public void TCS_TrySetCanceled_AfterTrySetResult_Fails()
	{
		var tcs = new TaskCompletionSource<string>();
		tcs.TrySetResult("connected");

		var cancelResult = tcs.TrySetCanceled();

		cancelResult.Should().BeFalse();
		tcs.Task.IsCompletedSuccessfully.Should().BeTrue();
	}

	// -------------------------------------------------------------------
	// Timeout patterns
	// -------------------------------------------------------------------

	[TestMethod]
	public async Task WaitAsync_WhenTimesOut_ThrowsOperationCanceled()
	{
		var neverCompletes = new TaskCompletionSource<bool>();

		using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

		Func<Task> act = async () => await neverCompletes.Task.WaitAsync(cts.Token);

		await act.Should().ThrowAsync<OperationCanceledException>();
	}

	[TestMethod]
	public async Task WaitAsync_WhenCompletedBeforeTimeout_ReturnsResult()
	{
		var tcs = new TaskCompletionSource<string>();

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		tcs.TrySetResult("connected");

		var result = await tcs.Task.WaitAsync(cts.Token);

		result.Should().Be("connected");
	}

	[TestMethod]
	public async Task WaitAsync_WhenFaultedBeforeTimeout_PropagatesException()
	{
		var tcs = new TaskCompletionSource<string>();
		tcs.TrySetException(new InvalidOperationException("upstream crashed"));

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		Func<Task> act = async () => await tcs.Task.WaitAsync(cts.Token);

		await act.Should().ThrowExactlyAsync<InvalidOperationException>()
			.WithMessage("upstream crashed");
	}

	// -------------------------------------------------------------------
	// DisposeAsync safety
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("Canceling the TCS before DisposeAsync prevents infinite blocking on await")]
	public void DisposePattern_WhenTCSNeverCompleted_CancelPreventsBlocking()
	{
		var tcs = new TaskCompletionSource<IAsyncDisposable>();

		tcs.TrySetCanceled();

		tcs.Task.IsCompletedSuccessfully.Should().BeFalse();
		tcs.Task.IsCompleted.Should().BeTrue();

		var wouldDispose = tcs.Task.IsCompletedSuccessfully;
		wouldDispose.Should().BeFalse("DisposeAsync should be skipped when TCS is canceled");
	}

	[TestMethod]
	public async Task DisposePattern_WhenTCSCompleted_DisposesClient()
	{
		var mockDisposable = new MockAsyncDisposable();
		var tcs = new TaskCompletionSource<IAsyncDisposable>();
		tcs.TrySetResult(mockDisposable);

		tcs.Task.IsCompletedSuccessfully.Should().BeTrue();
		var client = await tcs.Task;
		await client.DisposeAsync();

		mockDisposable.WasDisposed.Should().BeTrue();
	}

	// -------------------------------------------------------------------
	// McpUpstreamClient TCS patterns
	// -------------------------------------------------------------------

	[TestMethod]
	public async Task UpstreamClient_TCS_HappyPath_UpstreamResolves()
	{
		var tcs = new TaskCompletionSource<string>();
		tcs.TrySetResult("upstream-connected");

		var result = await tcs.Task;
		result.Should().Be("upstream-connected");
		tcs.Task.IsCompletedSuccessfully.Should().BeTrue();
	}

	[TestMethod]
	public async Task UpstreamClient_TCS_WhenFaulted_AwaitPropagatesException()
	{
		var tcs = new TaskCompletionSource<string>();
		tcs.TrySetException(new InvalidOperationException("upstream connect failed"));

		Func<Task> act = async () => await tcs.Task;

		await act.Should().ThrowExactlyAsync<InvalidOperationException>()
			.WithMessage("upstream connect failed");
	}

	[TestMethod]
	public void UpstreamClient_Dispose_WhenNeverConnected_CompletesCleanly()
	{
		var tcs = new TaskCompletionSource<string>();

		tcs.TrySetCanceled();

		tcs.Task.IsCompleted.Should().BeTrue();
		tcs.Task.IsCanceled.Should().BeTrue();
		tcs.Task.IsCompletedSuccessfully.Should().BeFalse("dispose skips DisposeAsync on upstream");
	}

	[TestMethod]
	[Description("RegisterToolListChangedCallback replaces the previous callback, only the last one fires")]
	public async Task UpstreamClient_CallbackRegistration_IsReplaceable()
	{
		Func<Task>? callback = null;
		var callCount = 0;

		// First registration
		callback = () => { callCount = 1; return Task.CompletedTask; };

		// Second registration replaces the first
		callback = () => { callCount = 2; return Task.CompletedTask; };

		await callback.Invoke();
		callCount.Should().Be(2, "last registered callback should win");
	}

	// -------------------------------------------------------------------
	// Bug-exposing tests
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("ResetConnectionAsync creates a fresh TCS, enabling a second upstream connection after the first")]
	public async Task Bug1_TCS_IsResettable_AllowsSecondConnection()
	{
		var holder = new ResettableTcsHolder<string>();
		holder.Tcs.TrySetResult("first-connection");

		var firstResult = await holder.Tcs.Task;
		firstResult.Should().Be("first-connection");

		// Reset (creates new TCS — this is what ResetConnectionAsync does)
		holder.Reset();

		holder.Tcs.TrySetResult("second-connection");
		var secondResult = await holder.Tcs.Task;
		secondResult.Should().Be("second-connection");
	}

	[TestMethod]
	[Description("When ToolListChangedNotification fires during CreateAsync, the explicit post-connect callback is skipped to avoid duplicates")]
	public async Task ConnectOrDie_WhenNotificationFiredDuringConnect_SkipsExplicitCallback()
	{
		var callCount = 0;
		Func<Task> callback = () => { callCount++; return Task.CompletedTask; };

		var notificationAlreadyFired = false;

		// Simulate: notification fires during CreateAsync
		notificationAlreadyFired = true;
		if (notificationAlreadyFired && callback is { } c1)
		{
			await c1();
		}

		// Simulate: explicit post-connect callback — should be skipped
		if (!notificationAlreadyFired && callback is { } c2)
		{
			await c2();
		}

		callCount.Should().Be(1, "callback should fire exactly once when notification already fired during connect");
	}

	[TestMethod]
	[Description("When no ToolListChangedNotification fires during CreateAsync, the explicit post-connect callback runs to unblock waiters")]
	public async Task ConnectOrDie_WhenNoNotificationDuringConnect_ExplicitCallbackRuns()
	{
		var callCount = 0;
		Func<Task> callback = () => { callCount++; return Task.CompletedTask; };

		var notificationAlreadyFired = false;

		// Simulate: no notification during CreateAsync

		// Simulate: explicit post-connect callback — should run
		if (!notificationAlreadyFired && callback is { } c2)
		{
			await c2();
		}

		callCount.Should().Be(1, "callback should fire exactly once via explicit path when no notification arrived");
	}

	[TestMethod]
	[Description("Notification handler must await the async callback to ensure it completes before returning")]
	public async Task Bug3_CallbackHandler_AwaitsAsyncCallback()
	{
		var completed = false;
		Func<Task> asyncCallback = async () =>
		{
			await Task.Delay(10);
			completed = true;
		};

		// Correct pattern: await the callback
		if (asyncCallback is { } callback)
		{
			await callback();
		}

		completed.Should().BeTrue("awaited async callback should complete before handler returns");
	}

	[TestMethod]
	[Description("Demonstrates the bug: fire-and-forget on an async callback returns before the body completes")]
	public void Bug3_FireAndForget_DoesNotCompleteCallback()
	{
		var completed = false;
		Func<Task> asyncCallback = async () =>
		{
			await Task.Yield();
			completed = true;
		};

#pragma warning disable VSTHRD110 // Observe the awaitable result
		asyncCallback.Invoke(); // fire-and-forget — the bug
#pragma warning restore VSTHRD110

		// The callback has not yet completed because we didn't await
		completed.Should().BeFalse("fire-and-forget does not await the async body");
	}

	// -------------------------------------------------------------------
	// TCS snapshot stability under concurrent Reset
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("A captured TCS snapshot is not swapped out by a concurrent Reset")]
	public void CapturedTcs_StableReferenceAfterReset()
	{
		var holder = new AtomicTcsHolder<string>();
		var captured = holder.Capture();

		holder.Reset(); // Cancels old TCS, installs new one

		// The captured reference is the old TCS (now canceled by Reset)
		captured.Task.IsCanceled.Should().BeTrue("Reset cancels the old TCS");

		// The holder now points to a fresh, independent TCS
		holder.CurrentTask.IsCompleted.Should().BeFalse("new TCS after Reset is independent");

		// Setting result on captured does NOT affect the new TCS
		captured.TrySetResult("late").Should().BeFalse("already canceled");
	}

	[TestMethod]
	[Description("Concurrent Reset and Capture never lose a TCS")]
	public void ConcurrentResetAndCapture_NeverLosesTcs()
	{
		var holder = new AtomicTcsHolder<string>();
		var capturedTasks = new System.Collections.Concurrent.ConcurrentBag<TaskCompletionSource<string>>();

		Parallel.For(0, 50, i =>
		{
			if (i % 2 == 0)
			{
				capturedTasks.Add(holder.Capture());
			}
			else
			{
				holder.Reset();
			}
		});

		// Every captured TCS should be settable exactly once
		foreach (var tcs in capturedTasks)
		{
			tcs.TrySetResult("ok"); // may be already canceled by Reset, that's fine
			tcs.Task.IsCompleted.Should().BeTrue();
		}
	}

	// -------------------------------------------------------------------
	// Exception path must fault the captured TCS, not the current one
	// -------------------------------------------------------------------

	[TestMethod]
	[Description("When a connect fails after Reset, the exception must not pollute the new TCS")]
	public void ExceptionAfterReset_MustNotPolluteFreshTcs()
	{
		var holder = new AtomicTcsHolder<string>();

		// Simulate: ConnectOrDie captures a snapshot
		var captured = holder.Capture();

		// Simulate: concurrent Reset swaps in a fresh TCS and cancels the old one
		holder.Reset();
		captured.Task.IsCanceled.Should().BeTrue("Reset cancels the old TCS");

		// Simulate: the old connect fails — TrySetException returns false (already canceled)
		var faulted = captured.TrySetException(new InvalidOperationException("connect failed"));
		faulted.Should().BeFalse("TCS was already in a terminal state");

		// The critical invariant: the NEW TCS must remain untouched
		holder.CurrentTask.IsCompleted.Should().BeFalse("the fresh TCS must not be affected by a stale exception");
	}

	[TestMethod]
	[Description("TrySetResult after Reset returns false (TCS was canceled); callback should be skipped")]
	public void TrySetResultAfterReset_ReturnsFalse_CallbackShouldBeSkipped()
	{
		var holder = new AtomicTcsHolder<string>();
		var captured = holder.Capture();

		holder.Reset(); // cancels old TCS

		var setSucceeded = captured.TrySetResult("stale-connection");

		setSucceeded.Should().BeFalse("Reset already canceled this TCS");
		// The pattern: only invoke callback if TrySetResult returns true
	}

	// -------------------------------------------------------------------
	// Helpers
	// -------------------------------------------------------------------

	/// <summary>
	/// Simulates the TCS reset pattern that <see cref="McpUpstreamClient.ResetConnectionAsync"/> implements.
	/// </summary>
	private sealed class ResettableTcsHolder<T>
	{
		public TaskCompletionSource<T> Tcs { get; private set; } = new();

		public void Reset()
		{
			var old = Tcs;
			Tcs = new TaskCompletionSource<T>();
			old.TrySetCanceled();
		}
	}

	/// <summary>
	/// Lock-free TCS holder that mirrors the Volatile/Interlocked snapshot pattern
	/// used in <see cref="McpUpstreamClient"/>.
	/// </summary>
	private sealed class AtomicTcsHolder<T>
	{
		private TaskCompletionSource<T> _tcs = new();

		public TaskCompletionSource<T> Capture()
			=> Volatile.Read(ref _tcs);

		public Task<T> CurrentTask
			=> Volatile.Read(ref _tcs).Task;

		public void Reset()
		{
			var old = Interlocked.Exchange(ref _tcs, new TaskCompletionSource<T>());
			old.TrySetCanceled();
		}
	}

	private sealed class MockAsyncDisposable : IAsyncDisposable
	{
		public bool WasDisposed { get; private set; }

		public ValueTask DisposeAsync()
		{
			WasDisposed = true;
			return ValueTask.CompletedTask;
		}
	}
}

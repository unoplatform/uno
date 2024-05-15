using System;
using System.Threading;
using System.Threading.Tasks;

namespace MUXControlsTestApp.Utilities;

/// <summary>
/// A type that matches the API surface of ManualResetEvent and AutoResetEvent
/// Usages of ManualResetEvent and AutoResetEvent is problematic on Wasm, as they
/// will block the current thread until some other thread calls Set().
/// On Wasm, where we are in a single-threaded environment, this can block forever.
/// To ease the port of tests from WinUI, we introduce this type with similar API surface.
/// But WaitOne has to be async.
/// NOTE: There are more complex guarantees by .NET's ManualResetEvent and AutoResetEvent
/// But these are not important for us and we don't implement them as what we
/// are trying to implement here is far from the concept of EventWaitHandle.
/// For example, AutoResetEvent guarantees that only one waiting thread is released.
/// But on Wasm (the main reason we implement this), the whole application is actually single-threaded!
/// </summary>
public abstract class UnoManualOrAutoResetEvent : IDisposable
{
	private protected bool _wasSet;

	protected UnoManualOrAutoResetEvent(bool initialState)
		=> _wasSet = initialState;

	public void Set()
		=> _wasSet = true;

	public void Reset()
		=> _wasSet = false;

	private protected abstract void OnWaitOneSuccess();

	public async Task<bool> WaitOne()
	{
		for (int i = 0; i < TestUtilities.DefaultWaitMs / 50; i++)
		{
			if (_wasSet)
			{
				OnWaitOneSuccess();
				return true;
			}

			await Task.Delay(50);
		}

		if (!_wasSet)
		{
			Assert.Fail("WaitOne timed out.");
			return false; // Assert.Fail should be throwing anyways and we wouldn't return a value.
		}

		OnWaitOneSuccess();
		return true;
	}

	public async Task<bool> WaitOne(int millisecondsTimeout)
		=> await WaitOne(TimeSpan.FromMilliseconds(millisecondsTimeout));

	public async Task<bool> WaitOne(TimeSpan timeout)
	{
		for (int i = 0; i < timeout.TotalMilliseconds / 50; i++)
		{
			if (_wasSet)
			{
				OnWaitOneSuccess();
				return true;
			}

			await Task.Delay(50);
		}

		if (!_wasSet)
		{
			return false;
		}

		OnWaitOneSuccess();
		return true;
	}

	public void Dispose() { }
}

public sealed class UnoManualResetEvent : UnoManualOrAutoResetEvent
{
	public UnoManualResetEvent(bool initialState) : base(initialState)
	{
	}

	private protected override void OnWaitOneSuccess()
	{
	}
}

public sealed class UnoAutoResetEvent : UnoManualOrAutoResetEvent
{
	public UnoAutoResetEvent(bool initialState) : base(initialState)
	{
	}

	private protected override void OnWaitOneSuccess()
		=> Reset();
}

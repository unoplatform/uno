using System;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.ServerCore;

/// <summary>
/// Represents a running instance of the transport-agnostic devserver host.
/// </summary>
public sealed class RemoteControlServerHost : IAsyncDisposable
{
	private readonly IServiceProvider _serviceProvider;
	private bool _isDisposed;

	internal RemoteControlServerHost(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	/// <summary>
	/// Starts the devserver instance.
	/// </summary>
	/// <param name="ct">Cancellation token used to observe cancellation.</param>
	public ValueTask StartAsync(CancellationToken ct = default)
	{
		ThrowIfDisposed();
		return ValueTask.CompletedTask;
	}

	/// <summary>
	/// Stops the devserver instance.
	/// </summary>
	/// <param name="ct">Cancellation token used to observe cancellation.</param>
	public ValueTask StopAsync(CancellationToken ct = default)
	{
		ThrowIfDisposed();
		return ValueTask.CompletedTask;
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_isDisposed)
		{
			return;
		}

		_isDisposed = true;

		if (_serviceProvider is IAsyncDisposable asyncDisposable)
		{
			await asyncDisposable.DisposeAsync().ConfigureAwait(false);
			return;
		}

		if (_serviceProvider is IDisposable disposable)
		{
			disposable.Dispose();
		}
	}

	private void ThrowIfDisposed()
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(nameof(RemoteControlServerHost));
		}
	}
}
